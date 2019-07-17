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
using System.IO;
using System.Threading;
using System.IO.IsolatedStorage;


namespace MediaPlayer
{
	public partial class TEST : Window
	{
		public TEST()
		{
			InitializeComponent();
		}


		const string FILE = "TAM.TXT";

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			using (var storage = IsolatedStorageFile.GetUserStoreForDomain())
			using (var stream = new IsolatedStorageFileStream(FILE, FileMode.Create, storage))
			{
				stream.Close();

				MessageBox.Show(storage.FileExists(FILE).ToString());
				storage.Close();
			}
		}


		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			using (var storage = IsolatedStorageFile.GetUserStoreForDomain())
			{
				storage.DeleteFile(FILE);
				MessageBox.Show(storage.FileExists(FILE).ToString());
				storage.Close();
			}
		}
	}
}
