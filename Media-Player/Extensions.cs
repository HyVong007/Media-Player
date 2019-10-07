using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;


namespace MediaPlayer
{
	public static class Extensions
	{
		private static readonly string PATH = typeof(Extensions).ToString();
		private static readonly IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForDomain();


		static Extensions()
		{
			if (!storage.DirectoryExists(PATH)) storage.CreateDirectory(PATH);
			Application.Current.Exit += (object sender, ExitEventArgs e) => storage.Close();
		}


		/// <summary>
		/// <para>data == null will delete key.</para>
		/// </summary>
		public static void Write(this Application app, string key, object data, JsonSerializerSettings setting = null)
		{
			// Delete
			if (data == null)
			{
				if (app.Properties.Contains(key)) app.Properties.Remove(key);
				try
				{
					storage.DeleteFile($"{PATH}/{key}.txt");
				}
				catch (Exception) { }
				return;
			}

			// Write new or overwrite data
			app.Properties[key] = data;
			using (var stream = new IsolatedStorageFileStream($"{PATH}/{key}.txt", FileMode.Create, storage))
			using (var writer = new StreamWriter(stream))
			{
				writer.Write(JsonConvert.SerializeObject(data, setting));
				writer.Flush();
			}
		}


		public static T Read<T>(this Application app, string key)
		{
			if (app.Properties.Contains(key)) return (T)app.Properties[key];
			using (var stream = new IsolatedStorageFileStream($"{PATH}/{key}.txt", FileMode.Open, storage))
			using (var reader = new StreamReader(stream))
				return (T)(app.Properties[key] = JsonConvert.DeserializeObject<T>(reader.ReadToEnd()));
		}


		public static bool Contains<T>(this Application app, string key)
		{
			if (app.Properties.Contains(key)) return true;
			try { app.Read<T>(key); } catch (Exception) { return false; }
			return true;
		}
	}
}
