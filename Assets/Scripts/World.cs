using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public Transform player;
    public Vector3 spawnPosition;
    public Material material;
    public BlockType[] blockTypes;
    Chunk[,] chunkMap = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    public List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    private void Start()
    {
        spawnPosition = new Vector3(
                            (VoxelData.WorldSizeInChunks*VoxelData.ChunkWidth) / 2f,
                            VoxelData.ChunkHeight + 2f,
                            (VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f
                            );
        GenerateWorld();

        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);
        CheckViewDistance(); //temporary, just spawn immediate chunks instead, this is very hacky way of getting the initial chunks to load when the game starts
    }

    private void Update()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.position);
        if (!playerChunkCoord.Equals(playerLastChunkCoord))
        {
            CheckViewDistance();
        }
    }

    // First initialization of the world
    // These are chunk coordinates, NOT block coordinates
    void GenerateWorld()
    {
        for (int x = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; x < (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; z < (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; z++)
            {
                CreateNewChunk(x, z);
            }
        }

        player.position = spawnPosition;
    }

    ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);

        return new ChunkCoord(x, z);
    }

    void CheckViewDistance()
    {
        ChunkCoord coord = GetChunkCoordFromVector3(player.position);
        playerLastChunkCoord = coord;
        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);
        for (int x = coord.x - VoxelData.ViewDistanceInChunks; x < coord.x + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = coord.z - VoxelData.ViewDistanceInChunks; z < coord.z + VoxelData.ViewDistanceInChunks; z++)
            {
                if (IsChunkInWorld(new ChunkCoord(x, z)))
                {
                    if (chunkMap[x, z] == null)
                        CreateNewChunk(x, z);
                    if (!chunkMap[x,z].isActive)
                        chunkMap[x, z].isActive = true;   
                }

                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(new ChunkCoord(x,z)))
                    {
                        previouslyActiveChunks.RemoveAt(i);
                    }
                }
            }
        }

        foreach (ChunkCoord prevCoord in previouslyActiveChunks)
        {
            chunkMap[prevCoord.x, prevCoord.z].isActive = false;
        }
    }

    // *****************************
    // Engine that drives generation
    // *****************************
    // Input a position and it returns a Voxel ID
    public byte GetVoxel(Vector3 pos)
    {
        // If the current voxel is not in the world, just make it air.
        if (!IsVoxelInWorld(pos))
        {
            return 0;
        }

        if (pos.y < 1)
        {
            return 1;
        }
        else if (pos.y == VoxelData.ChunkHeight - 1)
        {
            return 3;
        }
        else
        {
            return 2;
        }
    }

    bool IsVoxelInWorld(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels &&
            pos.y >= 0 && pos.y < VoxelData.ChunkHeight &&
            pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // Create a new chunk @x,z and store it in a chunk map
    void CreateNewChunk(int x, int z)
    {
        chunkMap[x, z] = new Chunk(new ChunkCoord(x, z), this);
        //activeChunks.Add(new ChunkCoord(x,z));
    }

    bool IsChunkInWorld(ChunkCoord coord)
    {
        if (coord.x > 0 && coord.x < VoxelData.WorldSizeInChunks - 1 &&
           coord.z > 0 && coord.z < VoxelData.WorldSizeInChunks - 1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}

[System.Serializable]
public class BlockType
{
    public string blockName;
    public bool isSolid;

    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    // Back Front Top Bottom Left Right
    public int GetTextureID(int faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.Log("ERROR: World.cs - GetTextureID - Could not return correct texture.");
                return 0;

        }
    }
}