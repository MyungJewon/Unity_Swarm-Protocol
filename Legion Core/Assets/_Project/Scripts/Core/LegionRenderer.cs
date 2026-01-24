using UnityEngine;
using System.Runtime.InteropServices;

public class LegionRenderer : MonoBehaviour
{
    [Header("Settings")]
    public int instanceCount = 10000;
    public Mesh unitMesh;
    public Material unitMaterial;
    public Vector2 spawnArea = new Vector2(100, 100);

    struct UnitData
    {
        public Matrix4x4 objectToWorld;
        public float animOffset;
    }
    private ComputeBuffer unitBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    private Bounds renderBounds;

    void Start()
    {
        InitializeBuffers();
    }
    void Update()
    {
        if (unitMesh == null || unitMaterial == null) return;

        Graphics.DrawMeshInstancedIndirect(
            unitMesh, 
            0, 
            unitMaterial, 
            renderBounds, 
            argsBuffer, 
            0, 
            null, 
            UnityEngine.Rendering.ShadowCastingMode.Off,
            true
        );
    }

    private void InitializeBuffers()
    {
        unitBuffer = new ComputeBuffer(instanceCount, Marshal.SizeOf(typeof(UnitData)));
        UnitData[] data = new UnitData[instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-spawnArea.x, spawnArea.x), 
                0, 
                Random.Range(-spawnArea.y, spawnArea.y)
            );
            
            Quaternion rot = Quaternion.Euler(0, Random.Range(0, 360), 0);
            Vector3 scale = Vector3.one;
            data[i].objectToWorld = Matrix4x4.TRS(pos, rot, scale);
            data[i].animOffset = Random.Range(0f, 100f);
        }
        unitBuffer.SetData(data);
        
        unitMaterial.SetBuffer("_UnitBuffer", unitBuffer);

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        args[0] = (uint)unitMesh.GetIndexCount(0);
        args[1] = (uint)instanceCount;
        args[2] = (uint)unitMesh.GetIndexStart(0);
        args[3] = (uint)unitMesh.GetBaseVertex(0);
        args[4] = 0;
        argsBuffer.SetData(args);

        renderBounds = new Bounds(Vector3.zero, new Vector3(spawnArea.x * 2, 100, spawnArea.y * 2));
    }

    private void OnDisable()
    {
        if (unitBuffer != null) unitBuffer.Release();
        if (argsBuffer != null) argsBuffer.Release();
        unitBuffer = null;
        argsBuffer = null;
    }
}