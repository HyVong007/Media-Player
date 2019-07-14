using System;
using System.Windows;
using System.IO;
using System.IO.IsolatedStorage;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Collections;


namespace MediaPlayer
{
	public partial class App : Application
	{
		internal const string PATH_KEY = "PATH", PATH_FILE = "PATH.TXT";



		static App()
		{
			Database.initializeCompleted += () =>
			{
				if (Database.instance.rootFolder == null) Current.Properties[PATH_KEY] = "";
				new MainWindow().Show();
			};
		}


		public App()
		{
			InitializeComponent();
			originalProperties[PATH_KEY] = Properties[PATH_KEY] = "";
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
					originalProperties[PATH_KEY] = Properties[PATH_KEY] = reader.ReadLine();
					reader.Close();
				}
			}
			catch (Exception) { }
			finally { storage.Close(); }

			// Khởi tạo database
			new PopupWaiting().Show();
			PopupWaiting.instance.ContentRendered += (object _sender, EventArgs _e) => new Database(Properties[PATH_KEY] as string);
		}


		private readonly IDictionary originalProperties = new HybridDictionary();

		private void Application_Exit(object sender, ExitEventArgs e)
		{
			if (originalProperties[PATH_KEY] != Properties[PATH_KEY])
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
