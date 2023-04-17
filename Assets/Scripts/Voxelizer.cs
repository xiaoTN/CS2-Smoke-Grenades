using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxelizer : MonoBehaviour {
    public Vector3 boundsExtent = new Vector3(3, 3, 3);

    public float voxelSize = 0.25f;

    public Mesh debugMesh;

    public bool debugVoxels = true;

    [Range(0, 10)]
    public float maxRadius = 1.0f;

    public bool restartAnimation = false;

    private ComputeBuffer voxelsBuffer, argsBuffer;

    private ComputeShader voxelizeCompute;

    private Material debugVoxelMaterial;

    private Bounds debugBounds;

    private int voxelsX, voxelsY, voxelsZ;

    private float radius;

    void OnEnable() {
        radius = 0.0f;
        debugVoxelMaterial = new Material(Shader.Find("Hidden/VisualizeVoxels"));
        voxelizeCompute = (ComputeShader)Resources.Load("Voxelize");

        Vector3 boundsSize = boundsExtent * 2;
        debugBounds = new Bounds(new Vector3(0, boundsExtent.y, 0), boundsSize);

        voxelsX = Mathf.CeilToInt(boundsSize.x / voxelSize);
        voxelsY = Mathf.CeilToInt(boundsSize.y / voxelSize);
        voxelsZ = Mathf.CeilToInt(boundsSize.z / voxelSize);
        
        // Debug.LogFormat("Voxels X: {0}", voxelsX);
        // Debug.LogFormat("Voxels Y: {0}", voxelsY);
        // Debug.LogFormat("Voxels Z: {0}", voxelsZ);
        // Debug.LogFormat("Total Voxels: {0}", voxelsX * voxelsY * voxelsZ);

        voxelsBuffer = new ComputeBuffer((voxelsX * voxelsY * voxelsZ), 4);
        voxelizeCompute.SetBuffer(0, "_Voxels", voxelsBuffer);
        voxelizeCompute.SetVector("_VoxelResolution", new Vector3(voxelsX, voxelsY, voxelsZ));
        voxelizeCompute.SetVector("_BoundsExtent", boundsExtent);
        voxelizeCompute.SetFloat("_Radius", radius);

        voxelizeCompute.Dispatch(0, Mathf.CeilToInt((voxelsX * voxelsY * voxelsZ) / 128.0f), 1, 1);


        
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = (uint)debugMesh.GetIndexCount(0);
        args[1] = (uint)(voxelsX * voxelsY * voxelsZ);
        args[2] = (uint)debugMesh.GetIndexStart(0);
        args[3] = (uint)debugMesh.GetBaseVertex(0);
        argsBuffer.SetData(args);
    }

    float Easing(float x) {
        return Mathf.Sin((radius * Mathf.PI) / 2);
    }

    void Update() {
        if (radius < 1.0f) {
            radius += 2 * Time.deltaTime;
        }

        if (restartAnimation) {
            radius = 0.0f;
            restartAnimation = false;
        }

        if (debugVoxels) {
            debugVoxelMaterial.SetBuffer("_Voxels", voxelsBuffer);
            debugVoxelMaterial.SetVector("_VoxelResolution", new Vector3(voxelsX, voxelsY, voxelsZ));
            debugVoxelMaterial.SetVector("_BoundsExtent", boundsExtent);
            debugVoxelMaterial.SetFloat("_VoxelSize", voxelSize);

            
            voxelizeCompute.SetBuffer(0, "_Voxels", voxelsBuffer);
            voxelizeCompute.SetVector("_VoxelResolution", new Vector3(voxelsX, voxelsY, voxelsZ));
            voxelizeCompute.SetVector("_BoundsExtent", boundsExtent);
            voxelizeCompute.SetFloat("_Radius", Mathf.Lerp(0.0f, maxRadius, Easing(radius)));
            voxelizeCompute.Dispatch(0, Mathf.CeilToInt((voxelsX * voxelsY * voxelsZ) / 128.0f), 1, 1);

            Graphics.DrawMeshInstancedIndirect(debugMesh, 0, debugVoxelMaterial, debugBounds, argsBuffer);
        }
    }

    void OnDisable() {
        voxelsBuffer.Release();
        argsBuffer.Release();
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(debugBounds.center, debugBounds.extents * 2);
    }
}
