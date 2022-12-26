using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using System.IO;
using UnityEngine.XR;
using System;
using UnityEditor;
using UnityEngine.TextCore.Text;
using UnityEngine.U2D;
using UnityEngine.Assertions;

public class World : MonoBehaviour
{
    public bool Multithreading = true;

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

    public List<Chunk> chunksToUpdate = new List<Chunk>();
    List<Chunk> chunksToAddToUpdateList = new List<Chunk>();
    object chunksToUpdateLock = new object();


    public ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    public GameObject debugScreen;
    public GameObject creativeInventoryWindow;
    public GameObject cursorSlot;
    public Dictionary<string, Sprite> blockSprites;

    // TESTING THREADING STUPID SHIT.
    public Thread chunkUpdateThread;

    public void chunkUpdateThreaded()
    {
        //AutoResetEvent reset = new AutoResetEvent(false);
        while (true)
        {

            lock (chunksToUpdateLock)
            {
                /* APPLYING MODIFICATIONS TO EACH CHUNK'S QUEUE */

                //If a chunk has not yet been created then we will throw it into the chunksToCreate, otherwise we will go ahead and apply it's modifications.
                //It will then loop back around through here at some point after it's been created because we re-queue it's modifications
                //int numModsToAttempt = modifications.Count;
                //for (int i = 0; i < numModsToAttempt; i++)
                //{
                //    //We don't want to dequeue the 
                //    VoxelMod mod = modifications.Dequeue();
                //    ChunkCoord coord = GetChunkCoordFromVector3(mod.position);
                //    if (coord.x >= 0 && coord.x < VoxelData.WorldSizeInChunks && coord.z >= 0 && coord.z < VoxelData.WorldSizeInChunks)
                //    {
                //        //The voxel mod is in the world bounds.
                //        //We will now check to see if the chunk associated with this voxelmod has been created.
                //        Chunk targetChunk = null;
                //        if ((targetChunk = chunkMap[coord.x, coord.z]) == null)
                //        {
                //            //The chunk hasn't been created, let's add it to the chunksToCreateList.
                //            chunksToCreate.Add(coord);
                //            modifications.Enqueue(mod);
                //            continue;
                //        }
                //        else
                //        {
                //            //The chunk exists, let's queue it's voxelmod.
                //            targetChunk.modifications.Enqueue(mod);
                //            if (!chunksToAddToUpdateList.Contains(targetChunk))
                //                chunksToAddToUpdateList.Add(targetChunk); //We don't use `AddChunkToUpdateList` because that acquires a lock and we already have one.
                //        }
                //    }
                //    else
                //    {
                //        //Voxel mod out of world bounds.  Drop it.
                //    }
                //}


                for (int i = 0; i < chunksToAddToUpdateList.Count; i++)
                {
                    var targetChunk = chunksToAddToUpdateList[0];
                    chunksToAddToUpdateList.RemoveAt(0);
                    chunksToUpdate.Add(targetChunk);
                }
            }

            /* UPDATING THE CHUNKS */

            int maxToUpdate = 10;
            int numToUpdate = Math.Min(maxToUpdate, chunksToUpdate.Count);
            for (int i = 0; i < numToUpdate; i++)
            {
                var targetChunk = chunksToUpdate[0];

                Chunk.chunkUpdateThreadData updateData = default;
                updateData.ChunkPosition = new Vector3(targetChunk.coord.x * VoxelData.ChunkWidth, 0, targetChunk.coord.z * VoxelData.ChunkWidth);
                updateData.Valid = true;

                if (!targetChunk.IsGenerated)
                    targetChunk.PopulateVoxelMap();

                chunksToUpdate[0].UpdateChunk(updateData);

                chunksToUpdate.RemoveAt(0);
            }
            System.Threading.Thread.Sleep(1);
            // reset.Set();
        }
    }
    // Modifications to a chunk (trees overlapping chunks)
    volatile Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    // UI - Inventory
    private bool _inUI = false;
    private bool _inPauseMenu = false;
    private bool _inInventory = false;


