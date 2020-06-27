using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WaveButton
{
    /// <summary>
    /// WaveButton.xaml 的交互逻辑
    /// </summary>
    public partial class MyWaveButton : Button
    {
        public MyWaveButton()
        {
            InitializeComponent();
        }

        private void Grid_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            var grid = sender as Grid;
            var target = Template.FindName("MyEllipseGeometry", this) as EllipseGeometry;
            target.Center = Mouse.GetPosition(this);
            var animation = new DoubleAnimation
            {
                From = 0,
                To = grid.ActualWidth*2,
                Duration = new Duration(TimeSpan.FromSeconds(1))
            };
            target.BeginAnimation(EllipseGeometry.RadiusXProperty, animation);

            var targetPath = Template.FindName("MyPath", this) as Path;
            var animation2 = new DoubleAnimation
            {
                From = 0.2,
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(1))
            };
            targetPath.BeginAnimation(OpacityProperty, animation2);

        }
    }
}
