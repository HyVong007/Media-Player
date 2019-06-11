using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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


namespace MediaPlayer
{
	public partial class TestWindow : Window
	{
		public TestWindow()
		{
			InitializeComponent();
		}


		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Process.Start(Assembly.GetEntryAssembly().Location);
			Application.Current.Shutdown();
		}
	}
}
