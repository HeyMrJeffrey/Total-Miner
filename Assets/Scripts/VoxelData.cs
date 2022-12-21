using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// readonly - never change what a voxel is
public static class VoxelData
{
    // Chunks should be square, width and length are the same
    public static readonly int ChunkWidth = 16;
    public static readonly int ChunkHeight = 128;
    public static readonly int WorldSizeInChunks = 100;
    public static int WorldSizeInVoxels
    {
        get { return WorldSizeInChunks * ChunkWidth; }
    }


    // Needed to normalize texture sizes 
    // Allows for 16,32,64+ size textures
    // Texture atlas must be square for this impl
    public static readonly int textureAtlasSizeInBlocks = 32;

    // Get the distance between each texture
    // If 4x4 atlas, each block is .25
    public static float NormalizedBlockTextureSize
    {
        get { return 1f / (float)textureAtlasSizeInBlocks; }
    }

    // [8] - blocks have 8 vertices (corners)
    // Vector with x,y,z coordinates for vertices (corners)
    // These are offsets relative to block coordinate
    public static readonly Vector3[] voxelVertices = new Vector3[8]
    {
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 1.0f),
    };

    // 6 faces, one for each face in the cube
    public static readonly Vector3[] checkFaces = new Vector3[6]
    {
        new Vector3(0.0f, 0.0f, -1.0f), // Back Face
        new Vector3(0.0f, 0.0f, 1.0f),  // Front Face
        new Vector3(0.0f, 1.0f, 0.0f),  // Top Face
        new Vector3(0.0f, -1.0f, 0.0f), // Bottom Face
        new Vector3(-1.0f, 0.0f, 0.0f), // Left Face
        new Vector3(1.0f, 0.0f, 0.0f)   // Right Face
    };

    // 6 faces of a cube
    // 4 corners per face
    public static readonly int[,] voxelTriangles = new int[6, 4]
    {
        // Back Front Top Bottom Left Right
        
        {0,3,1,2}, // Back Face
        {5,6,4,7}, // Front Face
        {3,7,2,6}, // Top Face
        {1,5,0,4}, // Bottom Face
        {4,7,0,3}, // Left Face
        {1,2,5,6}  // Right Face
    };
}
