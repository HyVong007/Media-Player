using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using winform = System.Windows.Forms;
using System;
using System.Windows.Media;
using System.Diagnostics;


namespace MediaPlayer
{
	public partial class MainWindow : Window
	{
		private static MainWindow instance;

		#region KHỞI TẠO
		static MainWindow()
		{
			Database.initializeCompleted += () => instance.Dispatcher.Invoke(() =>
			{
				if (Database.instance.rootFolder == null) return;
				instance.UpdateFolderTree();
				PopupWaiting.instance.Close();
				instance.IsEnabled = true;
			});
		}


		public MainWindow()
		{
			instance = this;
			InitializeComponent();
			Database.voiceListenerInitialized += () => Dispatcher.Invoke(() => voiceButton.Visibility = Visibility.Visible);
			if (App.Current.Properties.Contains(App.PATH_KEY))
			{
				IsEnabled = false;
				new PopupWaiting().Show();
			}
			Task.Delay(1).ContinueWith((Task task) => new Database());
		}
		#endregion


		#region FOLDER TREE
		private void UpdateFolderTree()
		{
			void Swap<T>(ref T _obj1, ref T _obj2)
			{
				var t = _obj1; _obj1 = _obj2; _obj2 = t;
			}

			var a = new List<Database.Folder>() { Database.instance.rootFolder };
			var b = new List<Database.Folder>();
			var rootItem = new FolderItem() { Header = Path.GetFileName(a[0].path), folder = a[0] };
			var dictA = new Dictionary<Database.Folder, FolderItem>() { [a[0]] = rootItem };
			var dictB = new Dictionary<Database.Folder, FolderItem>();

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

			folderTreeView.Clear();
			folderTreeView.Items.Add(rootItem);
			rootItem.IsSelected = true;
		}


		private void FolderTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{
			e.Handled = true;
			if (textBox.Text != "") textBox.Text = "";
			else UpdateFileList();
		}
		#endregion


		#region FILE LIST
		private readonly Dictionary<FolderItem, ListViewItem[]> folder_file_dict = new Dictionary<FolderItem, ListViewItem[]>();

		private void UpdateFileList()
		{
			fileList.Clear();
			var folderItem = folderTreeView.SelectedItem as FolderItem;
			if (folderItem == null) return;
			if (folder_file_dict.ContainsKey(folderItem)) fileList.ItemsSource = folder_file_dict[folderItem];
			else
			{
				var files = folderItem.folder.files;
				var source = new ListViewItem[files.Length];
				for (int i = 0; i < files.Length; ++i)
				{
					string filePath = files[i];
					source[i] = new ListViewItem() { Content = Path.GetFileName(filePath), ToolTip = Path.GetDirectoryName(filePath) };
				}
				fileList.ItemsSource = folder_file_dict[folderItem] = source;
			}
		}


		private void FileList_KeyDown(object sender, KeyEventArgs e)
		{
			e.Handled = true;
			if (Keyboard.IsKeyDown(Key.Enter)) Play();
		}


		private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			var item = fileList.SelectedItem as ListBoxItem;
			if (item?.IsMouseOver == true) Play();
		}
		#endregion


		#region Ô TÌM KIẾM
		private void TextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift) && Keyboard.IsKeyDown(Key.LeftAlt))
			{
				var dialog = new winform.FolderBrowserDialog() { ShowNewFolderButton = false };
				if (dialog.ShowDialog() == winform.DialogResult.OK)
				{
					Database.instance.Refresh(dialog.SelectedPath);
					App.Restart();
				}
			}
			else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift) && Keyboard.IsKeyDown(Key.Space))
			{
				Database.instance.Refresh();
				App.Restart();
			}
			else if (Keyboard.IsKeyDown(Key.Enter)) Task.Delay(1).ContinueWith((Task task) => Dispatcher.Invoke(fileList.Focus));
		}


		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			e.Handled = true;
			cancelSource.Cancel();
			cancelSource = new CancellationTokenSource();
			if (textBox.Text == "")
			{
				// Wait to restore content of the current selected folder.
				((Action<CancellationToken>)(async (CancellationToken token) =>
				{
					await Task.Delay(500);
					if (!token.IsCancellationRequested) UpdateFileList();
				}))(cancelSource.Token);
				return;
			}

			Search(textBox.Text);
		}


		private CancellationTokenSource cancelSource = new CancellationTokenSource();

		/// <summary>
		/// Gọi bằng GUI Thread. Dùng worker thread để tìm kiếm, có thể cancel.
		/// </summary>
		/// <param name="textToSearch"></param>
		private async void Search(string textToSearch)
		{
			var token = cancelSource.Token;
			await Task.Delay(500);
			if (token.IsCancellationRequested) return;

			if (!Database.IsValidKeyword(textToSearch))
			{
				textBox.Text = Database.CreateKeyword(textToSearch);
				textBox.CaretIndex = textBox.Text.Length;
				return;
			}

			// ==================  Search Database  =================================
			fileList.Clear();
			var task = Task.Run(() =>
			{
				Database.instance.Search(textToSearch,
					(string filePath, float percent) =>
					{
						Dispatcher.Invoke(() =>
						{
							if (!token.IsCancellationRequested) fileList.Items.Add(new ListViewItem() { Content = Path.GetFileName(filePath), ToolTip = Path.GetDirectoryName(filePath) });
						});
					}, token);
			});
		}
		#endregion


		#region ICON MICROPHONE
		private Task<string> listening;
		private CancellationTokenSource cancelListening = new CancellationTokenSource();

		private async void VoiceButton_Click(object sender, RoutedEventArgs e)
		{
			if (listening?.IsCompleted == false) return;

			var oldBrush = voiceButton.Background;
			voiceButton.Background = new SolidColorBrush(Color.FromRgb(0, 255, 0));
			string result = await (listening = Database.instance.Listen(cancelListening.Token));
			voiceButton.Background = oldBrush;
			if (result != "") textBox.Text = result;
		}


		private void VoiceButton_LostFocus(object sender, RoutedEventArgs e)
		{
			if (listening != null)
			{
				if (!listening.IsCompleted)
				{
					cancelListening.Cancel(); cancelListening = new CancellationTokenSource();
				}
				listening = null;
			}
		}
		#endregion


		private void Play()
		{
			var item = fileList.SelectedItem as ListBoxItem;
			if (item != null) Process.Start(@"C:\Program Files\Windows Media Player\wmplayer.exe", $"\"{$@"{item.ToolTip}\{item.Content}"}\" /fullscreen");
		}


		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			cancelSource.Cancel(); cancelListening.Cancel();
		}
	}



	public class FolderItem : TreeViewItem
	{
		public FolderItem parent;
		public Database.Folder folder;
	}



	public static class Extension
	{
		public static void Clear(this ItemsControl itemsControl)
		{
			if (itemsControl.ItemsSource != null) itemsControl.ItemsSource = null;
			if (itemsControl.Items.Count != 0) itemsControl.Items.Clear();
		}
	}
}