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

namespace Test
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitControl(key1);
            InitControl(key2); 
            InitControl(key3);

            borders.Add(bd1);
            borders.Add(bd2);
            borders.Add(bd3);
            borderMap.Add(bd1, key1);
            borderMap.Add(bd2, key2);
            borderMap.Add(bd3, key3);
        }

        private Dictionary<string, Point> pointDic = new Dictionary<string, Point>();
        private Dictionary<string, Position> positionDic = new Dictionary<string, Position>();
        private List<Border> borders = new List<Border>();
        private Dictionary<Border, Grid> borderMap = new Dictionary<Border, Grid>();

        private void InitControl(Grid grid)
        {
            grid.MouseLeftButtonDown += keyb_MouseLeftButtonDown;
            grid.MouseMove += keyb_MouseMove;
            grid.MouseLeftButtonUp += keyb_MouseLeftButtonUp;
            pointDic.Add(grid.Name, new Point());
            positionDic.Add(grid.Name, new Position(Canvas.GetLeft(grid), Canvas.GetTop(grid)));

        }

        private void keyb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Grid uIElement = sender as Grid;
            pointDic[uIElement.Name] = e.GetPosition(null);
            Panel.SetZIndex(uIElement, 2);
            uIElement.CaptureMouse();
            //keyb.Cursor = Cursors.Hand;
        }

        private Border InBorder(Point position)
        {
            foreach (var border in borders)
            {
                double dx = Canvas.GetLeft(border);
                double dy = Canvas.GetTop(border);
                if (position.X >= dx && 
                    position.X <= dx + border.ActualWidth && 
                    position.Y >= dy &&
                    position.Y <= dy + border.ActualHeight)
                {
                    return border;
                }
            }
            return null;
        }

        private void keyb_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Grid uIElement = sender as Grid;
            Panel.SetZIndex(uIElement, 1);
            uIElement.ReleaseMouseCapture();
            Border border = InBorder(pointDic[uIElement.Name]);
            if (border!=null)
            {
                double dx = Canvas.GetLeft(border);
                double dy = Canvas.GetTop(border);
                Canvas.SetLeft(uIElement, dx);
                Canvas.SetTop(uIElement, dy);
                positionDic[uIElement.Name].CanvasLeft = dx;
                positionDic[uIElement.Name].CanvasTop = dy;
                //var border = GetMappingBorder(uIElement);
                //if (border != null)
                //{
                //    borderMap[border] = null;
                //    DoMove(border, MoveType.D);
                //}
                ReMapping(border, uIElement);
            }
            else
            {
                Position position = positionDic[uIElement.Name];
                Canvas.SetLeft(uIElement, position.CanvasLeft);
                Canvas.SetTop(uIElement, position.CanvasTop);
            }
        }

        private void ReMapping(Border currentBorder,Grid currentGrid)
        {
            var oldBorder = GetMappingBorder(currentGrid);
            int oldIndex = GetBorderIndex(oldBorder);
            int cIndex = GetBorderIndex(currentBorder);
            borderMap[oldBorder] = null;
            if (oldIndex < cIndex)//后方前移
            {
                for (int i = 0; i < cIndex; i++)
                {
                    if (borderMap.ElementAt(i).Value == null)
                    {
                        borderMap[borderMap.ElementAt(i).Key] = borderMap.ElementAt(i + 1).Value;
                        borderMap[borderMap.ElementAt(i + 1).Key] = null ;
                    }
                }
            }
            if (oldIndex > cIndex)
            {
                for (int i = borderMap.Count - 1; i > cIndex; i--)
                {
                    if (borderMap.ElementAt(i).Value == null)
                    {
                        borderMap[borderMap.ElementAt(i).Key] = borderMap.ElementAt(i - 1).Value;
                        borderMap[borderMap.ElementAt(i - 1).Key] = null;
                    }
                }
            }
            borderMap[currentBorder] = currentGrid;
            DoMove(currentGrid);
        }

        private void keyb_MouseMove(object sender, MouseEventArgs e)
        {
            Grid uIElement = sender as Grid;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                double dx = e.GetPosition(null).X - pointDic[uIElement.Name].X + Canvas.GetLeft(uIElement);
                double dy = e.GetPosition(null).Y - pointDic[uIElement.Name].Y + Canvas.GetTop(uIElement);
                //keyb.Margin = new Thickness(dx, dy, 0, 0);
                Canvas.SetLeft(uIElement, dx);
                Canvas.SetTop(uIElement, dy);
                pointDic[uIElement.Name] = e.GetPosition(null);
                //var border = GetMappingBorder(uIElement);
                //if (border != null)
                //{
                //    borderMap[border] = null;
                //    DoMove(border, MoveType.D);
                //}
            }
        }

        private Border GetMappingBorder(Grid grid)
        {
            foreach (var keyValuePair in borderMap)
            {
                if (keyValuePair.Value == grid)
                {
                    return keyValuePair.Key;
                }
            }
            return null;
        }

        private int GetBorderIndex(Border border)
        {
            int i = 0;
            foreach (var keyValuePair in borderMap)
            {
                if (keyValuePair.Key == border)
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        private void DoMove(Grid grid)
        {
            foreach (var item in borderMap)
            {
                if (grid == item.Value || item.Value ==null)
                {
                    continue;
                }
                double bx = Canvas.GetLeft(item.Key);
                double gx = Canvas.GetLeft(item.Value);
                if (bx !=gx )
                {
                    DoubleAnimation animation;
                    animation = new DoubleAnimation
                    {
                        By = bx - gx,
                        Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                        EasingFunction = new PowerEase()
                        {
                            EasingMode = EasingMode.EaseInOut
                        }
                    };
                    animation.Completed += Animation_Completed;
                    Storyboard.SetTarget(animation, item.Value);
                    item.Value.BeginAnimation(Canvas.LeftProperty, animation);
                }
            }
        }

        private void Animation_Completed(object sender, EventArgs e)
        {
            AnimationTimeline timeline = (sender as AnimationClock).Timeline;
            Grid uIElement = Storyboard.GetTarget(timeline) as Grid;
            positionDic[uIElement.Name].CanvasLeft = Canvas.GetLeft(uIElement);
            positionDic[uIElement.Name].CanvasTop = Canvas.GetTop(uIElement);
            uIElement.BeginAnimation(Canvas.LeftProperty, null);
            Canvas.SetLeft(uIElement, positionDic[uIElement.Name].CanvasLeft);

            //Border b=null;
            //foreach (var item in borderMap)
            //{
            //    var border = item.Key;
            //    if (Canvas.GetLeft(uIElement) == Canvas.GetLeft(border) &&
            //        Canvas.GetTop(uIElement) == Canvas.GetTop(border))
            //    {
            //        b = border;
            //    }
            //}
            //if (b != null)
            //{
            //    borderMap[b] = uIElement;
            //}
        }

    }

    internal enum MoveType
    {
        D,
        R
    }

    internal class Position
    {
        public Position(double canvasLeft,double canvasTop)
        {
            this.CanvasLeft = canvasLeft;
            this.CanvasTop = canvasTop;
        }

        public double CanvasLeft { get; set; }
        public double CanvasTop { get; set; }

    }
}
