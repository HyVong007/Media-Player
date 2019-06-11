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
	/// <summary>
	/// Interaction logic for TestWindow.xaml
	/// </summary>
	public partial class TestWindow : Window
	{
		public TestWindow()
		{
			InitializeComponent();
			var prop = Application.Current.Properties;
			textBox.Text = prop[App.ROOT_FOLDER_KEY] as string;
		}


		private void TextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			Application.Current.Properties[App.ROOT_FOLDER_KEY] = (sender as TextBox).Text;
		}
	}
}
