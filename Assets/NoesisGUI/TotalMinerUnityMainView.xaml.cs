#if UNITY_5_3_OR_NEWER
#define NOESIS
using Noesis;
#else
using System;
using System.Windows.Controls;
#endif

namespace TotalMinerUnity
{
    /// <summary>
    /// Interaction logic for TotalMinerUnityMainView.xaml
    /// </summary>
    public partial class TotalMinerUnityMainView : UserControl
    {
        public TotalMinerUnityMainView()
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
