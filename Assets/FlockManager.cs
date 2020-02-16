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
    public Transform boid;
    public int boidCount = 100;
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
                            new Vector3(UnityEngine.Random.Range(-boundsWidth, boundsWidth), 
                                        UnityEngine.Random.Range(-boundsHeight, boundsHeight),
                                        UnityEngine.Random.Range(-boundsDepth, boundsDepth)),
                            UnityEngine.Random.rotation);
                            
            boidTransforms.Add(boidTransform);
            boidSpeed[i] = UnityEngine.Random.Range(0.5f, 1.5f);
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
        [ReadOnly] public float deltaTime;
        [ReadOnly] public float distNeighbor;
        [ReadOnly] public int boidCount;
        
        // Implements this Flocking Algorithm:
        // "An Approach to Parallel Processing with Unity"
        // by Jeremy Servoz
        // https://software.intel.com/en-us/articles/an-approach-to-parallel-processing-with-unity
        public void Execute(int index, TransformAccess transform)
        {
            int flockSize = 0;
            Vector3 flockCenter = Vector3.zero;
            float flockSpeed = 0f;
            Vector3 flockForward = Vector3.zero;

            // Iterate through boids looking for flock
            for (int b = 0; b < boidCount; ++b)
            {
                if (b != index)
                {
                    Vector3 otherPos = boidPos[b];
                    Quaternion otherRot = boidRot[b];
                    if (PossibleNeighbor(transform.position.x, otherPos.x, distNeighbor))
                    {
                        float dist = Vector3.Distance(otherPos, transform.position);
                        if (Neighbor((otherRot * Vector3.forward).normalized, (transform.rotation * Vector3.forward).normalized, dist, distNeighbor))
                        {
                            // Add to flock data
                            flockSize++;
                            flockCenter += otherPos;
                            flockSpeed += boidSpeed[b];
                        }
                    }
                }
            }
            // Move boid forward according to speed
            transform.position += tranSpeed[index] * deltaTime * (transform.rotation * Vector3.forward).normalized;
        }

        bool Neighbor(Vector3 othFwd, Vector3 curFwd, float dist, float neighborDist)
        {
            // Dynamically calculate neighbor distance based on dot product of forwards
            float dotProduct = Vector3.Dot(othFwd, curFwd);
            float scale = dotProduct/2f + 1f; // Range is now 0.5 to 1.5
            return (dist < neighborDist * scale);
        }

        bool PossibleNeighbor(float x, float otherx, float dist)
        {
            return (Mathf.Abs(x - otherx) < dist);
        }
    }
}
