using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using static BlockType;

public class Chunk
{
    public ChunkCoord coord;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    public GameObject chunkObject;
    World world;


    [Flags]
    public enum eChunkFlags
    {
        CREATING,
        CREATED,
        INITIALIZING,
        INITIALIZED,
        GENERATING,
        GENERATED,
        DECORATING,
        DECORATED
    }

    public eChunkFlags ChunkFlags;

    public void SetChunkFlag(eChunkFlags flag)
    {
        ChunkFlags |= flag;
    }
    public void ClearChunkFlag(eChunkFlags flag)
    {
        ChunkFlags &= ~flag;
    }
    public bool IsChunkFlagSet(eChunkFlags flag)
    {
        return (ChunkFlags & flag) == flag;
    }

    public bool IsCreating => IsChunkFlagSet(eChunkFlags.CREATING);
    public bool IsCreated => IsChunkFlagSet(eChunkFlags.CREATED);
    public bool IsInitializing => IsChunkFlagSet(eChunkFlags.INITIALIZING);
    public bool IsInitialized => IsChunkFlagSet(eChunkFlags.INITIALIZED);
    public bool IsGenerating => IsChunkFlagSet(eChunkFlags.GENERATING);
    public bool IsGenerated => IsChunkFlagSet(eChunkFlags.GENERATED);
    public bool IsDecorating => IsChunkFlagSet(eChunkFlags.DECORATING);
    public bool IsDecorated => IsChunkFlagSet(eChunkFlags.DECORATED);


    public void SetChunkActive(bool active)
    {
        Assert.IsTrue(chunkObject != null, $"SetChunkActive on NULL_CHUNK_GO_{coord.ToString()}");
        isActive = active;
    }

    int vertexIndex = 0;
    // List is not he efficient way of doing this, but it is the easiest
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    Material[] materials = new Material[2];
    List<Vector2> uvs = new List<Vector2>();
    List<Vector3> normals = new List<Vector3>();

    // Stores voxel data
    public byte[,,] voxelMap = new byte[VoxelData.ChunkWidth,
                                VoxelData.ChunkHeight,
                                VoxelData.ChunkWidth];

    public Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    public struct chunkUpdateThreadData
    {
        public Vector3 ChunkPosition;
        public bool Valid;

        public chunkUpdateThreadData(bool valid = false)
        {
            ChunkPosition = Vector3.zero;
            Valid = valid;
        }
    }

    // Constructor
    public Chunk(ChunkCoord _coord, World _world, bool generateOnLoad)
    {
        SetChunkFlag(eChunkFlags.CREATING);

        coord = _coord;
        world = _world;

        //Get the modifications
        //if (world.modifications.ContainsKey(this.coord))
        //{
        //    for (int i = 0; i < world.modifications[this.coord].Count; i++)
        //        this.modifications.Enqueue(world.modifications[this.coord].Dequeue());
        //    world.modifications.Remove(this.coord);
        //}

        SetChunkFlag(eChunkFlags.CREATED);
        ClearChunkFlag(eChunkFlags.CREATING);

        if (generateOnLoad)
            Init(true, true);
    }

    public void Init(bool populate = true, bool update = true)
    {
        SetChunkFlag(eChunkFlags.INITIALIZING);
        chunkObject = new GameObject();
        SetChunkActive(_isActive);

        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        materials[0] = world.material;
        materials[1] = world.transparentMaterial;

        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth,
                                                     0f,
                                                     coord.z * VoxelData.ChunkWidth);

        chunkObject.name = "Chunk: " + coord.x + "," + coord.z;

        ClearChunkFlag(eChunkFlags.INITIALIZING);
        SetChunkFlag(eChunkFlags.INITIALIZED);

