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

    // Start is called before the first frame update
    void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        text = GetComponent<Text>();

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
}
