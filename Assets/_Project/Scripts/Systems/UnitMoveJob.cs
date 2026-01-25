using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct UnitMoveJob : IJobParallelFor
{
    [ReadOnly] public float deltaTime;
    [ReadOnly] public float3 targetPosition;
    [ReadOnly] public int isFeeding;
    [ReadOnly] public float time; 
    [ReadOnly] public NativeArray<float> moveSpeeds; 
    [ReadOnly] public float rotateSpeed;
    [ReadOnly] public float3 boundarySize;
    [ReadOnly] public float boundaryWeight;
    [ReadOnly] public float cellSize;
    [ReadOnly] public float avoidanceRadius;
    [ReadOnly] public float avoidanceWeight;
    [ReadOnly] public NativeParallelMultiHashMap<int, int> gridMap;
    [NativeDisableParallelForRestriction] public NativeArray<float3> positions;
    [NativeDisableParallelForRestriction] public NativeArray<quaternion> rotations;
    [NativeDisableParallelForRestriction] public NativeArray<LegionRenderer.UnitData> unitDataBuffer;

    public void Execute(int index)
    {
        float3 currentPos = positions[index];
        quaternion currentRot = rotations[index];
        float mySpeed = moveSpeeds[index];

        float3 moveDir;
        if (isFeeding == 1)
        {
            float3 toTarget = targetPosition - currentPos;
            moveDir = math.normalizesafe(toTarget);
        }
        else
        {
            float noiseOffset = index * 0.1f;
            float dirX = math.sin(time * 0.5f + noiseOffset);
            float dirZ = math.cos(time * 0.3f + noiseOffset);
            float dirY = math.sin(time * 0.2f + noiseOffset) * 0.2f;
            moveDir = math.normalizesafe(new float3(dirX, dirY, dirZ));
        }

        float3 boundaryForce = float3.zero;
        bool isOutside = false;

        if (currentPos.x > boundarySize.x) { boundaryForce.x = -1; isOutside = true; }
        else if (currentPos.x < -boundarySize.x) { boundaryForce.x = 1; isOutside = true; }

        if (currentPos.y > boundarySize.y) { boundaryForce.y = -1; isOutside = true; }
        else if (currentPos.y < -boundarySize.y) { boundaryForce.y = 1; isOutside = true; }

        if (currentPos.z > boundarySize.z) { boundaryForce.z = -1; isOutside = true; }
        else if (currentPos.z < -boundarySize.z) { boundaryForce.z = 1; isOutside = true; }

        if (isOutside)
        {
            moveDir += boundaryForce * boundaryWeight;
            moveDir = math.normalizesafe(moveDir);
        } 
        float3 separation = float3.zero;
        int neighborCount = 0;
        int3 cellCoords = GetCellCoords(currentPos, cellSize);
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    int3 neighborCell = cellCoords + new int3(x, y, z);
                    int hashKey = GetHash(neighborCell);
                    if (gridMap.TryGetFirstValue(hashKey, out int neighborIndex, out NativeParallelMultiHashMapIterator<int> it))
                    {
                        do
                        {
                            if (neighborIndex == index) continue;
                            float3 neighborPos = positions[neighborIndex];
                            float3 offset = currentPos - neighborPos;
                            float distSq = math.lengthsq(offset);
                            if (distSq < avoidanceRadius * avoidanceRadius && distSq > 0.001f)
                            {
                                float dist = math.sqrt(distSq);
                                float strength = 1.0f - (dist / avoidanceRadius);
                                separation += math.normalize(offset) * strength;
                                neighborCount++;
                            }
                        } while (gridMap.TryGetNextValue(out neighborIndex, ref it));
                    }
                }
            }
        }
        if (neighborCount > 0)
        {
            moveDir += separation * avoidanceWeight;
            moveDir = math.normalizesafe(moveDir);
        }
        float finalSpeed = (isFeeding == 1) ? mySpeed : mySpeed * 0.5f;
        if (isOutside) finalSpeed *= 1.5f;
        currentPos += moveDir * finalSpeed * deltaTime;
        if (math.lengthsq(moveDir) > 0.001f)
        {
            quaternion lookRot = quaternion.LookRotation(moveDir, math.up());
            quaternion correction = quaternion.RotateY(math.radians(90.0f)); 
            quaternion targetRot = math.mul(lookRot, correction);
            currentRot = math.slerp(currentRot, targetRot, rotateSpeed * deltaTime);
        }

        positions[index] = currentPos;
        rotations[index] = currentRot;

        LegionRenderer.UnitData data = unitDataBuffer[index];
        data.objectToWorld = float4x4.TRS(currentPos, currentRot, new float3(1, 1, 1));
        unitDataBuffer[index] = data;
    }
    private int3 GetCellCoords(float3 pos, float cell_size) { return new int3((int)math.floor(pos.x / cell_size), (int)math.floor(pos.y / cell_size), (int)math.floor(pos.z / cell_size)); }
    private int GetHash(int3 cell) { return (cell.x * 73856093) ^ (cell.y * 19349663) ^ (cell.z * 83492791); }
}