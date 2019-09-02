using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading;
using System.Threading.Tasks;


namespace MediaPlayer
{
	/// <summary>
	/// Đăng kí quan sát các folder và thực hiện cập nhật rootFolder và database.
	/// </summary>
	public sealed class Watcher
	{
		public static Watcher instance { get; private set; }
		private readonly List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
		private readonly ConcurrentQueue<WatcherChangeTypes> queue = new ConcurrentQueue<WatcherChangeTypes>();

		#region << okState >>
		private const string OKSTATE_FILE = "OKSTATE.TXT";
		private static bool? _okState;

		/// <summary>
		/// Trạng thái dữ liệu hiện tại/lần chạy trước đó có toàn vẹn hay không ?
		/// <para>True là toàn vẹn.</para>
		/// </summary>
		public static bool okState
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
		#endregion



		public Watcher()
		{
			if (instance == null) instance = this; else throw new Exception();
			Register();
		}


		private bool Register(string path)
		{
			FileSystemWatcher watcher = null;
			try
			{
				watcher = new FileSystemWatcher(path);
			}
			catch (Exception)
			{
				watcher?.Dispose();
				return false;
			}

			watcher.IncludeSubdirectories = false;
			watcher.Created += Watcher_Created;
			watcher.Deleted += Watcher_Deleted;
			watcher.Changed += Watcher_Changed;
			watcher.Renamed += Watcher_Renamed;
			watcher.Error += Watcher_Error;
			watchers.Add(watcher);
			watcher.EnableRaisingEvents = true;
			return true;
		}


		private async void Register()
		{
			var unregs = new List<Database.Folder>();
			foreach (var folder in Database.instance) unregs.Add(folder);

			var ready = new Dictionary<string, bool>()
			{
				[@"C:\"] = false,
				[@"D:\"] = false,
				[@"E:\"] = false
			};

			while (true)
			{
				DriveInfo[] drives;
				try
				{
					drives = DriveInfo.GetDrives();
				}
				catch (Exception) { await Task.Delay(1); if (token.IsCancellationRequested) break; continue; }

				foreach (var drive in drives)
					if (ready.ContainsKey(drive.Name) && !ready[drive.Name] && drive.IsReady)
					{
						ready[drive.Name] = drive.IsReady;
						var t = new List<Database.Folder>();
						foreach (var folder in unregs) if (!Register(folder.path)) t.Add(folder);
						unregs = t;
					}

				if (ready[@"C:\"] && ready[@"D:\"] && ready[@"E:\"]) break;
				await Task.Delay(1);
				if (token.IsCancellationRequested) break;
			}
		}


		#region << Xóa đối tượng và hủy các task của đối tượng >>
		private static CancellationTokenSource cancelSource = new CancellationTokenSource();
		private readonly CancellationToken token = cancelSource.Token;

		public void Dispose()
		{
			instance = null;
			cancelSource.Cancel();
			cancelSource = new CancellationTokenSource();
			foreach (var w in watchers)
			{
				w.EnableRaisingEvents = false;
				w.Dispose();
			}
		}
		#endregion


		#region << Các sự kiện FileSystem thay đổi >>
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
				okState = false;
				if (MainWindow.instance?.IsActive == true) CheckReset();
			});
		}


		private void Watcher_Renamed(object sender, RenamedEventArgs e)
		{
			queue.Enqueue(WatcherChangeTypes.Renamed);
			App.Current.Dispatcher.Invoke(() =>
			{
				okState = false;
				if (MainWindow.instance?.IsActive == true) CheckReset();
			});
		}


		private void Watcher_Changed(object sender, FileSystemEventArgs e)
		{
			queue.Enqueue(WatcherChangeTypes.Changed);
			App.Current.Dispatcher.Invoke(() =>
			{
				okState = false;
				if (MainWindow.instance?.IsActive == true) CheckReset();
			});
		}


		private void Watcher_Deleted(object sender, FileSystemEventArgs e)
		{
			queue.Enqueue(WatcherChangeTypes.Deleted);
			App.Current.Dispatcher.Invoke(() =>
			{
				okState = false;
				if (MainWindow.instance?.IsActive == true) CheckReset();
			});
		}


		private void Watcher_Created(object sender, FileSystemEventArgs e)
		{
			queue.Enqueue(WatcherChangeTypes.Created);
			App.Current.Dispatcher.Invoke(() =>
			{
				okState = false;
				if (MainWindow.instance?.IsActive == true) CheckReset();
			});
		}
		#endregion


		public bool CheckReset()
		{
			if (token.IsCancellationRequested) return false;
			int count = queue.Count;
			if (count != 0)
			{
				for (int i = 0; i < count; ++i) queue.TryDequeue(out WatcherChangeTypes result);
				MainWindow.instance.Close();
				new PopupWaiting().Show();
				if (!Database.instance.Refresh(App.instance.path)) throw new Exception();
				okState = true;
				PopupWaiting.instance.Close();
				new MainWindow().Show();
				return true;
			}
			return false;
		}
	}
}