using Noesis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Timers;
using TotalMinerUnity;
using TotalMinerUnity.Menus;
using UnityEngine;
using static UnityEditor.Timeline.TimelinePlaybackControls;

public class Main : MonoBehaviour
{
    void Start()
    {
        Globals.MainMenuBackgrounds = new Queue<Texture2D>();



        int random = UnityEngine.Random.Range(1, 113);
        Globals.MainMenuBackgrounds.Enqueue((Texture2D)Resources.Load($"Images/BackgroundScreen{random}"));
        random = GetRandomInRangeWithContext(random, 1, 113);
        Globals.MainMenuBackgrounds.Enqueue((Texture2D)Resources.Load($"Images/BackgroundScreen{random}"));
        random = GetRandomInRangeWithContext(random, 1, 113);
        Globals.MainMenuBackgrounds.Enqueue((Texture2D)Resources.Load($"Images/BackgroundScreen{random}"));
        random = GetRandomInRangeWithContext(random, 1, 113);
        Globals.MainMenuBackgrounds.Enqueue((Texture2D)Resources.Load($"Images/BackgroundScreen{random}"));


        Globals.MainCamera = GameObject.Find("Main Camera");
        Globals.MainWindow = Globals.MainCamera.GetComponent<NoesisView>().Content as MainWindow;
        Globals.MTQ = new MainThreadQueue();
        Globals.MainWindow.Loaded += (object sender, RoutedEventArgs e) => 
        {
            Globals.MainViewModel = Globals.MainWindow.DataContext as ViewModel;
        };
        

    }

    void Update()
    {
        if(Globals.MainMenuBackgrounds != null && Globals.MainMenuBackgrounds.Count < 4)
        {
            Globals.MainMenuBackgrounds.Enqueue((Texture2D)Resources.Load($"Images/BackgroundScreen{UnityEngine.Random.Range(1, 113)}"));
        }

    }

    int GetRandomInRangeWithContext(int context, int minInclusive, int maxInclusive)
    {
        int rand = context;
        for(int i = 0; i < 10 && rand == context; i++)
        {
            rand = UnityEngine.Random.Range(minInclusive, maxInclusive);
        }

        return rand;
    }


}
