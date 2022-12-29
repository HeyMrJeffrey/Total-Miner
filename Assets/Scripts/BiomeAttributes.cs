using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "Custom Menu/Biome Attributes")]
public class BiomeAttributes : ScriptableObject
{
    [Header("Biome Attributes")]
    public string BiomeName;
    public int offset;
    public float scale;

    public int TerrainHeight; //Heighest Terrain Height, from the solidGroundHeight
    public float TerrainScale;

    public byte surfaceBlock = 3;
    public byte subSurfaceBlock = 5;

    [Header("Major Flora")]
    public int majorFloraIndex = 0;
    // The area where trees can spawn
    public float majorFloraZoneScale = 1.3f;
    [Range(0.1f, 1f)]
    public float majorFloraZoneThreshold = 0.6f;

    // How many trees do we put in this area?
    public float majorFloraPlacementScale = 15f;
    [Range(0.1f, 1f)]
    public float majorFloraPlacementThreshold = 0.8f;

    public bool placeMajorFlora = true;

    public int maxHeight = 12;
    public int minHeight = 5;

    public Lode[] Lodes;
}

//This is for ores (diamonds, iron..etc..)
[System.Serializable]
public class Lode
{
    public string nodeName;
    public byte blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshhold;
    public float noiseOffset;
}