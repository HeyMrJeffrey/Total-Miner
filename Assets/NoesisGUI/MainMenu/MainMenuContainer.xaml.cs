
#if UNITY_5_3_OR_NEWER
#define NOESIS
using Noesis;
using UnityEngine;
#else
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
#endif

namespace TotalMinerUnity.Menus
{
    public partial class MainMenuContainer : UserControl
    {
        string SomeImage3 = "Assets/NoesisGUI/MainMenu/Images/BackgroundScreen6.png";


        public MainMenuContainer()
        {
            this.InitializeComponent();
        }

        public void test()
        {
#if NOESIS
            Debug.Log("hit");
#endif
        }


#if NOESIS
        private void InitializeComponent()
        {
            NoesisUnity.LoadComponent(this);
        }
#endif


        public void Test(object parameter)
        {
#if NOESIS
            Debug.Log("hit");
#endif
        }



    }

    }