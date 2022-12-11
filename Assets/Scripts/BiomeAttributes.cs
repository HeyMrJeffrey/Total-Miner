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