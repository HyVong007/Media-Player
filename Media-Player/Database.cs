using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Speech.Recognition;
using System.IO.IsolatedStorage;
using System.Collections;

namespace MediaPlayer
{
	public sealed class Database
	{
		public sealed class Folder
		{
			public string path;
			public string[] files;
			public Folder[] children;
			public Folder parent;
		}

		public static Database instance { get; private set; }

		#region KHỞI TẠO DATABASE VÀ NHẬN DẠNG GIỌNG NÓI
		public Folder rootFolder;
		private int totalFiles;

		/// <summary>
		/// Khởi tạo database đã xong (Chưa khởi tạo nhận dạng giọng nói).
		/// Được gọi bằng thread khác main.
		/// </summary>
		public static event Action initializeCompleted;


		/// <summary>
		///  Nếu có dữ liệu: rootFolder !=null.
		/// </summary>
		public Database(string rootFolderPath, bool update)
		{
			instance = this;
			if (!update && Cache_To_Instance()) goto COMPLETED;
			if (rootFolderPath != "")
			{
				try { Path_To_Instance(rootFolderPath); }
				catch (Exception) { rootFolder = null; }
				if (rootFolder != null) Instance_To_Cache();
			}

		COMPLETED:
			initializeCompleted?.Invoke();
		}


		/// <exception cref="UnauthorizedAccessException"/>
		private void Path_To_Instance(string rootFolderPath)
		{
			var a = new List<Folder>() { (rootFolder = new Folder() { path = rootFolderPath }) };
			var b = new List<Folder>();

			var fileList = new List<string>();
			do
			{
				foreach (var folder in a)
				{
					fileList.Clear();
					foreach (string filePath in Directory.GetFiles(folder.path))
						if (IsMediaFile(filePath)) fileList.Add(filePath);

					fileList.Sort();
					folder.files = fileList.ToArray();
					totalFiles += folder.files.Length;
					string[] children = Directory.GetDirectories(folder.path);
					folder.children = new Folder[children.Length];
					for (int i = 0; i < children.Length; ++i) b.Add(folder.children[i] = new Folder() { path = children[i], parent = folder });
				}

				var t = a;
				a = b; b = t; b.Clear();
			} while (a.Count != 0);
		}


		private const string CACHE_FILE = "CACHE.TXT";

		/// <summary>
		/// Main thread write file.
		/// </summary>
		private void Instance_To_Cache()
		{
			var storage = IsolatedStorageFile.GetUserStoreForDomain();
			using (var stream = new IsolatedStorageFileStream(CACHE_FILE, FileMode.Create, storage))
			using (var writer = new StreamWriter(stream))
			{
				var a = new List<Folder>() { rootFolder };
				var b = new List<Folder>();
				do
				{
					foreach (var folder in a)
					{
						writer.WriteLine(folder.path);
						writer.WriteLine(folder.files.Length);
						writer.WriteLine(folder.children.Length);
						foreach (string file in folder.files) writer.WriteLine(file);
						b.AddRange(folder.children);
					}

					var t = a; a = b; b = t; b.Clear();
				} while (a.Count != 0);
				writer.Flush();
				writer.Close();
			}
			storage.Close();
		}


		/// <summary>
		/// Read file. Thread ???
		/// <para>Return true: thành công, False: thất bại.</para>
		/// </summary>
		private bool Cache_To_Instance()
		{
			var storage = IsolatedStorageFile.GetUserStoreForDomain();
			try
			{
				using (var stream = new IsolatedStorageFileStream(CACHE_FILE, FileMode.Open, storage))
				using (var reader = new StreamReader(stream))
				{
					Folder CreateFolder()
					{
						var folder = new Folder()
						{
							path = reader.ReadLine(),
							files = new string[Convert.ToInt32(reader.ReadLine())],
							children = new Folder[Convert.ToInt32(reader.ReadLine())]
						};
						for (int i = 0; i < folder.files.Length; ++i) folder.files[i] = reader.ReadLine();
						return folder;
					}

					var a = new List<Folder>() { (rootFolder = CreateFolder()) };
					var b = new List<Folder>();
					do
					{
						foreach (var folder in a)
						{
							for (int i = 0; i < folder.children.Length; (folder.children[i++] = CreateFolder()).parent = folder) ;
							b.AddRange(folder.children);
						}

						var t = a; a = b; b = t; b.Clear();
					} while (a.Count != 0);
					reader.Close();
				}
			}
			catch (Exception) { return false; }
			finally { storage.Close(); }
			return true;
		}


		/// <summary>
		/// Main Thread write file.
		/// Return: True is successful.
		/// </summary>
		public bool Refresh(string rootFolderPath)
		{
			var backup = rootFolder;
			try { Path_To_Instance(rootFolderPath); }
			catch (Exception) { rootFolder = backup; return false; }
			Instance_To_Cache();
			return true;
		}
		#endregion