        if (populate)
            PopulateVoxelMap();
        if (update)
            UpdateChunk();
    }



    //Populates the voxels within a chunk
    public void PopulateVoxelMap(bool isThreadedCall = false)
    {
        SetChunkFlag(eChunkFlags.GENERATING);

        var positionToUse = new Vector3(coord.x * VoxelData.ChunkWidth, 0, coord.z * VoxelData.ChunkWidth);
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    voxelMap[x, y, z] = world.GetVoxel(new Vector3(x, y, z) + positionToUse);
                }

            }
        }
        ClearChunkFlag(eChunkFlags.GENERATING);
        SetChunkFlag(eChunkFlags.GENERATED);

    }

    public void UpdateChunk(Chunk.chunkUpdateThreadData threadedData = default)
    {

        if (threadedData.Valid)
            Monitor.Enter(this);
        ApplyModifications(threadedData);

        ClearMeshData();

        // Layer from bottom up.
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    if (world.blockTypes[voxelMap[x, y, z]].isSolid)
                        UpdateMeshData(new Vector3(x, y, z), threadedData);
                }
            }
        }

        CreateMesh(threadedData);


        if (threadedData.Valid)
            Monitor.Exit(this);
    }
    public void ApplyModifications(Chunk.chunkUpdateThreadData threadedData = default)
    {
        SetChunkFlag(eChunkFlags.DECORATING);
        Vector3 threaded_chunkPosition = Vector3.zero;
        if (threadedData.Valid) //This will only ever execute if isThreadedCall is TRUE
            threaded_chunkPosition = threadedData.ChunkPosition;

        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();
            Vector3 pos = v.position -= (threadedData.Valid)
                ? threaded_chunkPosition
                : position;
            voxelMap[(int)pos.x, (int)pos.y, (int)pos.z] = v.id;
        }
        ClearChunkFlag(eChunkFlags.DECORATING);
        SetChunkFlag(eChunkFlags.DECORATED);
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

    public void EditVoxel(Vector3 pos, byte newID)
    {

        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(chunkObject.transform.position.x);
        zCheck -= Mathf.FloorToInt(chunkObject.transform.position.z);

        voxelMap[xCheck, yCheck, zCheck] = newID;


        // Update Surrounding Chunks
        UpdateSurroundingVoxels(xCheck, yCheck, zCheck);

        UpdateChunk(default);

    }

    void UpdateSurroundingVoxels(int x, int y, int z)
    {
        Vector3 thisVoxel = new Vector3(x, y, z);

        for (int currentFace = 0; currentFace < 6; currentFace++)
        {
            Vector3 currentVoxel = thisVoxel + VoxelData.checkFaces[currentFace];

            if (!IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z))
            {
                /*
                 * We have the block position that we are editing (thisVoxel), so to get the neighbouring chunk in the direction of the currentFace iteration, we
                 * simply add together the position of the block we are editing to our chunk's position and then add the face direction.  This will give us the
                 * neighbouring block position (next to thisVoxel) that resides in the neighbouring chunk.  We then get the neighbouring chunk and if the 
                 * chunk is valid (not out of world bounds), then we update it.
                */
                var targetChunk = world.GetChunkFromVector3(thisVoxel + position + VoxelData.checkFaces[currentFace]);

                //Chunk could be null if the position given is out of the world bounds.
                if (targetChunk != null)
                    targetChunk.UpdateChunk(); //hopefully this isn't called more than once on the same chunk, otherwise we'd be wasting cpu time & resources.
            }
        }
    }

    // position - coordinate of the voxel
    bool CheckVoxelTransparency(Vector3 pos, Chunk.chunkUpdateThreadData threadedData = default)
    {
        if (threadedData.Valid)
        {

        }
        // Always round down to int. 
        // Hardcasting, (int)position, can cause issues.
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (!IsVoxelInChunk(x, y, z))
        {
            if (threadedData.Valid)
            {
                return world.CheckIfVoxelTransparent(pos + threadedData.ChunkPosition, threadedData);

            }
            else
                return world.CheckIfVoxelTransparent(pos + position);

        }

        return world.blockTypes[voxelMap[x, y, z]].isTransparent;
    }

    // position - coordinate of the voxel
    bool CheckVoxelStandard(Vector3 pos, Chunk.chunkUpdateThreadData threadedData = default)
    {
        if (threadedData.Valid)
        {

        }
        // Always round down to int. 
        // Hardcasting, (int)position, can cause issues.
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (!IsVoxelInChunk(x, y, z))
        {
            if (threadedData.Valid)
            {
                return world.CheckIfVoxelStandard(pos + threadedData.ChunkPosition, threadedData);

            }
            else
                return world.CheckIfVoxelStandard(pos + position);

        }

        return world.blockTypes[voxelMap[x, y, z]].isStandard;
    }

    // use by external scripts
    public byte GetVoxelFromGlobalVector3(Vector3 pos, Chunk.chunkUpdateThreadData threadedData = default)
    {

        if (!world.IsVoxelInWorld(pos))
            return 0; //Air

        var targetChunk = world.GetChunkFromVector3(pos);
        if (targetChunk == null)
            return 0; //Air

        int xMod = Mathf.FloorToInt(pos.x) % VoxelData.ChunkWidth;
        int yMod = Mathf.FloorToInt(pos.y) % VoxelData.ChunkHeight;
        int zMod = Mathf.FloorToInt(pos.z) % VoxelData.ChunkWidth;
        return targetChunk.voxelMap[xMod, yMod, zMod];
    }


    // position - coordinate of the voxel
    void UpdateMeshData(Vector3 position, Chunk.chunkUpdateThreadData threadedData = default)
    {

        byte blockID = voxelMap[(int)position.x, (int)position.y, (int)position.z];
        BlockType currentBlock = world.blockTypes[blockID];
        bool isTransparent = currentBlock.isTransparent;

        // Loop through each face (6 faces per block)
        for (int currentFace = 0; currentFace < 6; currentFace++)
        {
            // Check surrouding area. If a block is covering this face, dont draw it
            // Otherwise this face is exposed, draw it

            Vector3 nextVoxelToCheck = position + VoxelData.checkFaces[currentFace];
            if (CheckVoxelTransparency(nextVoxelToCheck, threadedData) || !CheckVoxelStandard(nextVoxelToCheck, threadedData))
            {
                int faceVertCount = 0;

                for (int i = 0; i < currentBlock.meshData.faces[currentFace].vertData.Length; i++)
                {
                    vertices.Add(position + currentBlock.meshData.faces[currentFace].vertData[i].position);
                    normals.Add(currentBlock.meshData.faces[currentFace].normal);
                    AddTexture(currentBlock.GetTextureID(currentFace), currentBlock.meshData.faces[currentFace].vertData[i].uv);
                    faceVertCount++;
                }

                if (!isTransparent)
                {
                    for (int i = 0; i < currentBlock.meshData.faces[currentFace].triangles.Length; i++)
                    {
                        triangles.Add(vertexIndex + currentBlock.meshData.faces[currentFace].triangles[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < currentBlock.meshData.faces[currentFace].triangles.Length; i++)
                    {
                        transparentTriangles.Add(vertexIndex + currentBlock.meshData.faces[currentFace].triangles[i]);
                    }
                }

                vertexIndex += faceVertCount;
            }
        }
    }

    void CreateMesh(Chunk.chunkUpdateThreadData threadedData = default)
    {

        Action applyMeshData = new Action(() =>
        {
            // Build Mesh
            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.subMeshCount = 2;
            mesh.SetTriangles(triangles.ToArray(), 0);
            mesh.SetTriangles(transparentTriangles.ToArray(), 1);
            mesh.uv = uvs.ToArray();

            // Recalc Normals because each vertice has a direction (3 directions per vertice)
            // This is necessary to draw cube, calculate lighting, etc
            //mesh.RecalculateNormals();
            mesh.normals = normals.ToArray();

            // Update the mesh filter
            meshFilter.mesh = mesh;

        });

        if (threadedData.Valid)
        {
            MainThreadQueue.Result result = new MainThreadQueue.Result();
            Globals.MTQ.RunAction(applyMeshData, result);
            result.Wait();
        }
        else
        {
            applyMeshData();
        }
    }

    void ClearMeshData()
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
        normals.Clear();
    }

    /*
    * #if DEBUG
    if (chunkObject == null)
    Debug.LogError($"Chunk ({coord.x}, {coord.z})'s {nameof(isActive)} property was accessed (read) while the chunkObject was null");
    #endif
    #if DEBUG
    if (chunkObject == null)
    Debug.LogError($"Chunk ({coord.x}, {coord.z})'s {nameof(isActive)} property was accessed (write) while the chunkObject was null");
    #endif
     */

    public bool _isActive = false;
    public bool isActive
    {
        get
        {
            if (chunkObject == null)
                return _isActive;
            else
                return chunkObject.activeSelf;
        }
        set
        {
            _isActive = value;
            if (chunkObject != null)
            {
                chunkObject.SetActive(value);
                if (!value && world.activeChunks.Contains(this.coord))
                    world.activeChunks.Remove(this.coord);
                else if (value && !world.activeChunks.Contains(this.coord))
                    world.activeChunks.Add(this.coord);
            }
        }
    }

    // Allows us to get data within a chunk
    // We could do ChunkObject.transform.position...
    // But this is simpler
    public Vector3 position
    {
        get { return chunkObject.transform.position; }
    }

    public Task<Vector3> GetPositionAsync()
    {
        return Task.Run<Vector3>(() =>
        {
            var result = new MainThreadQueue.Result<Transform>();
            Globals.MTQ.GetTransform(this.chunkObject, result);
            Debug.Log("POS: " + result.Value.position.ToString());
            return result.Value.position;
        });
    }

    // textureID - where a texture occurs in the atlas
    // NOT BLOCK ID - blocks like Grass/Wood have different texture depending on face.
    void AddTexture(int textureID, Vector2 uv)
    {
        float y = textureID / VoxelData.textureAtlasSizeInBlocks;
        float x = textureID - (y * VoxelData.textureAtlasSizeInBlocks);
        x *= VoxelData.NormalizedBlockTextureSize;
        y *= VoxelData.NormalizedBlockTextureSize;

        //If texture atlas start from top left
        y = 1f - y - VoxelData.NormalizedBlockTextureSize;

        x += VoxelData.NormalizedBlockTextureSize * uv.x;
        y += VoxelData.NormalizedBlockTextureSize * uv.y;

        uvs.Add(new Vector2(x, y));
        //uvs.Add(new Vector2(x, y));
        //uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureSize));
        //uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));
        //uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y + VoxelData.NormalizedBlockTextureSize));
    }
}

// The position of chunk we are drawing
// Not 0,16,32...
// BUt 0,1,2... 
public class ChunkCoord : IEquatable<ChunkCoord>
{
    public int x;
    public int z;

    public ChunkCoord()
    {
        x = 0;
        z = 0;
    }

    public ChunkCoord(int _x, int _z)
    {
        x = _x;
        z = _z;
    }

    public ChunkCoord(Vector3 pos)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int zCheck = Mathf.FloorToInt(pos.z);

        x = xCheck / VoxelData.ChunkWidth;
        z = zCheck / VoxelData.ChunkWidth;
    }

    public override bool Equals(object obj)
    {
        if (obj is ChunkCoord compareTo)
        {
            if (compareTo.x == this.x && compareTo.z == this.z)
                return true;
        }
        return false;
    }
    public override int GetHashCode()
    {
        return x.GetHashCode() ^ z.GetHashCode();
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

    public override string ToString()
    {
        return $"{x}, {z}";
    }
}