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
    /// Interaction logic for SubMenuTest.xaml
    /// </summary>
    public partial class SubMenuTest : UserControl
    {
        public SubMenuTest()
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
