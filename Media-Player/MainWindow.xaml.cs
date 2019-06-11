using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using winform = System.Windows.Forms;


namespace MediaPlayer
{
	public partial class MainWindow : Window
	{
		public class FolderItem : TreeViewItem
		{
			public FolderItem parent;
			public Folder folder;
		}


		public MainWindow()
		{
			InitializeComponent();
			if (Database.instance != null) UpdateFolderTree();
		}



		private void UpdateFolderTree()
		{
			void Swap<T>(ref T _obj1, ref T _obj2)
			{
				var t = _obj1; _obj1 = _obj2; _obj2 = t;
			}

			var a = new List<Folder>() { Database.instance.rootFolder };
			var b = new List<Folder>();
			var rootItem = new FolderItem() { Header = Path.GetFileName(a[0].path), folder = a[0] };
			var dictA = new Dictionary<Folder, FolderItem>() { [a[0]] = rootItem };
			var dictB = new Dictionary<Folder, FolderItem>();

			do
			{
				foreach (var folder in a)
				{
					var item = dictA[folder];
					foreach (var childFolder in folder.children)
					{
						var childItem = new FolderItem() { Header = Path.GetFileName(childFolder.path), folder = childFolder, parent = item };
						item.Items.Add(childItem);
						dictB[childFolder] = childItem;
						b.Add(childFolder);
					}
				}

				Swap(ref a, ref b); b.Clear();
				Swap(ref dictA, ref dictB); dictB.Clear();
			} while (a.Count != 0);

			folderTreeView.Items.Clear();
			folderTreeView.Items.Add(rootItem);
			rootItem.IsSelected = true;
		}


		private void UpdateFileList()
		{
			fileListBox.Items.Clear();
			var folder = (folderTreeView.SelectedItem as FolderItem)?.folder;
			if (folder == null) return;

			foreach (string f in folder.files) fileListBox.Items.Add(new ListBoxItem() { Content = Path.GetFileName(f), ToolTip = Path.GetDirectoryName(f) });
		}


		private void FolderTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			e.Handled = true;
			if (textBox.Text != "") textBox.Text = "";
			else UpdateFileList();
		}


		private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			e.Handled = true;
			if (Keyboard.IsKeyDown(Key.Left) || Keyboard.IsKeyDown(Key.Right) || Keyboard.IsKeyDown(Key.Up) || Keyboard.IsKeyDown(Key.Down)) return;
			if (fileListBox.SelectedItem is ListBoxItem item) new PlayerWindow($@"{item.ToolTip}\{item.Content.ToString()}").Show();
		}


		private void FileListBox_KeyDown(object sender, KeyEventArgs e)
		{
			e.Handled = true;
			if (Keyboard.IsKeyDown(Key.Enter))
			{
				var item = fileListBox.SelectedItem as ListBoxItem;
				new PlayerWindow($@"{item.ToolTip}\{item.Content.ToString()}").Show();
			}
		}


		private void FileListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			var item = fileListBox.SelectedItem as ListBoxItem;
			if (item?.IsMouseOver == true) new PlayerWindow($@"{item.ToolTip}\{item.Content.ToString()}").Show();
		}


		private void TextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift) && Keyboard.IsKeyDown(Key.LeftAlt))
			{
				var dialog = new winform.FolderBrowserDialog() { ShowNewFolderButton = false };
				if (dialog.ShowDialog() == winform.DialogResult.OK)
				{
					Application.Current.Properties[App.ROOT_FOLDER_KEY] = dialog.SelectedPath;
					App.Restart();
				}
			}
		}


		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			cancelSearching.Cancel();
			cancelSearching = new CancellationTokenSource();
		}


		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			e.Handled = true;
			cancelSearching.Cancel();
			cancelSearching = new CancellationTokenSource();
			if (textBox.Text == "")
			{
				// Restore content of the current selected folder.
				UpdateFileList();
				return;
			}

			Search(textBox.Text, cancelSearching.Token);
		}


		private CancellationTokenSource cancelSearching = new CancellationTokenSource();

		private async void Search(string textToSearch, CancellationToken token)
		{
			await Task.Delay(500);
			if (token.IsCancellationRequested) return;

			if (!Database.IsValidKeyword(textToSearch))
			{
				textBox.Text = Database.CreateKeyword(textToSearch);
				textBox.CaretIndex = textBox.Text.Length;
				return;
			}

			// ==================  Search Database  =================================
			fileListBox.Items.Clear();
			var task = Task.Run(() =>
			  {
				  // Thread Pool
				  Database.instance.Search(textToSearch,
					  (string filePath, float percent) =>
					  {
						  Dispatcher.Invoke(() =>
						  {
							  // GUI Thread
							  if (!token.IsCancellationRequested)
								  fileListBox.Items.Add(new ListBoxItem() { Content = Path.GetFileName(filePath), ToolTip = Path.GetDirectoryName(filePath) });
						  });
					  }, token);
			  });
		}
	}
}
