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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Collections.ObjectModel;


namespace MediaPlayer
{
	public partial class TEST : Window
	{
		public TEST()
		{
			InitializeComponent();
			MessageBox.Show(GetDescendantByType<ScrollViewer>(listView)?.ToString());
		}


		public static T GetDescendantByType<T>(Visual element) where T : Visual
		{
			if (element == null)
			{
				return null;
			}
			var type = typeof(T);
			if (element.GetType() == type)
			{
				return element as T;
			}
			Visual foundElement = null;
			if (element is FrameworkElement)
			{
				(element as FrameworkElement).ApplyTemplate();
			}
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
			{
				Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
				foundElement = GetDescendantByType<T>(visual);
				if (foundElement != null)
				{
					break;
				}
			}
			return foundElement as T;
		}
	}
}
