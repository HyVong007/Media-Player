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


		/// <summary>
		/// Should NOT use GUI Thread if long finding.
		/// </summary>
		/// <param name="keyword"></param>
		/// <param name="foundResult"></param>
		/// <returns></returns>
		public IReadOnlyList<string> Search(string keyword, Action<string, float> foundResult = null, CancellationToken token = default(CancellationToken))
		{
			foundResult?.Invoke(@"Nguyen Thanh Tam\Hehe.mp4", 100);
			return null;
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