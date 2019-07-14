using System;
using System.Windows;
using System.IO;
using System.IO.IsolatedStorage;
using System.Diagnostics;


namespace MediaPlayer
{
	public partial class App : Application
	{
		internal const string PATH_KEY = "PATH", PATH_FILE = "PATH.TXT";



		static App()
		{
			Database.initializeCompleted += () =>
			{
				if (Database.instance.rootFolder == null) if (Current.Properties.Contains(PATH_KEY)) Current.Properties.Remove(PATH_KEY);
				new MainWindow().Show();
			};
		}


		public App()
		{
			InitializeComponent();
		}


		private void Application_Startup(object sender, StartupEventArgs e)
		{
			// Chỉ cho phép 1 process chạy.
			if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1) { Shutdown(); return; }

			// Load path
			var storage = IsolatedStorageFile.GetUserStoreForDomain();
			try
			{
				using (var stream = new IsolatedStorageFileStream(PATH_FILE, FileMode.Open, storage))
				using (var reader = new StreamReader(stream))
				{
					Properties[PATH_KEY] = reader.ReadLine();
					reader.Close();
				}
			}
			catch (Exception) { }
			finally { storage.Close(); }

			// Khởi tạo database
			new PopupWaiting().Show();
			PopupWaiting.instance.ContentRendered += (object _sender, EventArgs _e) => new Database(Properties.Contains(PATH_KEY) ? Properties[PATH_KEY] as string : "");
		}


		private void Application_Exit(object sender, ExitEventArgs e)
		{
			if (Properties.Contains(PATH_KEY))
			{
				var storage = IsolatedStorageFile.GetUserStoreForDomain();
				using (var stream = new IsolatedStorageFileStream(PATH_FILE, FileMode.Create, storage))
				using (var writer = new StreamWriter(stream))
				{
					writer.WriteLine(Properties[PATH_KEY]);
					writer.Flush();
					writer.Close();
				}
				storage.Close();
			}
		}
	}
}