		#region LỌC FILE LÀ VIDEO/ SOUND
		private static readonly List<string> MEDIA_EXTENSIONS = new List<string>()
		{
			".MP3", ".M4A", ".AAC", ".WAV", ".AMR", ".FLAC", ".MIDI", ".MID", ".MKA",
			".MP4", ".MPG", ".WMV", ".WEBM", ".FLV", ".AVI", ".3GP", ".WMA", ".MKV", ".MOV"
		};

		private static bool IsMediaFile(string filePath) => MEDIA_EXTENSIONS.Contains(Path.GetExtension(filePath).ToUpper());
		#endregion


		#region TÌM KIẾM BẰNG GÕ CHỮ
		private static readonly IReadOnlyDictionary<char, List<char>> VN_UNICODES = new Dictionary<char, List<char>>()
		{
			['A'] = new List<char>()
			{
				'Á', 'À', 'Ả', 'Ã', 'Ạ',
				'Ă', 'Ắ', 'Ằ', 'Ẳ', 'Ẵ', 'Ặ',
				'Â', 'Ấ', 'Ầ', 'Ẩ', 'Ẫ', 'Ậ'
			},
			['D'] = new List<char>()
			{
				'Đ'
			},
			['E'] = new List<char>()
			{
				'É', 'È', 'Ẻ', 'Ẽ', 'Ẹ',
				'Ê', 'Ế', 'Ề', 'Ể', 'Ễ', 'Ệ',
			},
			['O'] = new List<char>()
			{
				'Ó', 'Ò', 'Ỏ', 'Õ', 'Ọ',
				'Ơ', 'Ớ', 'Ờ', 'Ở', 'Ỡ', 'Ợ',
				'Ô', 'Ố', 'Ồ', 'Ổ', 'Ỗ', 'Ộ'
			},
			['U'] = new List<char>()
			{
				'Ú', 'Ù', 'Ủ', 'Ũ', 'Ụ',
				'Ư', 'Ứ', 'Ừ', 'Ử', 'Ữ', 'Ự'
			},
			['I'] = new List<char>()
			{
				'Í', 'Ì', 'Ỉ', 'Ĩ', 'Ị'
			},
			['Y'] = new List<char>()
			{
				'Ý', 'Ỳ', 'Ỷ', 'Ỹ', 'Ỵ'
			},
		};

		public static bool IsValidKeyword(string text)
		{
			text = text.ToUpper();
			foreach (char C in text)
			{
				if (C == ' ' || ('A' <= C && C <= 'Z') || ('0' <= C && C <= '9')) continue;
				bool valid = false;
				foreach (var list in VN_UNICODES.Values) if (list.Contains(C)) { valid = true; break; };
				if (valid) continue;
				return false;
			}

			return true;
		}


		public static string CreateKeyword(string text)
		{
			string keyword = "";
			foreach (char c in text) if (IsValidKeyword(c.ToString())) keyword += c;
			return keyword;
		}


		public IReadOnlyList<string> Search(string keyword, Action<string, float> foundResult = null, CancellationToken token = default)
		{
			var result = SearchByWord(keyword, foundResult, token);
			return (token.IsCancellationRequested || result.Count != 0) ? result : SearchByCharacter(keyword, foundResult, token);
		}


		private IReadOnlyList<string> SearchByWord(string keyword, Action<string, float> foundResult = null, CancellationToken token = default(CancellationToken))
		{
			var keyList = ToWords(keyword);
			var a = new List<Folder>() { rootFolder };
			var b = new List<Folder>();
			var result = new List<string>();
			int fileScanned = 0;
			do
			{
				foreach (var folder in a)
				{
					for (int i = 0; i < folder.files.Length; ++i)
					{
						if (token.IsCancellationRequested) return result;
						++fileScanned;
						string filePath = folder.files[i];
						string fileName = Path.GetFileNameWithoutExtension(filePath);

						if (SourceIsResult(ToWords(fileName), keyList))
						{
							result.Add(filePath);
							foundResult?.Invoke(filePath, (float)fileScanned / totalFiles);
						}
					}
					b.AddRange(folder.children);
				}

				var t = a; a = b; b = t; b.Clear();
			} while (a.Count != 0);
			return result;
		}


		private IReadOnlyList<string> SearchByCharacter(string keyword, Action<string, float> foundResult = null, CancellationToken token = default(CancellationToken))
		{
			void ConvertToCompact(ref string text)
			{
				string tmp = text.ToUpper();
				text = "";
				foreach (char C in tmp)
					if (('A' <= C && C <= 'Z') || ('0' <= C && C <= '9')) text += C;
					else foreach (var kvp in VN_UNICODES)
							if (kvp.Value.Contains(C))
							{
								text += kvp.Key;
								break;
							}
			}

			ConvertToCompact(ref keyword);
			var result = new List<string>();
			var a = new List<Folder>() { rootFolder };
			var b = new List<Folder>();
			int fileScanned = 0;
			do
			{
				foreach (var folder in a)
				{
					for (int i = 0; i < folder.files.Length; ++i)
					{
						if (token.IsCancellationRequested) return result;
						string filePath = folder.files[i];
						++fileScanned;
						string fileName = Path.GetFileNameWithoutExtension(filePath);
						ConvertToCompact(ref fileName);

						if (SourceIsResult(new List<char>(fileName), keyword))
						{
							result.Add(filePath);
							foundResult?.Invoke(filePath, (float)fileScanned / totalFiles);
						}
					}
					b.AddRange(folder.children);
				}

				var t = a; a = b; b = t; b.Clear();
			} while (a.Count != 0);
			return result;
		}


