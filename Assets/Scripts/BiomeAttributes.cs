using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "BasicBiome")]
public class BiomeAttributes : ScriptableObject
{
    public string BiomeName;
    public int SolidGroundHeight;
    public int TerrainHeight; //Heighest Terrain Height, from the solidGroundHeight
    public float TerrainScale;

    [Header("Trees")]
    // The area where trees can spawn
    public float treeZoneScale = 1.3f;
    [Range(0.1f, 1f)]
    public float treeZoneThreshold = 0.6f;

    // How many trees do we put in this area?
    public float treePlacementScale = 15f;
    [Range(0.1f, 1f)]
    public float treePlacementThreshold = 0.8f;

    public int maxTreeHeight = 12;
    public int minTreeHeight = 5;

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