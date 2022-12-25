#if UNITY_5_3_OR_NEWER
#define NOESIS
using Noesis;
using System.ComponentModel;
using System.Windows.Input;
using Unity.VisualScripting;
using UnityEngine;
#else
using System;
using System.Windows.Controls;
#endif

namespace TotalMinerUnity
{
    /// <summary>
    /// Interaction logic for MainMenu.xaml
    /// </summary>
    public partial class MainMenu : UserControl
    {

        

        public MainMenu()
        {
            

            InitializeComponent();
        }

#if NOESIS
        private void InitializeComponent()
        {
            NoesisUnity.LoadComponent(this);
        }
#endif




        
    }
}
