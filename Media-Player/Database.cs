using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;


namespace MediaPlayer
{
	public sealed class Database
	{
		public static Database instance { get; private set; }

		public Folder rootFolder;



		private int totalFiles;

		public Database(string rootFolderPath)
		{
			instance = this;
			var a = new List<Folder>() { (rootFolder = new Folder() { path = rootFolderPath }) };
			var b = new List<Folder>();

			do
			{
				foreach (var folder in a)
				{
					folder.files = Directory.GetFiles(folder.path);
					totalFiles += folder.files.Length;
					string[] children = Directory.GetDirectories(folder.path);
					folder.children = new Folder[children.Length];
					for (int i = 0; i < children.Length; ++i) b.Add(folder.children[i] = new Folder() { path = children[i], parent = folder });
				}

				var t = a;
				a = b; b = t; b.Clear();
			} while (a.Count != 0);
		}

		//  ==========================================================================


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
			// word = {A->Z} or word={0->9}, all characters must be continous.
			List<string> ToWords(string text)
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

			var keyList = ToWords(keyword);
			var a = new List<Folder>() { rootFolder };
			var b = new List<Folder>();
			var result = new List<string>();
			int fileScanned = 0;
			do
			{
				foreach (var folder in a)
				{
					foreach (string filePath in folder.files)
					{
						if (token.IsCancellationRequested) return result;
						++fileScanned;
						string fileName = Path.GetFileNameWithoutExtension(filePath);

						if (SourceIsResult(ToWords(fileName), keyList))
						{
							result.Add(filePath);
							foundResult?.Invoke(filePath, (float)fileScanned / totalFiles);
						}
					}
					foreach (var childFolder in folder.children) b.Add(childFolder);
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
					foreach (string filePath in folder.files)
					{
						if (token.IsCancellationRequested) return result;
						++fileScanned;
						string fileName = Path.GetFileNameWithoutExtension(filePath);
						ConvertToCompact(ref fileName);

						if (SourceIsResult(new List<char>(fileName), keyword))
						{
							result.Add(filePath);
							foundResult?.Invoke(filePath, (float)fileScanned / totalFiles);
						}
					}
					foreach (var childFolder in folder.children) b.Add(childFolder);
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
	}



	public sealed class Folder
	{
		public string path;
		public string[] files;
		public Folder[] children;
		public Folder parent;
	}
}