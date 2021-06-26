using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using AdvancedMesh;
using Unity.Collections;

public class AdvancedMeshAnimationSample : MonoBehaviour
{
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public struct VertexUpdateJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<PlaneVertex> buffer;

        public float time;
        public void Execute(int index)
        {
            PlaneVertex pv = buffer[index];

            float height = 1.1f * Mathf.PerlinNoise(pv.pos.x, pv.pos.y + time) - 0.5f;
            Vector3 pos = new Vector3(pv.pos.x, pv.pos.y, height);
            pv.pos = pos;
            buffer[index] = pv;
        }
    }

    [SerializeField] private AdvancedPlane _plane;

    private VertexUpdateJob _verticesUpdatejob;
    private float _time;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(_plane != null)
        {
            _plane.UpdateMesh(UpdateVertices);
        }
    }

    private void UpdateVertices(NativeArray<PlaneVertex> vBuffer, int count)
    {
        _time += Time.deltaTime * 0.5f;
        _verticesUpdatejob = new VertexUpdateJob()
        {
            buffer = vBuffer,
            time = _time
        };

        var jobHandle = _verticesUpdatejob.Schedule(count, 100);
        jobHandle.Complete();
    }
}
