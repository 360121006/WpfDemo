using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Controls
{
    /// <summary>
    /// DynamicMagnet.xaml 的交互逻辑
    /// </summary>
    public partial class DynamicMagnet : UserControl
    {
        public DynamicMagnet()
        {
            InitializeComponent();
            DataContext = this;
        }

        private bool animationCompleteFlag = true;

        public static readonly DependencyProperty ImageSourcesProperty = DependencyProperty.Register("ImageSources", typeof(ICollection<BitmapImage>), typeof(DynamicMagnet), new PropertyMetadata(ImageSourcesChangedCallback));
        public ICollection<BitmapImage> ImageSources
        {
            get { return (ICollection<BitmapImage>)GetValue(ImageSourcesProperty); }
            set { SetValue(ImageSourcesProperty, value); }
        }

        private static void ImageSourcesChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DynamicMagnet dynamicMagnet = d as DynamicMagnet;
            var imageContainer = VisualTreeUtility.FindVisualChild<StackPanel>(dynamicMagnet,"ImageContainer");
            int count = imageContainer.Children.Count;
            imageContainer.Children.RemoveRange(0, count);
            var canvas = imageContainer.Parent as Canvas;
            foreach (var bitmapImage in e.NewValue as ICollection<BitmapImage>)
            {
                var image = new Image
                {
                    Source = bitmapImage,
                    Width = canvas.ActualWidth,
                    Height = canvas.ActualHeight,
                    Stretch = Stretch.UniformToFill,
                };
                imageContainer.Children.Add(image);
            }
            imageContainer.Children.Add(new Image
            {
                Source = (e.NewValue as ICollection<BitmapImage>).First(),
                Width = canvas.ActualWidth,
                Height = canvas.ActualHeight,
                Stretch = Stretch.UniformToFill
            });

        }

        private void ImageContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!animationCompleteFlag) return;
            animationCompleteFlag = false;
            var imageContainer = sender as StackPanel;
            var canvas = imageContainer.Parent as Canvas;
            DoubleAnimation animation;
            animation = new DoubleAnimation
            {
                By = -1 * canvas.ActualWidth,
                Duration = new Duration(TimeSpan.FromSeconds(1)),
                EasingFunction = new PowerEase()
                {
                    EasingMode = EasingMode.EaseInOut
                }
            };
            animation.Completed += Animation_Completed;
            imageContainer.BeginAnimation(Canvas.LeftProperty, animation);
        }

        private void Animation_Completed(object sender, EventArgs e)
        {
            if (Canvas.GetLeft(ImageContainer) <= canvas.ActualWidth * -1 * (ImageContainer.Children.Count - 1))
            {
                ImageContainer.BeginAnimation(Canvas.LeftProperty, null);
                Canvas.SetLeft(ImageContainer, 0);
            }
            animationCompleteFlag = true;
        }
    }
}
