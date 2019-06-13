using System;
using System.Windows;
using System.IO;
using System.IO.IsolatedStorage;
using System.Diagnostics;
using System.Reflection;


namespace MediaPlayer
{
	public partial class App : Application
	{
		public const string ROOT_FOLDER_KEY = "path", APP_TXT_KEY = "MediaPlayer.txt";


		public App()
		{
			InitializeComponent();
		}


		private void Application_Startup(object sender, StartupEventArgs e)
		{
			var storage = IsolatedStorageFile.GetUserStoreForDomain();
			string path = "";
			try
			{
				using (var stream = new IsolatedStorageFileStream(APP_TXT_KEY, FileMode.Open, storage))
				using (var reader = new StreamReader(stream))
				{
					while (!reader.EndOfStream) path = reader.ReadLine();
					reader.Close();
				}
			}
			catch (FileNotFoundException) { path = ""; }
			Properties[ROOT_FOLDER_KEY] = path;
		}


		private void Application_Exit(object sender, ExitEventArgs e)
		{
			var storage = IsolatedStorageFile.GetUserStoreForDomain();
			using (var stream = new IsolatedStorageFileStream(APP_TXT_KEY, FileMode.Create, storage))
			using (var writer = new StreamWriter(stream))
			{
				writer.WriteLine(Properties[ROOT_FOLDER_KEY]);
				writer.Flush();
				writer.Close();
			}

			if (restart) Process.Start(Assembly.GetEntryAssembly().Location);
		}


		private static bool restart;

		public static void Restart()
		{
			restart = true;
			Current.Shutdown();
		}
	}
}
