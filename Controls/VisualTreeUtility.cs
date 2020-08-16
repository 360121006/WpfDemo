using System.Windows;
using System.Windows.Media;

namespace Controls
{
    internal class VisualTreeUtility
    {
        public static T FindVisualChild<T>(DependencyObject obj,string targetName) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T item )
                {
                    var nameProperty = item.GetType().GetProperty("Name");
                    var value = nameProperty.GetValue(item, null);
                    if (targetName == value.ToString())
                    {
                        return item;
                    }
                }
                T childOfChild = FindVisualChild<T>(child,targetName);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
    }
}
