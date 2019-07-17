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
using System.Windows.Threading;


namespace MediaPlayer
{
	public partial class MainWindow : Window
	{
		public static MainWindow instance { get; private set; }



		#region KHỞI TẠO
		public MainWindow()
		{
			if (instance == null) instance = this; else throw new Exception();
			InitializeComponent();
			fileList_ScrollViewer = fileList.GetChild<ScrollViewer>();
			fileList.Items.CurrentChanged += (object sender, EventArgs e) => lastIndex = -1;
			Width = SystemParameters.WorkArea.Width;
			Height = SystemParameters.WorkArea.Height;
			Left = 0;
			Top = 0;
			Database.instance.voiceListenerInitialized += () => Dispatcher.Invoke(() => voiceButton.Visibility = Visibility.Visible);
		}


		private bool refreshingDatabase;

		private bool RefreshDatabase()
		{
			refreshingDatabase = true;
			var dialog = new winform.FolderBrowserDialog() { ShowNewFolderButton = false, SelectedPath = App.instance.path };
			if (dialog.ShowDialog() == winform.DialogResult.OK)
			{
				if (!Database.instance.Refresh(dialog.SelectedPath))
				{
					MessageBox.Show("Xảy ra lỗi ! Không có permission để truy cập thư mục !");
					refreshingDatabase = false;
					return false;
				}
				App.instance.path = dialog.SelectedPath;
				App.Watcher.instance?.Dispose();
				new App.Watcher();
				Close();
				new MainWindow().Show();
				refreshingDatabase = false;
				return true;
			}
			refreshingDatabase = false;
			return false;
		}
		#endregion


		#region WINDOW EVENT
		private void Window_Activated(object sender, EventArgs e)
		{
			if (refreshingDatabase) return;
			if (!App.instance.CheckReset())
				if (Database.instance.rootFolder != null) UpdateFolderTree();
				else while (!RefreshDatabase()) ;
		}


		private bool manualClose;

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (e.Cancel = !manualClose) return;
			cancelSource.Cancel(); cancelListening.Cancel();
		}


		public new void Close()
		{
			instance = null;
			manualClose = true;
			base.Close();
		}


		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift) && Keyboard.IsKeyDown(Key.LeftAlt)) RefreshDatabase();
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
			if (searchBox.Text != "") searchBox.Text = "";
			else UpdateFileList();
		}
		#endregion


		#region FILE LIST
		private ScrollViewer fileList_ScrollViewer;
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
			fileList_ScrollViewer.ScrollToLeftEnd();
		}



		private int lastIndex = -1;

		private void FileList_KeyDown(object sender, KeyEventArgs e)
		{
			if (Keyboard.IsKeyDown(Key.Enter)) { Play(); return; }

			// Cuộn đến item có content chứa kí tự vừa nhập
			char c = default;
			if (Key.A <= e.Key && e.Key <= Key.Z) c = e.Key.ToString()[0];
			else if (Key.D0 <= e.Key && e.Key <= Key.D9) c = e.Key.ToString()[1];
			else if (Key.NumPad0 <= e.Key && e.Key <= Key.NumPad9) c = e.Key.ToString()[6];
			else return;

			for (int i = lastIndex + 1; i < fileList.Items.Count; ++i)
			{
				var item = fileList.Items[i] as ListViewItem;
				string text = item.Content as string;
				foreach (char c2 in text) if (c2 == c)
					{
						fileList.ScrollIntoView(item);
						item.Focus();
						item.IsSelected = true;
						lastIndex = i;
						return;
					}
			}
			lastIndex = -1;
		}


		private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			if ((fileList.SelectedItem as ListBoxItem)?.IsMouseOver == true) Play();
		}
		#endregion


		#region Ô TÌM KIẾM
		private void TextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (Keyboard.IsKeyDown(Key.Enter)) SearchYoutube(Database.CreateKeyword(searchBox.Text));
		}


		private void SearchYoutube(string textToSearch)
		{
			char[] array = textToSearch.ToCharArray();
			for (int i = 0; i < array.Length; ++i) if (array[i] == ' ') array[i] = '+';
			textToSearch = new string(array);
			var p = new Process();
			p.StartInfo.FileName = "chrome";
			p.StartInfo.Arguments = $@"https://www.youtube.com/results?search_query={textToSearch} --parent-window --start-maximized";
			p.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
			p.Start();
		}


		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			e.Handled = true;
			cancelSource.Cancel();
			cancelSource = new CancellationTokenSource();
			if (searchBox.Text == "")
			{
				// Wait to restore content of the current selected folder.
				((Action<CancellationToken>)(async (CancellationToken token) =>
				{
					await Task.Delay(500);
					if (!token.IsCancellationRequested) UpdateFileList();
				}))(cancelSource.Token);
				return;
			}

			Search(searchBox.Text);
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
				searchBox.Text = Database.CreateKeyword(textToSearch);
				searchBox.CaretIndex = searchBox.Text.Length;
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
							if (!token.IsCancellationRequested)
							{
								fileList.Items.Add(new ListViewItem() { Content = Path.GetFileName(filePath), ToolTip = Path.GetDirectoryName(filePath) });
								fileList_ScrollViewer.ScrollToLeftEnd();
							}
						});
					}, token);
			});
		}


		private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
		{
			e.Handled = true;
			searchBox.Background = new SolidColorBrush(Colors.Green);
		}


		private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
		{
			e.Handled = true;
			searchBox.Background = new SolidColorBrush(Colors.White);
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
			if (result != "") searchBox.Text = result;
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
			var item = fileList.SelectedItem as ListViewItem;
			if (item != null) Process.Start(@"C:\Program Files\Windows Media Player\wmplayer.exe", $"\"{$@"{item.ToolTip}\{item.Content}"}\"  /fullscreen");
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


		public static T GetChild<T>(this Visual element) where T : Visual
		{
			if (element == null) return null;
			var type = typeof(T);
			if (element.GetType() == type) return element as T;
			Visual foundElement = null;
			if (element is FrameworkElement) (element as FrameworkElement).ApplyTemplate();
			for (int i = VisualTreeHelper.GetChildrenCount(element) - 1; i >= 0; --i)
			{
				var visual = VisualTreeHelper.GetChild(element, i) as Visual;
				foundElement = GetChild<T>(visual);
				if (foundElement != null) break;
			}
			return foundElement as T;
		}
	}
}