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
    public Transform bird;
    public int birdCount = 100;
    public float boundsWidth;
    public float boundsHeight;
    public float boundsDepth;
    public float distNeighbor = 1f;

    List<Transform> boidTransforms;

    void Start()
    {
        boidTransforms = new List<Transform>(birdCount);
        for (int i = 0; i < birdCount; ++i)
        {
            Transform boidTransform = 
                Instantiate(bird, 
                            new Vector3(UnityEngine.Random.Range(-boundsWidth, boundsWidth), 
                                        UnityEngine.Random.Range(-boundsHeight, boundsHeight),
                                        UnityEngine.Random.Range(-boundsDepth, boundsDepth)),
                            Quaternion.identity);
            boidTransforms.Add(boidTransform);
        }
    }

    void Update()
    {
        
    }

    public struct CalcBoidTransforms : IJobParallelForTransform
    {
        [ReadOnly] public TransformAccessArray boidTransforms;
        [ReadOnly] public float deltaTime;
        [ReadOnly] public float distNeighbor;

        public void Execute(int index, TransformAccess transform)
        {
            // Iterate through boids looking for flock
            for (int b = 0; b < boidTransforms.length; ++b)
            {
                if (b != index)
                {
                    Transform otherBoid = boidTransforms[b];
                    if (PossibleNeighbor(transform.position.x, otherBoid.position.x, distNeighbor))
                    {

                    }
                }
            }
        }

        bool PossibleNeighbor(float x, float otherx, float dist)
        {
            return (Mathf.Abs(x - otherx) < dist);
        }
    }
}
