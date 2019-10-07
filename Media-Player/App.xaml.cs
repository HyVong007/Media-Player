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

		private const string PATH_KEY = "PATH";
		private string _path = "";
		public string path
		{
			get => _path.Length == 0 && Current.Contains<string>(PATH_KEY) ? _path = Current.Read<string>(PATH_KEY) : _path;

			set
			{
				if (_path == value) return;
				_path = value ?? throw new Exception();
				Current.Write(PATH_KEY, value);
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