    public void Init()
    {
        Multithreading = true;

        biomes = new BiomeAttributes[]
        {
                (BiomeAttributes)AssetDatabase.LoadAssetAtPath("Assets/Data/Biomes/Grasslands.asset", typeof(BiomeAttributes)),
                (BiomeAttributes)AssetDatabase.LoadAssetAtPath("Assets/Data/Biomes/Desert.asset", typeof(BiomeAttributes)),
                (BiomeAttributes)AssetDatabase.LoadAssetAtPath("Assets/Data/Biomes/Forest.asset", typeof(BiomeAttributes))
        };

        material = (Material)AssetDatabase.LoadAssetAtPath("Assets/Materials/TP_OriginalHD.mat", typeof(Material));
        transparentMaterial = (Material)AssetDatabase.LoadAssetAtPath("Assets/Materials/TP_OriginalHD_Transparent.mat", typeof(Material));

        GameObject canvas = GameObject.Find("Canvas");
        debugScreen = canvas.transform.Find("Debug Screen").gameObject;
        creativeInventoryWindow = canvas.transform.Find("CreativeInventory").gameObject;
        cursorSlot = canvas.transform.Find("CursorSlot").gameObject;


        Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/TP_OriginalHD_Icons") as Sprite[];
        blockSprites = new Dictionary<string, Sprite>(sprites.Length);

        foreach (Sprite s in sprites)
        {
            blockSprites.Add(s.name, s);
        }


        blockTypes = new BlockType[]
        {
                new BlockType("Air", blockSprites["TP_OriginalHD_Icons_Air"], 0, true, false),
                new BlockType("Bedrock", blockSprites["TP_OriginalHD_Icons_Bedrock"], 29),
                new BlockType("Stone", blockSprites["TP_OriginalHD_Icons_Stone"], 17),
                new BlockType("Grass", blockSprites["TP_OriginalHD_Icons_Grass"], 1,2,260,260,260,260),
                new BlockType("Sand", blockSprites["TP_OriginalHD_Icons_Sand"], 3),
                new BlockType("Dirt", blockSprites["TP_OriginalHD_Icons_Dirt"], 2),
                new BlockType("Wood", blockSprites["TP_OriginalHD_Icons_Wood"], 261,261,5,5,5,5),
                new BlockType("WoodPlanks", blockSprites["TP_OriginalHD_Icons_WoodPlanks"], 6),
                new BlockType("Bricks", blockSprites["TP_OriginalHD_Icons_Bricks"], 43),
                new BlockType("Cobblestone",blockSprites["TP_OriginalHD_Icons_Cobblestone"],  42),
                new BlockType("Glass", blockSprites["TP_OriginalHD_Icons_Glass"], 9, true),
                new BlockType("Leaves", blockSprites["TP_OriginalHD_Icons_Leaves"], 8, true),
                new BlockType("Cactus", blockSprites["TP_OriginalHD_Icons_Cactus"], 151)
        };

        spawnPosition = 
            new Vector3((VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f,
                         VoxelData.ChunkHeight - 40,
                        (VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f);

        //create player
        //bad spot but so many ref fix later
        Player p = (new GameObject("Player")).AddComponent<Player>();
        p.Init(this);
        player = p.transform;

    }

    private void Start()
    {
        // JSON EXPORT SETTINGS
        //string jsonExport = JsonUtility.ToJson(settings);
        //File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);

        // JSON IMPORT SETTINGS
        //move to main and put setting in own static class
        string jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
        settings = JsonUtility.FromJson<Settings>(jsonImport);

        UnityEngine.Random.InitState(settings.seed);
        
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

        if (Globals.MTQ == null)//some how it can be null in unity
            Globals.MTQ = new MainThreadQueue();


        Globals.MTQ.Execute(5);

        //Only update the chunks if the player has moved from the chunk they were previously on.
        if (!playerChunkCoord.Equals(playerLastChunkCoord))
        {
            CheckViewDistance();
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
                    chunkMap[coord.x, coord.z] = new Chunk(coord, this, false);
                    chunkMap[coord.x, coord.z].Init(true, false);
                }

                // Enqueueing into the chunk modifications, not the world modifications
                chunkMap[coord.x, coord.z].modifications.Enqueue(v);
                if (!chunksToUpdate.Contains(chunkMap[coord.x, coord.z]))
                    chunksToUpdate.Add(chunkMap[coord.x, coord.z]);

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
            if (chunkMap[coord.x, coord.z] == null)
                chunkMap[coord.x, coord.z] = new Chunk(coord, this, false);
            if (!chunkMap[coord.x, coord.z].IsInitialized && !chunkMap[coord.x, coord.z].IsInitializing)
                chunkMap[coord.x, coord.z].Init(false, false); //We're not going to populate or update it here.  That will be handled in the multithreading code.
            AddChunkToUpdateList(chunkMap[coord.x, coord.z]);
        }
    }

    void UpdateChunks()
    {
        Assert.IsTrue(!Multithreading, "UpdateChunks cannot be called if multithreading is active.");

        bool updated = false;
        int index = 0;
        while (!updated && index < chunksToUpdate.Count - 1)
        {
            if (chunksToUpdate[index].IsGenerated)
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


    bool IsTargetVisible(Camera c, GameObject go)
    {
        var planes = GeometryUtility.CalculateFrustumPlanes(c);
        var point = go.transform.position;
        foreach (var plane in planes)
        {
            if (plane.GetDistanceToPoint(point) < 0)
                return false;
        }
        return true;
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
                        chunkMap[x, z].Init(false, false);
                        chunkMap[x, z].isActive = true;
                        chunksToCreate.Add(thisChunkCoord);
                    }
                    else
                    {
                        chunkMap[x, z].isActive = true;
                        chunkMap[x, z].SetChunkActive(true);
                    }
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

        if (chunkMap[thisChunk.x, thisChunk.z] != null && chunkMap[thisChunk.x, thisChunk.z].IsGenerated)
        {
            return blockTypes[chunkMap[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos)].isSolid;
        }

        return blockTypes[GetVoxel(pos)].isSolid;
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
    public bool CheckIfVoxelTransparent(Vector3 pos, Chunk.chunkUpdateThreadData threadedData = default)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.ChunkHeight)
            return false;

        if (chunkMap[thisChunk.x, thisChunk.z] != null && chunkMap[thisChunk.x, thisChunk.z].IsGenerated)
        {
            return blockTypes[chunkMap[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos, threadedData)].isTransparent;
        }

        return blockTypes[GetVoxel(pos)].isTransparent;
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

    public bool AddChunkToUpdateList(Chunk chunk)
    {
        bool value = false;

        lock (chunksToUpdateLock)
        {
            if (chunksToAddToUpdateList.Contains(chunk))
                value = false;
            else
            {
                chunksToAddToUpdateList.Add(chunk);
                value = true;
            }
        }
        return value;
    }
    public int AddChunksToUpdateList(IEnumerable<Chunk> chunks)
    {
        int toRet = 0;
        lock (chunksToUpdateLock)
        {
            foreach (var chunk in chunks)
            {
                if (!chunksToAddToUpdateList.Contains(chunk))
                {
                    chunksToAddToUpdateList.Add(chunk);
                    toRet++;
                }
            }
        }
        return toRet;
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

    public BlockType(string name, Sprite icon, int texNum, bool isTransparent = false, bool isSolid = true)
    {
        blockName = name;
        this.isTransparent = isTransparent;
        this.isSolid = isSolid;
        backFaceTexture =
        frontFaceTexture =
        topFaceTexture =
        bottomFaceTexture =
        leftFaceTexture =
        rightFaceTexture = texNum;
        this.icon = icon;
    }

    public BlockType(string name, Sprite icon, int texNumTop, int texNumBottom, int texNumLeft, int texNumRight, int texNumFront, int texNumBack, bool isTransparent = false, bool isSolid = true)
    {
        blockName = name;
        this.isTransparent = isTransparent;
        this.isSolid = isSolid;
        backFaceTexture = texNumBack;
        frontFaceTexture = texNumFront;
        topFaceTexture = texNumTop;
        bottomFaceTexture = texNumBottom;
        leftFaceTexture = texNumLeft;
        rightFaceTexture = texNumRight;
        this.icon = icon;
    }

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