using System;
using System.Windows;
using System.IO;
using System.IO.IsolatedStorage;
using System.Diagnostics;


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



		static App()
		{
			Database.initializeCompleted += () =>
			{
				Watcher.okState = true;
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


		private void Application_Startup(object sender, StartupEventArgs e)
		{
			// Chỉ cho phép 1 process chạy.
			if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1) { Shutdown(); return; }

			// Khởi tạo database
			new PopupWaiting().Show();
			PopupWaiting.instance.ContentRendered += (object _sender, EventArgs _e) => new Database(path, !Watcher.okState);
		}


		private void Application_Exit(object sender, ExitEventArgs e)
		{
			Watcher.instance?.Dispose();
			if (!Watcher.okState)
			{
				Database.instance.Refresh(path);
				Watcher.okState = true;
			}
		}
	}
}
