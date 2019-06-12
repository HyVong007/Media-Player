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


namespace MediaPlayer
{
	public partial class PlayerWindow : Window
	{
		private static PlayerWindow instance;


		public PlayerWindow(string path)
		{
			InitializeComponent();
			instance?.Close();
			instance = this;
			player.Source = new Uri(@path);
			Title = $"Đang phát: {System.IO.Path.GetFileName(path)}";
		}


		private void Window_Activated(object sender, EventArgs e)
		{
			player.Play();
		}


		private void Window_Deactivated(object sender, EventArgs e)
		{
			player.Pause();
		}


		private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			WindowStyle = (WindowStyle == WindowStyle.None) ? WindowStyle.SingleBorderWindow : WindowStyle.None;
			WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
		}
	}
}
