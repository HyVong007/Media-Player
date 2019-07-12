using System;
using System.Windows;
using System.IO;
using System.IO.IsolatedStorage;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;


namespace MediaPlayer
{
	public partial class App : Application
	{
		public const string PATH_KEY = "PATH", PATH_FILE = "PATH.TXT";


		public App()
		{
			InitializeComponent();
		}


		private void Application_Startup(object sender, StartupEventArgs e)
		{
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
			catch (FileNotFoundException) { }
			finally { storage.Close(); }
		}


		private void Application_Exit(object sender, ExitEventArgs e)
		{
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
