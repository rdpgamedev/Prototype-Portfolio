using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public class FlockManager : MonoBehaviour
{
    public static FlockManager instance;

    public Transform boid;
    public int boidCount = 100;
    public float MAX_ROT_SPEED;
    public float AVG_SPEED;
    public float boundsWidth;
    public float boundsHeight;
    public float boundsDepth;
    public float distNeighbor = 1f;

    //Transform[] boidTransforms;
    TransformAccessArray boidTransforms;
    NativeArray<float> boidSpeed;
    NativeArray<Vector3> posBuffer;
    NativeArray<Quaternion> rotBuffer;
    NativeArray<float> speedBuffer;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        //boidTransforms = new Transform[boidCount];
        boidTransforms = new TransformAccessArray(boidCount);

        boidSpeed = new NativeArray<float>(boidCount, Allocator.Persistent);
        posBuffer = new NativeArray<Vector3>(boidCount, Allocator.Persistent);
        rotBuffer = new NativeArray<Quaternion>(boidCount, Allocator.Persistent);
        speedBuffer = new NativeArray<float>(boidCount, Allocator.Persistent);

        for (int i = 0; i < boidCount; ++i)
        {
            Transform boidTransform = 
                Instantiate(boid, 
                            new Vector3(UnityEngine.Random.Range(-boundsWidth/2f, boundsWidth/2f), 
                                        UnityEngine.Random.Range(-boundsHeight/2f, boundsHeight/2f),
                                        UnityEngine.Random.Range(-boundsDepth/2f, boundsDepth/2f)),
                            UnityEngine.Random.rotation);
                            
            boidTransforms.Add(boidTransform);
            boidSpeed[i] = UnityEngine.Random.Range(AVG_SPEED - 1.0f, AVG_SPEED + 1.0f);
        }
    }

    void Update()
    {
        var copyJob = new CopyBoidStatesJob()
        {
            srcSpeed = boidSpeed,
            destPos = posBuffer,
            destRot = rotBuffer,
            destSpeed = speedBuffer
        };

        JobHandle copyJobHandle = copyJob.Schedule(boidTransforms);
        copyJobHandle.Complete();

        var calcBoidTransformsJob = new CalcBoidTransformsJob()
        {
            tranSpeed = boidSpeed,
            boidPos = posBuffer,
            boidRot = rotBuffer,
            boidSpeed = speedBuffer,
            rotSpeed = MAX_ROT_SPEED,
            deltaTime = Time.deltaTime,
            distNeighbor = distNeighbor,
            boidCount = boidCount
        };

        JobHandle calcBoidsJobHandle = calcBoidTransformsJob.Schedule(boidTransforms);
        calcBoidsJobHandle.Complete();
    }

    void OnDestroy()
    {
        boidTransforms.Dispose();
        boidSpeed.Dispose();
        posBuffer.Dispose();
        rotBuffer.Dispose();
        speedBuffer.Dispose();
    }

    void OnDrawGizmosSelected()
    {
        // Draw cube to show bounds volume
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawCube(transform.position, new Vector3(boundsWidth, boundsHeight, boundsDepth));
    }

    public struct CopyBoidStatesJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<float> srcSpeed;
        public NativeArray<Vector3> destPos;
        public NativeArray<Quaternion> destRot;
        public NativeArray<float> destSpeed;

        public void Execute(int index, TransformAccess transform)
        {
            destPos[index] = transform.position;
            destRot[index] = transform.rotation;
            destSpeed[index] = srcSpeed[index];
        }
    }

    public struct CalcBoidTransformsJob : IJobParallelForTransform
    {
        public NativeArray<float> tranSpeed;
        [ReadOnly] public NativeArray<Vector3> boidPos;
        [ReadOnly] public NativeArray<Quaternion> boidRot;
        [ReadOnly] public NativeArray<float> boidSpeed;
        [ReadOnly] public float rotSpeed;
        [ReadOnly] public float deltaTime;
        [ReadOnly] public float distNeighbor;
        [ReadOnly] public int boidCount;
        
        // Implements this Flocking Algorithm:
        // "An Approach to Parallel Processing with Unity"
        // by Jeremy Servoz
        // https://software.intel.com/en-us/articles/an-approach-to-parallel-processing-with-unity
        public void Execute(int index, TransformAccess transform)
        {
            int flockSize = 1;
            Vector3 flockCenter = transform.position;
            float flockSpeed = boidSpeed[index];
            Vector3 flockForward = (transform.rotation * Vector3.forward).normalized;

            // Iterate through boids looking for flock
            for (int b = 0; b < boidCount; ++b)
            {
                if (b != index)
                {
                    Vector3 otherPos = boidPos[b];
                    Quaternion otherRot = boidRot[b];
                    float otherSpeed = boidSpeed[b];
                    if (PossibleNeighbor(transform.position.x, otherPos.x, distNeighbor))
                    {
                        float dist = Vector3.Distance(otherPos, transform.position);
                        if (Neighbor((otherRot * Vector3.forward).normalized, (transform.rotation * Vector3.forward).normalized, dist, distNeighbor))
                        {
                            // Add to flock data
                            flockSize++;
                            // Cohesion
                            flockCenter += otherPos;
                            flockSpeed += otherSpeed;
                            
                        }
                    }
                }
            }
            // Rotate boid toward new direction
            Vector3 direction = (transform.rotation * Vector3.forward);
            if (flockSize > 1)
            {
                Vector3 centerDirection = (flockCenter/flockSize - transform.position).normalized;
                direction = Vector3.RotateTowards(direction, centerDirection, rotSpeed * deltaTime, 0.0f);
                transform.rotation = Quaternion.LookRotation(direction);
            }
            
            // Approach flock speed
            tranSpeed[index] += (flockSpeed/flockSize - tranSpeed[index]) * 0.5f;

            // Calculate new position
            Vector3 newPos = tranSpeed[index] * deltaTime * (transform.rotation * Vector3.forward).normalized + transform.position;

            // Check if newPos is in bounds
            if (!PositionInBounds(newPos))
            {
                // newPos is not in bounds so rotate boid toward origin and calculate new position
                Vector3 originDirection = Vector3.zero - transform.position;
                direction = Vector3.RotateTowards(direction, originDirection, rotSpeed * deltaTime, 0.0f);
                transform.rotation = Quaternion.LookRotation(direction);
                newPos = tranSpeed[index] * deltaTime * (transform.rotation * Vector3.forward).normalized + transform.position;
            }

            // Move boid forward according to new forward direction and speed
            transform.position = newPos;
        }

        bool Neighbor(Vector3 othFwd, Vector3 curFwd, float dist, float neighborDist)
        {
            // Dynamically calculate neighbor distance based on dot product of forwards
            float dotProduct = Vector3.Dot(othFwd, curFwd);
            float scale = dotProduct/2f + 1f; // Range is now 0.5 to 1.5
            return (dist < neighborDist * scale);
        }

        bool PositionInBounds(Vector3 position)
        {
            FlockManager manager = FlockManager.instance;
            
            return (position.x <  manager.boundsWidth/2f
                 && position.x > -manager.boundsWidth/2f
                 && position.y <  manager.boundsHeight/2f
                 && position.y > -manager.boundsHeight/2f
                 && position.z <  manager.boundsDepth/2f
                 && position.z > -manager.boundsDepth/2f);
        }

        bool PossibleNeighbor(float x, float otherx, float dist)
        {
            return (Mathf.Abs(x - otherx) < dist);
        }
    }
}
