using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static BlockType;

[CreateAssetMenu(fileName = "New Voxel Mesh Data", menuName = "Custom Menu/Voxel Mesh Data")]
public class VoxelMeshBuilder : ScriptableObject 
{
    public FaceMeshData[] faces = new FaceMeshData[6]; // No matter the shape, there will only be 6 faces. Top "faces" of a stair are still 1 face.
    static int[] trianglesOne = new int[6] { 0, 1, 3, 3, 1, 2 };
    static int[] trianglesTwo = new int[6] { 0, 3, 1, 1, 3, 2 };

    public VoxelMeshBuilder(FaceMeshData[] _faces)
    {
        faces = _faces;
    }
    public static VoxelMeshBuilder GenerateBlockMesh(eBlockMeshTypes meshType)
    {
        switch(meshType)
        {
            case eBlockMeshTypes.HALF_SLAB_BLOCK:
                return HalfSlabBlockMesh();
            case eBlockMeshTypes.STAIR_BLOCK:
                return StandardBlockMesh();
            case eBlockMeshTypes.STANDARD_BLOCK:
            default:
                return StandardBlockMesh();
        }
    }

    public static VoxelMeshBuilder HalfSlabBlockMesh()
    {
        // Back Face
        VertData[] backFaceVertData = new VertData[4];
        backFaceVertData[0] = new VertData(new Vector3(0, 0, 0), new Vector2(0, 0));
        backFaceVertData[1] = new VertData(new Vector3(0, 0.5f, 0), new Vector2(0, 0.5f));
        backFaceVertData[2] = new VertData(new Vector3(1, 0.5f, 0), new Vector2(1, 0.5f));
        backFaceVertData[3] = new VertData(new Vector3(1, 0, 0), new Vector2(1, 0));
        // Front Face
        VertData[] frontFaceVertData = new VertData[4];
        frontFaceVertData[0] = new VertData(new Vector3(0, 0, 1), new Vector2(0, 0));
        frontFaceVertData[1] = new VertData(new Vector3(0, 0.5f, 1), new Vector2(0, 0.5f));
        frontFaceVertData[2] = new VertData(new Vector3(1, 0.5f, 1), new Vector2(1, 0.5f));
        frontFaceVertData[3] = new VertData(new Vector3(1, 0, 1), new Vector2(1, 0));
        // Top Face
        VertData[] topFaceVertData = new VertData[4];
        topFaceVertData[0] = new VertData(new Vector3(0, 0.5f, 0), new Vector2(0, 0));
        topFaceVertData[1] = new VertData(new Vector3(0, 0.5f, 1), new Vector2(0, 1));
        topFaceVertData[2] = new VertData(new Vector3(1, 0.5f, 1), new Vector2(1, 1));
        topFaceVertData[3] = new VertData(new Vector3(1, 0.5f, 0), new Vector2(1, 0));
        // Bottom Face
        VertData[] bottomFaceVertData = new VertData[4];
        bottomFaceVertData[0] = new VertData(new Vector3(0, 0, 0), new Vector2(0, 0));
        bottomFaceVertData[1] = new VertData(new Vector3(0, 0, 1), new Vector2(0, 1));
        bottomFaceVertData[2] = new VertData(new Vector3(1, 0, 1), new Vector2(1, 1));
        bottomFaceVertData[3] = new VertData(new Vector3(1, 0, 0), new Vector2(1, 0));
        // Left Face
        VertData[] leftFaceVertData = new VertData[4];
        leftFaceVertData[0] = new VertData(new Vector3(0, 0, 0), new Vector2(0, 0));
        leftFaceVertData[1] = new VertData(new Vector3(0, 0.5f, 0), new Vector2(0, 0.5f));
        leftFaceVertData[2] = new VertData(new Vector3(0, 0.5f, 1), new Vector2(1, 0.5f));
        leftFaceVertData[3] = new VertData(new Vector3(0, 0, 1), new Vector2(1, 0));
        // Right Face
        VertData[] rightFaceVertData = new VertData[4];
        rightFaceVertData[0] = new VertData(new Vector3(1, 0, 0), new Vector2(0, 0));
        rightFaceVertData[1] = new VertData(new Vector3(1, 0.5f, 0), new Vector2(0, 0.5f));
        rightFaceVertData[2] = new VertData(new Vector3(1, 0.5f, 1), new Vector2(1, 0.5f));
        rightFaceVertData[3] = new VertData(new Vector3(1, 0, 1), new Vector2(1, 0));

        FaceMeshData[] faces = new FaceMeshData[6];
        faces[0] = new FaceMeshData(new Vector3(0, 0, -1), backFaceVertData, trianglesOne);
        faces[1] = new FaceMeshData(new Vector3(1, 0, 0), frontFaceVertData, trianglesTwo);
        faces[2] = new FaceMeshData(new Vector3(0, 1, 0), topFaceVertData, trianglesOne);
        faces[3] = new FaceMeshData(new Vector3(0, -1, 0), bottomFaceVertData, trianglesTwo);
        faces[4] = new FaceMeshData(new Vector3(-1, 0, 0), leftFaceVertData, trianglesTwo);
        faces[5] = new FaceMeshData(new Vector3(1, 0, 0), rightFaceVertData, trianglesOne);

        return new VoxelMeshBuilder(faces);
    }

