using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class DebugScreen : MonoBehaviour
{
    World world;
    Text text;
    float frameRate;
    float timer;

    int halfWorldSizeInVoxels;
    int halfWorldSizeInChunks;

    Player playerScript;
    // Start is called before the first frame update
    void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        text = GetComponent<Text>();
        playerScript = GameObject.Find("Player").GetComponent<Player>();
        halfWorldSizeInVoxels = VoxelData.WorldSizeInVoxels / 2;
        halfWorldSizeInChunks = VoxelData.WorldSizeInChunks / 2;
    }

    // Update is called once per frame
    void Update()
    {
        StringBuilder tBuilt = new StringBuilder();
        tBuilt.AppendLine("DEBUG INFO");
        tBuilt.AppendLine($"FPS: {frameRate}");
        tBuilt.AppendLine($"XYZ: {world.player.transform.position.ToString()}");

        tBuilt.Append("CHUNK: ");
        var currentChunk = world.GetChunkFromVector3(world.player.transform.position);
        if (currentChunk == null)
            tBuilt.AppendLine("NULL");
        else
            tBuilt.AppendLine(currentChunk.coord.ToString());

        Vector3 belowVector = new Vector3(world.player.transform.position.x, world.player.transform.position.y - 0.1f, world.player.transform.position.z);
        tBuilt.AppendLine($"BELOW: {GetVoxelFromVector3(belowVector)}");


        HitTest rc = CalculateHitTest(playerScript.camera.position, playerScript.camera.forward, playerScript.reach);

        if (rc.IsValid)
        {
            tBuilt.AppendLine($"RC: POS ({rc.Point.ToString()}), ID: {GetVoxelFromVector3(rc.Point)}, DIST: {rc.Distance}");
        }
        else
            tBuilt.AppendLine("NO RC");


        text.text = tBuilt.ToString();

        if (timer > 1f)
        {
            frameRate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0;
        }
        else
        {
            timer += Time.deltaTime;
        }
    }



    public struct HitTest
    {
        public bool IsValid;
        public Vector3Int Point;
        public int FacePos;
        public float Distance;
    }
    public HitTest CalculateHitTest(Vector3 origin, Vector3 dir, float range)
    {
        /* TESTING ADVANCED RAY CASTING */

        var result = new HitTest();
        var e = GetMapBounds();
        int stepX, outX;
        int stepY, outY;
        int stepZ, outZ;

        var cb = new Vector3();
        var tmax = new Vector3();
        var tdelta = new Vector3();

        var pos = origin;
        var p = new Vector3Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
        int ox = p.x;
        int oz = p.z;

        if (dir.x > 0)
        {
            stepX = 1; outX = e.max.x;
            cb.x = e.min.x + (p.x + 1);
        }
        else
        {
            stepX = -1; outX = e.min.x - 1;
            cb.x = e.min.x + p.x;
        }
        if (dir.y > 0)
        {
            stepY = 1; outY = e.max.y;
            cb.y = e.min.y + (p.y + 1);
        }
        else
        {
            stepY = -1; outY = e.min.y - 1;
            cb.y = e.min.y + p.y;
        }
        if (dir.z > 0)
        {
            stepZ = 1; outZ = e.max.z;
            cb.z = e.min.z + (p.z + 1);
        }
        else
        {
            stepZ = -1; outZ = e.min.z - 1;
            cb.z = e.min.z + p.z;
        }

        float rxr, ryr, rzr;
        if (dir.x != 0)
        {
            rxr = 1.0f / dir.x;
            tmax.x = (cb.x - pos.x) * rxr;
            tdelta.x = stepX * rxr;
        }
        else tmax.x = float.MaxValue;

        if (dir.y != 0)
        {
            ryr = 1.0f / dir.y;
            tmax.y = (cb.y - pos.y) * ryr;
            tdelta.y = stepY * ryr;
        }
        else tmax.y = float.MaxValue;

        if (dir.z != 0)
        {
            rzr = 1.0f / dir.z;
            tmax.z = (cb.z - pos.z) * rzr;
            tdelta.z = stepZ * rzr;
        }
        else tmax.z = float.MaxValue;

        // Trace the primary ray
        
        int i = 0;

        //Craig had used range squared, along with 
        var rr = range; //* range;

        //Craig had used DistanceSquared, but I don't believe it's 100% necessary to square the distance?
        while (Vector3.Distance(pos, GetBlockCenter(p)) < rr)
        {
            var block = GetVoxelFromVector3(p);

            if (block > 0)
            {
                /* THIS IS WHERE YOU WOULD DO BLOCK CHECKS / IGNORES */

                /*
                if (nonSwingTargets != null && nonSwingTargets.Contains(block))
                {
                    block = 0;
                }
                else
                {
                    if (solidBlocksOnly)
                    {
                        if (map.BlockData[block].Buffer > 1)
                        {
                            block = 0;
                        }
                    }
                    else if (ignoreIcons && map.BlockData[block].IsIcon)
                    {
                        block = 0;
                    }

                    if (block > 0)
                    {
                        var blockID = (Block)block;
                        if (isOnRope && blockID == Block.Rope && p.X == ox && p.Z == oz)
                        {
                            block = 0;
                        }
                        else if (!checkPlayerLiquid && (blockID == Block.Water || blockID == Block.Lava))
                        {
                            //var aux = map.GetAuxData(p);
                            //if (aux != 1) block = 0;
                            block = 0;
                        }
                        else if (blockID == Block.Cloud)
                        {
                            if (!map.IsNextTo(p - mbo, 0, -1, true, false)) block = 0;
                        }
                    }
                }
                */

                if (block > 0)
                {
                    var ray = new Ray(pos, dir);
                   
                    var box = GetBlockBox(p);
                    float dist = float.NaN;
                    if (box.IntersectRay(ray, out dist))
                    {
                        result.Distance = dist;
                        result.Point.x = p.x;
                        result.Point.y = p.y;
                        result.Point.z = p.z;
                        result.IsValid = true;
                        break;
                    }
                }
            }

            if (tmax.x < tmax.y)
            {
                if (tmax.x < tmax.z)
                {
                    p.x += stepX;
                    if (p.x == outX)
                        return result;
                    tmax.x += tdelta.x;
                }
                else
                {
                    p.z += stepZ;
                    if (p.z == outZ)
                        return result;
                    tmax.z += tdelta.z;
                }
            }
            else
            {
                if (tmax.y < tmax.z)
                {
                    p.y += stepY;
                    if (p.y == outY)
                        return result;
                    tmax.y += tdelta.y;
                }
                else
                {
                    p.z += stepZ;
                    if (p.z == outZ)
                        return result;
                    tmax.z += tdelta.z;
                }
            }

            ++i;
        }
        return result;
    }

    /// <summary>
    /// Returns a Bounds object for encapsulating the bounds of a block.  Will need more work as other blocks are created (half blocks..etc..)
    /// </summary>
    public Bounds GetBlockBox(Vector3 pos)
    {
        var blockIDAtPos = GetVoxelFromVector3(pos);
        var posInt = new Vector3Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
        switch (blockIDAtPos)
        {
            default: //For regular 1x1x1 cubes
                {
                    Bounds bound = new Bounds()
                    {
                        min = new Vector3()
                        {
                            x = posInt.x,
                            y = posInt.y,
                            z = posInt.z
                        },
                        max = new Vector3()
                        {
                            x = posInt.x + 1,
                            y = posInt.y + 1,
                            z = posInt.z + 1
                        }
                    };
                    return bound;
                    
                }
        }
    }
    public Vector3 GetBlockCenter(Vector3 point)
    {
        return new Vector3(point.x + 0.5f, point.y + 0.5f, point.z + 0.5f);
    }
    /// <summary>
    /// Get a block ID at a given position on the map.
    /// </summary>
    public byte GetVoxelFromVector3(Vector3 pos)
    {
        if (!world.IsVoxelInWorld(pos))
            return 0; //Air

        var targetChunk = world.GetChunkFromVector3(pos);

        var blockPosInChunk = pos - targetChunk.position;

        int xCheck = Mathf.FloorToInt(blockPosInChunk.x);
        int yCheck = Mathf.FloorToInt(blockPosInChunk.y);
        int zCheck = Mathf.FloorToInt(blockPosInChunk.z);

        return targetChunk.voxelMap[xCheck, yCheck, zCheck];
    }

    public BoundsInt GetMapBounds()
    {
        BoundsInt toRet = new BoundsInt();
        toRet = new BoundsInt(0, 0, 0, VoxelData.ChunkWidth * VoxelData.WorldSizeInChunks, VoxelData.ChunkHeight, VoxelData.ChunkWidth * VoxelData.WorldSizeInChunks);
        return toRet;
    }
}
