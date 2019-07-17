using System;
using System.Windows;
using System.IO;
using System.IO.IsolatedStorage;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;


namespace MediaPlayer
{
	public partial class App : Application
	{
		public static App instance { get; private set; }

		private const string PATH_FILE = "PATH.TXT";
		private string _path;
		public string path
		{
			get
			{
				if (_path != null) return _path;
				var storage = IsolatedStorageFile.GetUserStoreForDomain();
				try
				{
					using (var stream = new IsolatedStorageFileStream(PATH_FILE, FileMode.Open, storage))
					using (var reader = new StreamReader(stream))
					{
						_path = reader.ReadLine();
						reader.Close();
					}
				}
				catch (Exception) { _path = ""; }
				finally { storage.Close(); }
				return _path;
			}

			set
			{
				if (_path == value) return;
				_path = value ?? throw new Exception();
				var storage = IsolatedStorageFile.GetUserStoreForDomain();
				using (var stream = new IsolatedStorageFileStream(PATH_FILE, FileMode.Create, storage))
				using (var writer = new StreamWriter(stream))
				{
					writer.WriteLine(_path);
					writer.Flush();
					writer.Close();
				}
				storage.Close();
			}
		}

		private const string OKSTATE_FILE = "OKSTATE.TXT";
		private bool? _okState;

		/// <summary>
		/// Trạng thái dữ liệu hiện tại/lần chạy trước đó có toàn vẹn hay không ?
		/// <para>True là toàn vẹn.</para>
		/// </summary>
		public bool okState
		{
			get
			{
				if (_okState != null) return _okState.Value;
				using (var storage = IsolatedStorageFile.GetUserStoreForDomain())
				{
					_okState = storage.FileExists(OKSTATE_FILE);
					storage.Close();
				}
				return _okState.Value;
			}

			set
			{
				if (_okState == value) return;
				_okState = value;
				using (var storage = IsolatedStorageFile.GetUserStoreForDomain())
				{
					if (value)
					{
						if (!storage.FileExists(OKSTATE_FILE)) using (var stream = new IsolatedStorageFileStream(OKSTATE_FILE, FileMode.Create, storage)) stream.Close();
					}
					else if (storage.FileExists(OKSTATE_FILE)) storage.DeleteFile(OKSTATE_FILE);
					storage.Close();
				}
			}
		}



		static App()
		{
			Database.initializeCompleted += () =>
			{
				instance.okState = true;
				PopupWaiting.instance.Close();
				if (Database.instance.rootFolder == null) { instance.path = ""; Watcher.instance?.Dispose(); }
				else new Watcher();
				new MainWindow().Show();
			};
		}


		public App()
		{
			instance = this;
			InitializeComponent();
		}


		public sealed class Watcher
		{
			public static Watcher instance { get; private set; }
			private readonly List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
			public readonly ConcurrentQueue<WatcherChangeTypes> queue = new ConcurrentQueue<WatcherChangeTypes>();



			public Watcher()
			{
				if (instance == null) instance = this; else throw new Exception();
				var e = Database.instance.GetFolders();
				while (e.MoveNext()) Register(e.Current.path);
			}


			private void Register(string path)
			{
				var watcher = new FileSystemWatcher(path);
				watcher.IncludeSubdirectories = false;
				watcher.Created += Watcher_Created;
				watcher.Deleted += Watcher_Deleted;
				watcher.Changed += Watcher_Changed;
				watcher.Renamed += Watcher_Renamed;
				watcher.Error += Watcher_Error;
				watchers.Add(watcher);
				watcher.EnableRaisingEvents = true;
			}


			public void Dispose()
			{
				instance = null;
				foreach (var w in watchers)
				{
					w.EnableRaisingEvents = false;
					w.Dispose();
				}
			}


			private void Watcher_Error(object sender, ErrorEventArgs e)
			{
				var w = sender as FileSystemWatcher;
				string path = w.Path;
				w.Dispose();
				watchers.Remove(w);
				if (Directory.Exists(path)) Register(path);
				queue.Enqueue(WatcherChangeTypes.All);
				App.Current.Dispatcher.Invoke(() =>
				{
					App.instance.okState = false;
					if (MediaPlayer.MainWindow.instance?.IsActive == true) App.instance.CheckReset();
				});
			}


			private void Watcher_Renamed(object sender, RenamedEventArgs e)
			{
				queue.Enqueue(WatcherChangeTypes.Renamed);
				App.Current.Dispatcher.Invoke(() =>
				{
					App.instance.okState = false;
					if (MediaPlayer.MainWindow.instance?.IsActive == true) App.instance.CheckReset();
				});
			}


			private void Watcher_Changed(object sender, FileSystemEventArgs e)
			{
				queue.Enqueue(WatcherChangeTypes.Changed);
				App.Current.Dispatcher.Invoke(() =>
				{
					App.instance.okState = false;
					if (MediaPlayer.MainWindow.instance?.IsActive == true) App.instance.CheckReset();
				});
			}


			private void Watcher_Deleted(object sender, FileSystemEventArgs e)
			{
				queue.Enqueue(WatcherChangeTypes.Deleted);
				App.Current.Dispatcher.Invoke(() =>
				{
					App.instance.okState = false;
					if (MediaPlayer.MainWindow.instance?.IsActive == true) App.instance.CheckReset();
				});
			}


			private void Watcher_Created(object sender, FileSystemEventArgs e)
			{
				queue.Enqueue(WatcherChangeTypes.Created);
				App.Current.Dispatcher.Invoke(() =>
				{
					App.instance.okState = false;
					if (MediaPlayer.MainWindow.instance?.IsActive == true) App.instance.CheckReset();
				});
			}
		}


		public bool CheckReset()
		{
			var watcher = Watcher.instance;
			if (watcher == null) return false;

			int count = watcher.queue.Count;
			if (count != 0)
			{
				for (int i = 0; i < count; ++i) watcher.queue.TryDequeue(out WatcherChangeTypes result);
				MediaPlayer.MainWindow.instance.Close();
				new PopupWaiting().Show();
				if (!Database.instance.Refresh(path)) throw new Exception();
				okState = true;
				PopupWaiting.instance.Close();
				new MainWindow().Show();
				return true;
			}
			return false;
		}


		private void Application_Startup(object sender, StartupEventArgs e)
		{
			// Chỉ cho phép 1 process chạy.
			if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1) { Shutdown(); return; }

			// Khởi tạo database
			new PopupWaiting().Show();
			PopupWaiting.instance.ContentRendered += (object _sender, EventArgs _e) => new Database(path, !okState);
		}


		private void Application_Exit(object sender, ExitEventArgs e)
		{
			Watcher.instance?.Dispose();
			if (!okState)
			{
				Database.instance.Refresh(path);
				okState = true;
			}
		}
	}
}