    public static VoxelMeshBuilder StandardBlockMesh()
    {
        // Back Face
        VertData[] backFaceVertData = new VertData[4];
        backFaceVertData[0] = new VertData(new Vector3(0, 0, 0), new Vector2(0, 0));
        backFaceVertData[1] = new VertData(new Vector3(0, 1, 0), new Vector2(0, 1));
        backFaceVertData[2] = new VertData(new Vector3(1, 1, 0), new Vector2(1, 1));
        backFaceVertData[3] = new VertData(new Vector3(1, 0, 0), new Vector2(1, 0));
        // Front Face
        VertData[] frontFaceVertData = new VertData[4];
        frontFaceVertData[0] = new VertData(new Vector3(0, 0, 1), new Vector2(0, 0));
        frontFaceVertData[1] = new VertData(new Vector3(0, 1, 1), new Vector2(0, 1));
        frontFaceVertData[2] = new VertData(new Vector3(1, 1, 1), new Vector2(1, 1));
        frontFaceVertData[3] = new VertData(new Vector3(1, 0, 1), new Vector2(1, 0));
        // Top Face
        VertData[] topFaceVertData = new VertData[4];
        topFaceVertData[0] = new VertData(new Vector3(0, 1, 0), new Vector2(0, 0));
        topFaceVertData[1] = new VertData(new Vector3(0, 1, 1), new Vector2(0, 1));
        topFaceVertData[2] = new VertData(new Vector3(1, 1, 1), new Vector2(1, 1));
        topFaceVertData[3] = new VertData(new Vector3(1, 1, 0), new Vector2(1, 0));
        // Bottom Face
        VertData[] bottomFaceVertData = new VertData[4];
        bottomFaceVertData[0] = new VertData(new Vector3(0, 0, 0), new Vector2(0, 0));
        bottomFaceVertData[1] = new VertData(new Vector3(0, 0, 1), new Vector2(0, 1));
        bottomFaceVertData[2] = new VertData(new Vector3(1, 0, 1), new Vector2(1, 1));
        bottomFaceVertData[3] = new VertData(new Vector3(1, 0, 0), new Vector2(1, 0));
        // Left Face
        VertData[] leftFaceVertData = new VertData[4];
        leftFaceVertData[0] = new VertData(new Vector3(0, 0, 0), new Vector2(0, 0));
        leftFaceVertData[1] = new VertData(new Vector3(0, 1, 0), new Vector2(0, 1));
        leftFaceVertData[2] = new VertData(new Vector3(0, 1, 1), new Vector2(1, 1));
        leftFaceVertData[3] = new VertData(new Vector3(0, 0, 1), new Vector2(1, 0));
        // Right Face
        VertData[] rightFaceVertData = new VertData[4];
        rightFaceVertData[0] = new VertData(new Vector3(1, 0, 0), new Vector2(0, 0));
        rightFaceVertData[1] = new VertData(new Vector3(1, 1, 0), new Vector2(0, 1));
        rightFaceVertData[2] = new VertData(new Vector3(1, 1, 1), new Vector2(1, 1));
        rightFaceVertData[3] = new VertData(new Vector3(1, 0, 1), new Vector2(1, 0));

        FaceMeshData[] faces = new FaceMeshData[6];
        faces[0] = new FaceMeshData(new Vector3(0, 0, -1), backFaceVertData, trianglesOne);
        faces[1] = new FaceMeshData(new Vector3(1, 0, 0), frontFaceVertData, trianglesTwo);
        faces[2] = new FaceMeshData(new Vector3(0, 1, 0), topFaceVertData, trianglesOne);
        faces[3] = new FaceMeshData(new Vector3(0, -1, 0), bottomFaceVertData, trianglesTwo);
        faces[4] = new FaceMeshData(new Vector3(-1, 0, 0), leftFaceVertData, trianglesTwo);
        faces[5] = new FaceMeshData(new Vector3(1, 0, 0), rightFaceVertData, trianglesOne);

        return new VoxelMeshBuilder(faces);
    }
}

[System.Serializable]
public class VertData
{
    public Vector3 position;
    public Vector2 uv;

    public VertData(Vector3 _position, Vector2 _uv)
    {
        position = _position;
        uv = _uv;   
    }
}

[System.Serializable]
public class FaceMeshData
{
    public Vector3 normal;
    public VertData[] vertData;
    public int[] triangles;

    public FaceMeshData(Vector3 _normal, VertData[] _vertData, int[] _triangles)
    {
        normal = _normal;
        vertData = _vertData;
        triangles = _triangles;
    }               
}