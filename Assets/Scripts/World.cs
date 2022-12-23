using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using System.IO;
using UnityEngine.XR;
using System;

public class World : MonoBehaviour
{
    public bool Multithreading;

    public Settings settings;

    [Header("World Generation Values")]
    public BiomeAttributes[] biomes;

    public Transform player;
    public Vector3 spawnPosition;
    public Material material;
    public Material transparentMaterial;
    public BlockType[] blockTypes;

    Chunk[,] chunkMap = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];
    public List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    public List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();

    List<Chunk> chunksToUpdate = new List<Chunk>();
    object chunksToUpdateLock = new object();

    private bool applyingModifications = false;

    public ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    public GameObject debugScreen;
    public GameObject creativeInventoryWindow;
    public GameObject cursorSlot;


    // TESTING THREADING STUPID SHIT.
    public Thread chunkUpdateThread;

    public void chunkUpdateThreaded()
    {
        //AutoResetEvent reset = new AutoResetEvent(false);
        while (true)
        {
            // TESTING MULTITHREADING GETTING THE FUCKING COORDINATES
            //var transformResult = new MainThreadQueue.Result<Transform>();
            //SingletonManager.MTQ.GetTransform(targetChunk.chunkObject, transformResult);
            //var transform = transformResult.Value;

            //var positionResult = new MainThreadQueue.Result<Vector3>();
            //SingletonManager.MTQ.GetPositionFromTransform(transform, positionResult);
            //var position = positionResult.Value;

            //var pos = position;
            //Debug.Log(pos.ToString());

            bool updated = false;
            int index = 0;

            lock (chunksToUpdateLock)
            {
                while (!updated && index < chunksToUpdate.Count - 1)
                {
                    if (chunksToUpdate[index].isVoxelMapPopulated)
                    {
                        Chunk.chunkUpdateThreadData updateData = default;

                        //Get the chunkobject's position
                        MainThreadQueue.Result<Vector3> positionResult = new MainThreadQueue.Result<Vector3>();
                        SingletonManager.MTQ.GetPositionFromGameObject(chunksToUpdate[index].chunkObject, positionResult);
                        updateData.ChunkPosition = positionResult.Value;
                        
                        updateData.Valid = true;

                        try
                        {
                            chunksToUpdate[index].UpdateChunk(updateData);

                        }
                        catch (Exception ex)
                        {

                        }
                        chunksToUpdate.RemoveAt(index);
                        updated = true;
                    }
                    else
                    {
                        index++;
                    }
                }
            }
            System.Threading.Thread.Sleep(1);
           // reset.Set();
        }
    }
    // Modifications to a chunk (trees overlapping chunks)
    Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    // UI - Inventory
    private bool _inUI = false;
    private bool _inPauseMenu = false;
    private bool _inInventory = false;


    private void Start()
    {
        Multithreading = true;
        // JSON EXPORT SETTINGS
        //string jsonExport = JsonUtility.ToJson(settings);
        //File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);

        // JSON IMPORT SETTINGS
        string jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
        settings = JsonUtility.FromJson<Settings>(jsonImport);

        UnityEngine.Random.InitState(settings.seed);
        SingletonManager.MTQ = new MainThreadQueue();

        spawnPosition = new Vector3(
                            (VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f,
                            VoxelData.ChunkHeight - 40,
                            (VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f
                            );


        GenerateWorld();



        playerLastChunkCoord = playerChunkCoord = GetChunkCoordFromVector3(player.position);
        CheckViewDistance(); //temporary, just spawn immediate chunks instead, this is very hacky way of getting the initial chunks to load when the game starts



        // TESTING THREADING SHIT
        if (Multithreading)
        {
            chunkUpdateThread = new Thread(chunkUpdateThreaded);
            chunkUpdateThread.Start();
        }
    }

    private void Update()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.position);

        SingletonManager.MTQ.Execute(5);

        //Only update the chunks if the player has moved from the chunk they were previously on.
        if (!playerChunkCoord.Equals(playerLastChunkCoord))
        {
            CheckViewDistance();
        }

        if (modifications.Count > 0 && !applyingModifications)
        {
            //StartCoroutine(ApplyModifications());
        }

        if (chunksToCreate.Count > 0)
        {
            CreateChunk();
        }

        if (chunksToUpdate.Count > 0 && !Multithreading)
        {
            UpdateChunks();
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            debugScreen.SetActive(!debugScreen.activeSelf);
        }

    }

    // First initialization of the world
    // These are chunk coordinates, NOT block coordinates
    void GenerateWorld()
    {
        for (int x = (VoxelData.WorldSizeInChunks / 2) - settings.viewDistance; x < (VoxelData.WorldSizeInChunks / 2) + settings.viewDistance; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - settings.viewDistance; z < (VoxelData.WorldSizeInChunks / 2) + settings.viewDistance; z++)
            {
                chunkMap[x, z] = new Chunk(new ChunkCoord(x, z), this, true);
                activeChunks.Add(new ChunkCoord(x, z));
            }
        }

        /// TODO: Everytime we are useing a queue, we need to ensure we are not adding to it elsewhere
        /// Otherwise we may end up in an endless loop (perhaps lock the queue?)
        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();
            ChunkCoord coord = GetChunkCoordFromVector3(v.position);

            if (coord.x >= 0 && coord.x < VoxelData.WorldSizeInChunks && coord.z >= 0 && coord.z < VoxelData.WorldSizeInChunks)
            {

                // If a modification is occuring on a chunk that isnt genned, but on the border of one that is
                // i.e. a tree on a genned chunk that has leaves on a non genned chunk
                // gen that chunk
                if (chunkMap[coord.x, coord.z] == null)
                {
                    chunkMap[coord.x, coord.z] = new Chunk(coord, this, true);
                    activeChunks.Add(coord);
                }

                // Enqueueing into the chunk modifications, not the world modifications
                chunkMap[coord.x, coord.z].modifications.Enqueue(v);

                AddChunkToUpdateList(chunkMap[coord.x, coord.z]);
            }
        }

        lock (chunksToUpdateLock)
        {
            for (int i = 0; i < chunksToUpdate.Count; i++)
            {
                chunksToUpdate[0].UpdateChunk(default);
                chunksToUpdate.RemoveAt(0);
            }
        }

        player.position = spawnPosition;
    }

    void CreateChunk()
    {
        if (chunksToCreate.Count > 0)
        {
            ChunkCoord coord = chunksToCreate[0];
            chunksToCreate.RemoveAt(0);
            activeChunks.Add(coord);
            chunkMap[coord.x, coord.z].Init();
        }
    }

    void UpdateChunks()
    {
        if (Multithreading)
            return;
        
        bool updated = false;
        
        int index = 0;


        
        while (!updated && index < chunksToUpdate.Count - 1)
        
            {
        
            if (chunksToUpdate[index].isVoxelMapPopulated)
        
                {
        
                chunksToUpdate[index].UpdateChunk();
        
                chunksToUpdate.RemoveAt(index);
        
                updated = true;
        
                }
        
            else
        
            {
        
                index++;
        
                }
        }
    }

    IEnumerator ApplyModifications()
    {
        applyingModifications = true;
        int count = 0;

        while (modifications.Count > 0)
        {
            VoxelMod v = modifications.Dequeue();
            ChunkCoord coord = GetChunkCoordFromVector3(v.position);

            if (coord.x >= 0 && coord.x < VoxelData.WorldSizeInChunks && coord.z >= 0 && coord.z < VoxelData.WorldSizeInChunks)
            {

                // If a modification is occuring on a chunk that isnt genned, but on the border of one that is
                // i.e. a tree on a genned chunk that has leaves on a non genned chunk
                // gen that chunk
                if (chunkMap[coord.x, coord.z] == null)
                {
                    chunkMap[coord.x, coord.z] = new Chunk(coord, this, true);
                    activeChunks.Add(coord);
                }

                // Enqueueing into the chunk modifications, not the world modifications
                chunkMap[coord.x, coord.z].modifications.Enqueue(v);

                AddChunkToUpdateList(chunkMap[coord.x, coord.z]);

                count++;
                // Only 200 voxel modifications per frame
                if (count > 200)
                {
                    count = 0;
                    yield return null;
                }
            }
        }

        applyingModifications = false;
    }


    ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);

        return new ChunkCoord(x, z);
    }

    public Chunk GetChunkFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);

        //If the position given is out of the world bounds, then there is no chunk there.
        if (!IsVoxelInWorld(pos))
            return null;

        return chunkMap[x, z];
    }

    void CheckViewDistance()
    {
        ChunkCoord coord = GetChunkCoordFromVector3(player.position);
        playerLastChunkCoord = coord;
        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);
        activeChunks.Clear();
        for (int x = coord.x - settings.viewDistance; x < coord.x + settings.viewDistance; x++)
        {
            for (int z = coord.z - settings.viewDistance; z < coord.z + settings.viewDistance; z++)
            {
                ChunkCoord thisChunkCoord = new ChunkCoord(x, z);

                if (IsChunkInWorld(thisChunkCoord))
                {
                    //If the chunk hasn't been created at all, then let's create it.
                    if (chunkMap[x, z] == null)
                    {
                        chunkMap[x, z] = new Chunk(thisChunkCoord, this, false);
                        chunksToCreate.Add(thisChunkCoord);
                    }
                    else if (!(chunkMap[x, z].isActive))
                    {
                        chunkMap[x, z].isActive = true;
                    }
                    activeChunks.Add(thisChunkCoord);
                }

                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(thisChunkCoord))
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
    public bool CheckForVoxel(Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.ChunkHeight)
            return false;

        if (chunkMap[thisChunk.x, thisChunk.z] != null && chunkMap[thisChunk.x, thisChunk.z].isVoxelMapPopulated)
        {
            return blockTypes[chunkMap[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isSolid;
        }

        return blockTypes[GetVoxel(pos)].isSolid;
    }

    public bool CheckIfVoxelTransparent(Vector3 pos, Chunk.chunkUpdateThreadData threadedData = default)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.ChunkHeight)
            return false;

        if (chunkMap[thisChunk.x, thisChunk.z] != null && chunkMap[thisChunk.x, thisChunk.z].isVoxelMapPopulated)
        {
            return blockTypes[chunkMap[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos, threadedData)].isTransparent;
        }

        return blockTypes[GetVoxel(pos)].isTransparent;
    }

    public bool inUI
    {
        get
        {
            return _inUI;
        }
        set
        {
            _inUI = value;
            if (_inUI)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    public bool inPauseMenu
    {
        get
        {
            return _inPauseMenu;
        }
        set
        {
            if (value)
            {
                inInventory = false;
                inUI = true;
                _inPauseMenu = true;

            }
            else
            {
                inUI = false;
                _inPauseMenu = false;
            }
        }
    }

    public bool inInventory
    {
        get
        {
            return _inInventory;
        }
        set
        {
            
            if (value)
            {
                inPauseMenu = false;
                inUI = true;
                _inInventory = true;
                creativeInventoryWindow.SetActive(true);
                cursorSlot.SetActive(true);
                
            }
            else
            {
                creativeInventoryWindow.SetActive(false);
                cursorSlot.SetActive(false);
                inUI = false;
                _inInventory = false;
            }
        }
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
        4 = Sand
        5 = Dirt
        6 = Wood
        7 = WoodPlanks
        8 = Bricks
        9 = Cobblestone
        10 = Glass
        11 = Leaves
        */

        int yPos = Mathf.FloorToInt(pos.y);

        /* IMMUTABLE PASS */
        // If the current voxel is not in the world, just make it air.

        //If outside the world, return an air block.
        if (!IsVoxelInWorld(pos))
            return 0; //Air

        //If bottom block of chunk, return bedrock
        if (yPos == 0)
            return 1; //Bedrock

        /* BIOME SELECTION PASS */
        int solidGroundHeight = 42;
        float sumOfHeights = 0;
        int count = 0;
        float strongestWeight = 0;
        int strongestBiomeIndex = 0;

        for (int i = 0; i < biomes.Length; i++)
        {
            float weight = Noise.Get2DPerlin(new Vector2(pos.x, pos.z), biomes[i].offset, biomes[i].scale);
            // Keep track of which weight is strongest;
            if (weight > strongestWeight)
            {
                strongestWeight = weight;
                strongestBiomeIndex = i;
            }

            // Get height of terrain for the current biome and multiply it by its weight.
            float height = biomes[i].TerrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biomes[i].TerrainScale) * weight;

            // If heigh value is greater than 0, add it to sum of heights
            if (height > 0)
            {
                sumOfHeights += height;
                count++;
            }
        }
        // Set biome to the one with th strongest weight
        BiomeAttributes biome = biomes[strongestBiomeIndex];

        // Get the average of heights
        sumOfHeights /= count;

        int terrainHeight = Mathf.FloorToInt(sumOfHeights + solidGroundHeight);


        /* BASIC TERRAIN PASS */
        byte voxelValue = 0;


        if (yPos == terrainHeight)
            voxelValue = biome.surfaceBlock; //Grass
        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
            voxelValue = biome.subSurfaceBlock; //Dirt
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

        /* TREE PASS */

        if (yPos == terrainHeight && biome.placeMajorFlora)
        {
            if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.majorFloraPlacementScale) > biome.majorFloraZoneThreshold)
            {
                //voxelValue = 5; // Dirt. Made this dirt just to show where trees *can* spawn. Just remove to go back to grass

                if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.majorFloraPlacementScale) > biome.majorFloraPlacementThreshold)
                {
                    Structure.GenerateMajorFlora(biome.majorFloraIndex, pos, modifications, biome.minHeight, biome.maxHeight);
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

    public bool IsChunkInUpdateList(Chunk chunk)
    {
        bool value = false;
        lock (chunksToUpdateLock)
        {
            value = chunksToUpdate.Contains(chunk);
        }
        return value;
    }
    public bool AddChunkToUpdateList(Chunk chunk)
    {
        bool value = false;
        lock (chunksToUpdateLock)
        {
            if (chunksToUpdate.Contains(chunk))
                value = false;
            else
            {
                chunksToUpdate.Add(chunk);
                value = true;
            }
        }

        return value;
    }
    public bool RemoveChunkFromUpdateList(Chunk chunk)
    {
        bool value = false;
        lock (chunksToUpdateLock)
        {
            if (!chunksToUpdate.Contains(chunk))
                value = false;
            else
            {
                chunksToUpdate.Remove(chunk);
                value = true;
            }
        }

        return value;
    }
}

[System.Serializable]
public class BlockType
{
    public string blockName;
    public bool isSolid;         //Whether the block is physically there (can player pass through the block like air? or not)
    public bool isTransparent;    //Whether the block can be seen through by the player
    public Sprite icon;

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

//added for trees
public class VoxelMod
{
    public Vector3 position;
    public byte id;

    public VoxelMod()
    {
        position = new Vector3();
        id = 0;
    }

    public VoxelMod(Vector3 _position, byte _id)
    {
        position = _position;
        id = _id;
    }

}

[System.Serializable]
public class Settings
{
    [Header("Game Data")]
    public string version;

    [Header("Performance")]
    public int viewDistance;

    [Header("Controls")]
    [Range(8f, 100f)]
    public float mouseSensitivity;

    [Header("World Generation")]
    public int seed;
}