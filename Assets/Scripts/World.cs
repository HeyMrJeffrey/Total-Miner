using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public int seed;
    public BiomeAttributes biome;

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
        Random.InitState(seed);

        spawnPosition = new Vector3(
                            (VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f,
                            VoxelData.ChunkHeight - 50f,
                            (VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f
                            );
        GenerateWorld();

        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);
        CheckViewDistance(); //temporary, just spawn immediate chunks instead, this is very hacky way of getting the initial chunks to load when the game starts
    }

    private void Update()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.position);

        /*
        //Only update the chunks if the player has moved from the chunk they were previously on.
        if (!playerChunkCoord.Equals(playerLastChunkCoord))
        {
            CheckViewDistance();
        }
        */
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
                    //If the chunk hasn't been created at all, then let's create it.
                    if (chunkMap[x, z] == null)
                        CreateNewChunk(x, z);
                    //We now know that the chunk has been created / it exists.  Now we know it's within the view distance, so we activate it.
                    chunkMap[x, z].isActive = true; //This adds this chunkcoord the world.activeChunks list
                }

                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(new ChunkCoord(x, z)))
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
    /// TODO: Need to prevent reference exception errors if player ends up outside world space.
    /// As of right now, we assume player MUST BE in a vlid world space
    // take in global vector 3 (800,200,1024), true if voxel is in world false otherwise
    public bool CheckForVoxel(float _x, float _y, float _z)
    {
        // get bottom left back corner of each voxel
        int xCheck = Mathf.FloorToInt(_x);
        int yCheck= Mathf.FloorToInt(_y);
        int zCheck = Mathf.FloorToInt(_z);

        int xChunk = xCheck / VoxelData.ChunkWidth;
        int zChunk = zCheck / VoxelData.ChunkWidth;

        // get voxel value within the chunk
        // dont need y, there is only one chunk up and down
        xCheck -= (xChunk * VoxelData.ChunkWidth);
        zCheck -= (zChunk * VoxelData.ChunkWidth);

        return blockTypes[chunkMap[xChunk, zChunk].voxelMap[xCheck, yCheck, zCheck]].isSolid;
    }


    // *****************************
    // Engine that drives generation
    // *****************************
    // Input a position and it returns a Voxel ID
    public byte GetVoxel(Vector3 pos)
    {
        /* BLOCK TYPES
        0 = Air
        1 = Bedrock
        2 = Stone
        3 = Grass
        4 = Dirt
        5 = Furnace
        6 = Sand
        7 = Dirt
        */

        int yPos = Mathf.FloorToInt(pos.y);

        /* IMMUTABLE PASS */
        // If the current voxel is not in the world, just make it air.

        //If outside the world, return an air block.
        if (!IsVoxelInWorld(pos))
            return 0;

        //If bottom block of chunk, return bedrock
        if (yPos == 0)
            return 1;


        /* BASIC TERRAIN PASS */

        int terrainHeight = Mathf.FloorToInt(biome.TerrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.TerrainScale)) + biome.SolidGroundHeight;
        byte voxelValue = 0;


        if (yPos == terrainHeight)
            voxelValue = 3; //Grass
        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
            voxelValue = 7; //Dirt
        else if (yPos > terrainHeight)
            return 0; //Air
        else
            voxelValue = 2; //Stone'


        /* SECOND PASS */

        if (voxelValue == 2) //if Stone
        {
            foreach (Lode lode in biome.Lodes)
            {
                if (yPos > lode.minHeight && yPos < lode.maxHeight)
                {
                    if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshhold))
                    {
                        voxelValue = lode.blockID;
                    }
                }
            }
        }

        return voxelValue;
    }

    public bool IsVoxelInWorld(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.ChunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels)
            return true;
        else
            return false;
    }

    // Create a new chunk @x,z and store it in a chunk map
    void CreateNewChunk(int x, int z)
    {
        chunkMap[x, z] = new Chunk(new ChunkCoord(x, z), this);
    }

    bool IsChunkInWorld(ChunkCoord coord)
    {
        if (coord.x >= 0 && coord.x < VoxelData.WorldSizeInChunks &&
           coord.z >= 0 && coord.z < VoxelData.WorldSizeInChunks)
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