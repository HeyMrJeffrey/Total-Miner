using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Structure
{
    public static void GenerateMajorFlora(int index, Vector3 position, Queue<VoxelMod> queue, int minTrunkHeight, int maxTrunkHeight)
    {
        switch (index)
        {
            case 0: //Grasslands
                MakeTree(position, queue, minTrunkHeight, maxTrunkHeight);
                break;
            case 1: //Desert
                MakeCacti(position, queue, minTrunkHeight, maxTrunkHeight);
                break;
            case 2: //Forest
                MakeTree(position, queue, minTrunkHeight, maxTrunkHeight);
                break;
        }
    }

    public static void MakeTree(Vector3 position, Queue<VoxelMod> queue, int minTrunkHeight, int maxTrunkHeight)
    {
        var noise = Noise.Get2DPerlin(new Vector2(position.x, position.z), 250f, 3f);
        int height = (int)(maxTrunkHeight * noise);

        if (height < minTrunkHeight)
        {
            height = minTrunkHeight;
        }

        // All these for loops give the OG minecraft oak tree
        for(int i = 1; i < height; i++)
        {
            queue.Enqueue(new VoxelMod(new Vector3(position.x, (position.y + i), position.z), 6));
        }


        for (int x = -2; x < 3; x++)
        {
            for (int z = -2; z < 3; z++)
            {
                queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + height - 2, position.z + z), 11));
                queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + height - 3, position.z + z), 11));
            }
        }
        for (int x = -1; x < 2; x++)
        {
            for (int z = -1; z < 2; z++)
            {
                queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + height - 1, position.z + z), 11));
            }
        }
        for (int x = -1; x < 2; x++)
        {
            if (x == 0)
            {
                for (int z = -1; z < 2; z++)
                {
                    queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + height, position.z + z), 11));
                }
            }
            else
            {
                queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + height, position.z), 11));
            }
        }
    }

    public static void MakeCacti(Vector3 position, Queue<VoxelMod> queue, int minTrunkHeight, int maxTrunkHeight)
    {
        var noise = Noise.Get2DPerlin(new Vector2(position.x, position.z), 10f, 2f);
        int height = (int)(maxTrunkHeight * noise);

        if (height < minTrunkHeight)
        {
            height = minTrunkHeight;
        }

        // All these for loops give the OG minecraft oak tree
        for (int i = 1; i <= height; i++)
        {
            queue.Enqueue(new VoxelMod(new Vector3(position.x, (position.y + i), position.z), 12));
        }
    }
}
