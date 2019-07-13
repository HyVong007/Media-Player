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

		private readonly ListViewItem[] source = new ListViewItem[]
		{
			new ListViewItem(){Content="Haha"},
			new ListViewItem(){Content="Keke"}
		};

		public TEST()
		{
			InitializeComponent();
			listView.Items.Add(new ListViewItem() { Content = "nguyen thanh tam" });
			listView.ItemsSource = source;
		}


		private void Button_Click(object sender, RoutedEventArgs e)
		{
			//listView.ItemsSource = null;
		}
	}
}
