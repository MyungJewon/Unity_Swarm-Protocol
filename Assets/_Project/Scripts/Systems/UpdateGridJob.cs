using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct UpdateGridJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> positions;
    [ReadOnly] public float cellSize;
    
    [WriteOnly] public NativeParallelMultiHashMap<int, int>.ParallelWriter gridMap;

    public void Execute(int index)
    {
        float3 pos = positions[index];
        
        int3 cell = new int3(
            (int)math.floor(pos.x / cellSize), 
            (int)math.floor(pos.y / cellSize), 
            (int)math.floor(pos.z / cellSize)
        );

        int hash = (cell.x * 73856093) ^ (cell.y * 19349663) ^ (cell.z * 83492791);

        gridMap.Add(hash, index);
    }
}