		private static bool SourceIsResult<T>(List<T> source, IEnumerable<T> key)
		{
			int lastIndex = -1;
			foreach (var k in key)
				while (true)
				{
					int index = source.IndexOf(k);
					if (index < 0) return false;
					source[index] = default;
					if (index > lastIndex)
					{
						lastIndex = index; break;
					}
				}
			return true;
		}


		/// <summary>
		/// Word = {A->Z} or word={0->9}, all characters must be continous.
		/// </summary>
		private static List<string> ToWords(string text)
		{
			// Return: true= (A->Z or {Â, Ê, Ô ...}), false= (0->9), null= (other).
			bool? CheckTypeAndModify(ref char C)
			{
				if ('A' <= C && C <= 'Z') return true;
				if ('0' <= C && C <= '9') return false;
				foreach (var kvp in VN_UNICODES)
					if (kvp.Value.Contains(C))
					{
						C = kvp.Key; return true;
					}
				return null;
			}

			text = text.ToUpper();
			var _result = new List<string>();
			string word = "";
			int index = 0;
			while (true)
			{
				word = "";
				char C = text[index];
				bool? type = CheckTypeAndModify(ref C);
				if (type != null) word += C;

				while (true)
				{
					if (++index == text.Length)
					{
						if (word != "") _result.Add(word);
						return _result;
					}

					C = text[index];
					if (CheckTypeAndModify(ref C) == type)
					{
						if (type != null) word += C;
					}
					else
					{
						if (word != "") _result.Add(word);
						break;
					}
				}
			}
		}
		#endregion


		#region CHUYỂN GIỌNG NÓI RA CHỮ
		private SpeechRecognitionEngine voiceListener;

		private void InitializeVoiceSearch()
		{
			voiceListener = new SpeechRecognitionEngine();
			voiceListener.SetInputToDefaultAudioDevice();
			voiceListener.SpeechRecognized += (object sender, SpeechRecognizedEventArgs e) => result = e.Result.Text;
			voiceListener.RecognizeCompleted += (object sender, RecognizeCompletedEventArgs e) => recognizeCompleted = true;
			//voiceListener.LoadGrammarCompleted += (object sender, LoadGrammarCompletedEventArgs e) => VoiceListenerInitialized?.Invoke();

			var choices = new Choices();
			var a = new List<Folder>() { rootFolder };
			var b = new List<Folder>();

			do
			{
				foreach (var folder in a)
				{
					foreach (string filePath in folder.files)
					{
						string name = "";
						foreach (var w in ToWords(Path.GetFileNameWithoutExtension(filePath))) name += $"{w} ";

						choices.Add(name);
						//choices.Add(CreateKeyword(Path.GetFileNameWithoutExtension(filePath)));

						//await Task.Delay(1);
					}

					foreach (var childFolder in folder.children) b.Add(childFolder);
				}

				var t = a; a = b; b = t; b.Clear();
			} while (a.Count != 0);

			/*choices = new Choices(new string[]
			{
				"tinh anh ban chieu", "nguoi dien yeu trang", "chuyen tau hoang hon", "pho dem",
				"ao cuoi mau hoa ca", "long me", "me yeu", "huyen thoai me", "nuoc mat ben them",
				"quan nua khuya", "nguoi di ngoai pho", "vo dong so bach thu ha"
			});*/




			var gb = new GrammarBuilder();
			gb.Append(choices);
			//gb.AppendDictation();
			gb.AppendWildcard();
			voiceListener.LoadGrammar(new Grammar(gb));
			voiceListenerInitialized?.Invoke();
		}

		/// <summary>
		/// Được gọi bằng thread khác main.
		/// </summary>
		public event Action voiceListenerInitialized;

		private string result;
		private bool recognizeCompleted;

		public async Task<string> Listen(CancellationToken token = default)
		{
			result = ""; recognizeCompleted = false;
			voiceListener.RecognizeAsync();
			while (!recognizeCompleted && !token.IsCancellationRequested) await Task.Delay(1);

			if (token.IsCancellationRequested)
			{
				result = "";
				if (!recognizeCompleted) voiceListener.RecognizeAsyncCancel();
			}
			return result;
		}
		#endregion


		public IEnumerator<Folder> GetFolders()
		{
			var a = new List<Folder>() { rootFolder };
			var b = new List<Folder>();
			do
			{
				foreach (var folder in a)
				{
					yield return folder;
					b.AddRange(folder.children);
				}

				var t = a; a = b; b = t; b.Clear();
			} while (a.Count != 0);
		}
	}
}