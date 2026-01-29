using System;
using UnityEngine;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections.Generic;

public class LegionRenderer : MonoBehaviour
{

    [Header("Settings")]
    public int instanceCount = 10000;
    public Mesh[] unitMeshes;
    public Material unitMaterial;
    [Header("Movement")]
    public Transform targetTransform;
    public float minMoveSpeed = 10.0f;
    public float maxMoveSpeed = 20.0f;
    public float rotateSpeed = 10.0f;

    [Header("Optimization")]
    public float cellSize = 2.0f;       
    public float avoidanceRadius = 1.5f; 
    public float avoidanceWeight = 2.0f; 
    
    [Header("Boundary Settings")]
    public Vector3 tankSize = new Vector3(50, 30, 50);
    public float boundaryForce = 5.0f;
    
    public bool hasFood = false; 
    public int gatheredCount = 0;
    public float eatingRadius = 3.0f;

    public struct UnitData { public Matrix4x4 objectToWorld; public float animOffset; public float pad1, pad2, pad3; }
    private ComputeBuffer unitBuffer;
    private List<ComputeBuffer> argsBuffers = new List<ComputeBuffer>();
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    private Bounds renderBounds;
    private NativeArray<float3> positions;
    private NativeArray<quaternion> rotations;
    private NativeArray<UnitData> unitDataArray;
    private NativeArray<float> moveSpeeds; 
    private NativeParallelMultiHashMap<int, int> gridMap;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;
    }

    void Start()
    {
        InitializeBuffers();
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, tankSize * 2);
    }

    void Update()
    {
        if (unitMeshes == null || unitMeshes.Length == 0 || unitMaterial == null) return;

        float3 targetPos = targetTransform != null ? (float3)targetTransform.position : float3.zero;

        gridMap.Clear();

        UpdateGridJob gridJob = new UpdateGridJob
        {
            positions = positions,
            cellSize = cellSize,
            gridMap = gridMap.AsParallelWriter()
        };
        JobHandle gridHandle = gridJob.Schedule(instanceCount, 64);

        UnitMoveJob moveJob = new UnitMoveJob
        {
            deltaTime = Time.deltaTime,
            targetPosition = targetPos,
            isFeeding = hasFood ? 1 : 0, 
            time = Time.time,            
            moveSpeeds = moveSpeeds, 
            rotateSpeed = rotateSpeed,
            cellSize = cellSize,
            avoidanceRadius = avoidanceRadius,
            avoidanceWeight = avoidanceWeight,
            gridMap = gridMap,
            centerPosition = (float3)transform.position, 
            boundarySize = (float3)tankSize,
            boundaryWeight = boundaryForce,
            positions = positions,
            rotations = rotations,
            unitDataBuffer = unitDataArray
        };
        
        JobHandle moveHandle = moveJob.Schedule(instanceCount, 64, gridHandle);
        moveHandle.Complete();
        gatheredCount = 0;
        if (hasFood)
        {
            float r2 = eatingRadius * eatingRadius;
            for (int i = 0; i < instanceCount; i++)
            {
                float distSq = math.distancesq(positions[i], targetPos);
                if (distSq < r2)
                {
                    gatheredCount++;
                }
            }
        }
        
        unitBuffer.SetData(unitDataArray);
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        int currentStartIndex = 0;

        int countPerMesh = instanceCount / unitMeshes.Length;
        int remainder = instanceCount % unitMeshes.Length;

        for (int i = 0; i < unitMeshes.Length; i++)
        {
            if (unitMeshes[i] == null) continue;
            int countForThisMesh = countPerMesh + (i == unitMeshes.Length - 1 ? remainder : 0);
            props.SetFloat("_BaseIndex", (float)currentStartIndex);

            Graphics.DrawMeshInstancedIndirect(
                unitMeshes[i], 
                0, 
                unitMaterial, 
                renderBounds, 
                argsBuffers[i], 
                0, 
                props,
                UnityEngine.Rendering.ShadowCastingMode.Off, 
                true
            );

            currentStartIndex += countForThisMesh;
        }
    }

    private void InitializeBuffers()
    {
         unitBuffer = new ComputeBuffer(instanceCount, Marshal.SizeOf(typeof(UnitData)));
        
        int meshCount = unitMeshes.Length;
        if (meshCount == 0) return;

        int countPerMesh = instanceCount / meshCount;
        int remainder = instanceCount % meshCount;
        int startOffset = 0;

        for (int i = 0; i < meshCount; i++)
        {
            if (unitMeshes[i] == null) continue;
            int currentCount = countPerMesh + (i == meshCount - 1 ? remainder : 0);
            ComputeBuffer newArgsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            args[0] = (uint)unitMeshes[i].GetIndexCount(0);
            args[1] = (uint)currentCount;
            args[2] = (uint)unitMeshes[i].GetIndexStart(0);
            args[3] = (uint)unitMeshes[i].GetBaseVertex(0);
            args[4] = (uint)startOffset;
            newArgsBuffer.SetData(args);
            argsBuffers.Add(newArgsBuffer);
            startOffset += currentCount;
        }

        positions = new NativeArray<float3>(instanceCount, Allocator.Persistent);
        rotations = new NativeArray<quaternion>(instanceCount, Allocator.Persistent);
        unitDataArray = new NativeArray<UnitData>(instanceCount, Allocator.Persistent);
        gridMap = new NativeParallelMultiHashMap<int, int>(instanceCount, Allocator.Persistent);
        moveSpeeds = new NativeArray<float>(instanceCount, Allocator.Persistent);
        
        float3 center = (float3)transform.position;

        var initData = new UnitData[instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            float3 pos = new float3(
                center.x + UnityEngine.Random.Range(-tankSize.x, tankSize.x),
                center.y + UnityEngine.Random.Range(-tankSize.y, tankSize.y),
                center.z + UnityEngine.Random.Range(-tankSize.z, tankSize.z)
            );

            quaternion rot = quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
            positions[i] = pos;
            rotations[i] = rot;
            moveSpeeds[i] = UnityEngine.Random.Range(minMoveSpeed, maxMoveSpeed);
            initData[i].objectToWorld = Matrix4x4.TRS(pos, rot, Vector3.one);
            initData[i].animOffset = UnityEngine.Random.Range(0f, 100f);
        }
        unitDataArray.CopyFrom(initData);
        unitBuffer.SetData(unitDataArray);
        unitMaterial.SetBuffer("_UnitBuffer", unitBuffer);
        renderBounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
    }

    private void OnDisable()
    {
        if (positions.IsCreated) positions.Dispose();
        if (rotations.IsCreated) rotations.Dispose();
        if (unitDataArray.IsCreated) unitDataArray.Dispose();
        if (gridMap.IsCreated) gridMap.Dispose();
        if (moveSpeeds.IsCreated) moveSpeeds.Dispose();
        if (unitBuffer != null) unitBuffer.Release();
        foreach (var buffer in argsBuffers) if (buffer != null) buffer.Release();
        argsBuffers.Clear();
    }
}