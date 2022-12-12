using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public ChunkCoord coord;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    GameObject chunkObject;
    World world;

    int vertexIndex = 0;
    // List is not he efficient way of doing this, but it is the easiest
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    // Stores voxel data
    public byte[,,] voxelMap = new byte[VoxelData.ChunkWidth,
                                VoxelData.ChunkHeight,
                                VoxelData.ChunkWidth];

    // Constructor
    public Chunk(ChunkCoord _coord, World _world)
    {
        coord = _coord;
        world = _world;

        chunkObject = new GameObject();
        isActive = false;

        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshRenderer.material = world.material;
        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth,
                                                     0f,
                                                     coord.z * VoxelData.ChunkWidth);
        chunkObject.name = "Chunk: " + coord.x + "," + coord.z;
        PopulateVoxelMap();
        CreateMeshData();
        CreateMesh();
    }

    //Populates the voxels within a chunk
    void PopulateVoxelMap()
    {
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    voxelMap[x, y, z] = world.GetVoxel(new Vector3(x, y, z) + position);
                }
            }
        }
    }

    void CreateMeshData()
    {
        // Layer from bottom up.
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    if (world.blockTypes[voxelMap[x, y, z]].isSolid)
                        AddVoxelDataToChunk(new Vector3(x, y, z));
                }
            }
        }
    }

    // 0 is within chunk, not within worldspace
    bool IsVoxelInChunk(int x, int y, int z)
    {

        if (x < 0 || x > VoxelData.ChunkWidth - 1 ||
            y < 0 || y > VoxelData.ChunkHeight - 1 ||
            z < 0 || z > VoxelData.ChunkWidth - 1)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    // position - coordinate of the voxel
    bool CheckVoxel(Vector3 pos)
    {
        // Always round down to int. 
        // Hardcasting, (int)position, can cause issues.
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (!IsVoxelInChunk(x, y, z))
        {
            return world.blockTypes[world.GetVoxel(pos + position)].isSolid;
        }

        return world.blockTypes[voxelMap[x, y, z]].isSolid;
    }

    // position - coordinate of the voxel
    void AddVoxelDataToChunk(Vector3 position)
    {
        // Loop through each face (6 faces per block)
        for (int currentFace = 0; currentFace < 6; currentFace++)
        {
            // Check surrouding area. If a block is covering this face, dont draw it
            // Otherwise this face is exposed, draw it

            Vector3 nextVoxelToCheck = position + VoxelData.checkFaces[currentFace];
            if (!CheckVoxel(nextVoxelToCheck))
            {
                // Populate the four corners of the face of the block.
                vertices.Add(position + VoxelData.voxelVertices[VoxelData.voxelTriangles[currentFace, 0]]);
                vertices.Add(position + VoxelData.voxelVertices[VoxelData.voxelTriangles[currentFace, 1]]);
                vertices.Add(position + VoxelData.voxelVertices[VoxelData.voxelTriangles[currentFace, 2]]);
                vertices.Add(position + VoxelData.voxelVertices[VoxelData.voxelTriangles[currentFace, 3]]);

                byte blockID = voxelMap[(int)position.x, (int)position.y, (int)position.z];

                AddTexture(world.blockTypes[blockID].GetTextureID(currentFace));

                // Populate the six vertices to draw the 2 triangles
                // Two vertices must overlap to get a crisp edge
                triangles.Add(vertexIndex + 0);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);

                // Increment the vertex index
                vertexIndex += 4;
            }
        }
    }

    void CreateMesh()
    {
        // Build Mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        // Recalc Normals because each vertice has a direction (3 directions per vertice)
        // This is necessary to draw cube, calculate lighting, etc
        mesh.RecalculateNormals();

        // Update the mesh filter
        meshFilter.mesh = mesh;
    }

    public bool isActive
    {
        get
        {
#if DEBUG
            if (chunkObject == null)
                Debug.LogError($"Chunk ({coord.x}, {coord.z})'s {nameof(isActive)} property was accessed (read) while the chunkObject was null");
#endif
            return chunkObject.activeSelf;
        }
        set
        {
#if DEBUG
            if (chunkObject == null)
                Debug.LogError($"Chunk ({coord.x}, {coord.z})'s {nameof(isActive)} property was accessed (write) while the chunkObject was null");
#endif
            //TODO: This will most likely need a threadlock (lock) on the adding/removing for enumerator iteration in other threads.
            if (!value && world.activeChunks.Contains(this.coord))
                world.activeChunks.Remove(this.coord);
            else if (value && !world.activeChunks.Contains(this.coord))
                world.activeChunks.Add(this.coord);
            chunkObject.SetActive(value);
        }
    }

    // Allows us to get data within a chunk
    // We could do ChunkObject.transform.position...
    // But this is simpler
    public Vector3 position
    {
        get { return chunkObject.transform.position; }
    }

    // textureID - where a texture occurs in the atlas
    // NOT BLOCK ID - blocks like Grass/Wood have different texture depending on face.
    void AddTexture(int textureID)
    {
        float y = textureID / VoxelData.textureAtlasSizeInBlocks;
        float x = textureID - (y * VoxelData.textureAtlasSizeInBlocks);
        x *= VoxelData.NormalizedBlockTextureSize;
        y *= VoxelData.NormalizedBlockTextureSize;

        //If texture atlas start from top left
        y = 1f - y - VoxelData.NormalizedBlockTextureSize;

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureSize));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y + VoxelData.NormalizedBlockTextureSize));
    }
}

// The position of chunk we are drawing
// Not 0,16,32...
// BUt 0,1,2... 
public class ChunkCoord
{
    public int x;
    public int z;

    public ChunkCoord(int _x, int _z)
    {
        x = _x;
        z = _z;
    }

    public bool Equals(ChunkCoord coordToCompare)
    {
        if (coordToCompare == null)
        {
            return false;
        }
        else if (coordToCompare.x == x && coordToCompare.z == z)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}