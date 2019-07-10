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


namespace Media_Player
{
	/// <summary>
	/// Interaction logic for TEST.xaml
	/// </summary>
	public partial class TEST : Window
	{
		public TEST()
		{
			InitializeComponent();
		}


		private void Button_Click(object sender, RoutedEventArgs e)
		{
			/*var cmd = new Process();
			cmd.StartInfo.FileName = "cmd";
			cmd.StartInfo.UseShellExecute = false;
			cmd.StartInfo.RedirectStandardInput = true;
			cmd.StartInfo.RedirectStandardOutput = true;
			cmd.StartInfo.WorkingDirectory = @"C:\Program Files\Windows Media Player";
			cmd.Start();


			string arg = @"E:\Ai kho vi ai.mp3";
			cmd.StandardInput.WriteLine($".\\wmplayer.exe \"{arg}\"");
			cmd.StandardInput.Flush();










			MessageBox.Show(cmd.StandardOutput.ReadToEnd());*/









			string arg = @"E:\Ai kho vi ai.mp3";
			Process.Start(@"C:\Program Files\Windows Media Player\wmplayer.exe", $"\"{arg}\"");
		}
	}
}
