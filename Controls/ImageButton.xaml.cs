using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace Controls
{
    /// <summary>
    /// PicButton.xaml 的交互逻辑
    /// </summary>
    public partial class ImageButton
    {
        public ImageButton()
        {
            InitializeComponent();

        }

        public static readonly DependencyProperty WaveBackgroundProperty = DependencyProperty.Register("WaveBackground", typeof(Brush), typeof(ImageButton), new PropertyMetadata(new SolidColorBrush(Colors.Black)));
        public Brush WaveBackground
        {
            get { return (Brush)GetValue(WaveBackgroundProperty); }
            set { SetValue(WaveBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ImageBackgroundProperty = DependencyProperty.Register("ImageBackground", typeof(Brush), typeof(ImageButton), new PropertyMetadata(new SolidColorBrush(Colors.Black)));
        public Brush ImageBackground
        {
            get { return (Brush)GetValue(ImageBackgroundProperty); }
            set { SetValue(ImageBackgroundProperty, value); }
        }

        public static readonly DependencyProperty ContentMarginProperty = DependencyProperty.Register("ContentMargin", typeof(Thickness), typeof(ImageButton), new PropertyMetadata(new Thickness(0)));
        public Thickness ContentMargin
        {
            get { return (Thickness)GetValue(ContentMarginProperty); }
            set { SetValue(ContentMarginProperty, value); }
        }

        public static readonly DependencyProperty ImageMarginProperty = DependencyProperty.Register("ImageMargin", typeof(Thickness), typeof(ImageButton), new PropertyMetadata(new Thickness(0)));
        public Thickness ImageMargin
        {
            get { return (Thickness)GetValue(ImageMarginProperty); }
            set { SetValue(ImageMarginProperty, value); }
        }

        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(ImageButton), new PropertyMetadata(new CornerRadius(2)));
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        public static readonly DependencyProperty ImageStyleProperty = DependencyProperty.Register("ImageStyle", typeof(Style), typeof(ImageButton));
        public Style ImageStyle
        {
            get { return (Style)GetValue(ImageStyleProperty); }
            set { SetValue(ImageStyleProperty, value); }
        }

        private void Grid_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as Grid;
            var target = Template.FindName("MyEllipseGeometry", this) as EllipseGeometry;
            target.Center = Mouse.GetPosition(this);
            var animation = new DoubleAnimation
            {
                From = 0,
                To = grid.ActualWidth * 2,
                Duration = new Duration(TimeSpan.FromSeconds(1)),
                //EasingFunction = new PowerEase()
                //{
                //    EasingMode = EasingMode.EaseIn
                //}
            };

            var targetPath = Template.FindName("MyPath", this) as Path;
            var animation2 = new DoubleAnimation
            {
                From = 0.3,
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(1)),
            };
            target.BeginAnimation(EllipseGeometry.RadiusXProperty, animation);
            targetPath.BeginAnimation(OpacityProperty, animation2);

        }
    }
}
