
#if UNITY_5_3_OR_NEWER
#define NOESIS
using Noesis;
#else
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
#endif

namespace TotalMinerUnity.Menus
{
    /// <summary>
    /// Interaction logic for MainMenuContainer.xaml
    /// </summary>
    public partial class MainMenuContainer : UserControl
    {

        public MainMenuContainer()
        {
            this.InitializeComponent();
        }


#if NOESIS
        private void InitializeComponent()
        {
            NoesisUnity.LoadComponent(this);
        }

#endif
    }

}