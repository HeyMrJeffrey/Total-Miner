using Noesis;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using TotalMinerUnity;
using UnityEngine;

public class Main : MonoBehaviour
{


    void Start()
    {
        Globals.MainCamera = GameObject.Find("Main Camera");
        Globals.MainWindow = Globals.MainCamera.GetComponent<NoesisView>().Content as MainWindow;

        Globals.MainWindow.Loaded += (object sender, RoutedEventArgs e) => 
        {
            Globals.MainViewModel = Globals.MainWindow.DataContext as ViewModel;
        };
    }

    void Update()
    {
        
    }
}
