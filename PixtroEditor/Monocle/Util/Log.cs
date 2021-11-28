using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Monocle {
	public static class LogFile {
		static LogFile() {
			DIRECTORY = Directory.GetCurrentDirectory() + "/log.txt";

			streamWriter = new StreamWriter(File.Create(DIRECTORY));
		}

		static StreamWriter streamWriter;
		static List<string> log = new List<string>();
		static readonly string DIRECTORY;

		public static void Log(string s) {
			streamWriter.WriteLine(s);
#if DEBUG
			Console.WriteLine(s);
#endif
		}
		public static void Log(object s) {
			Log(s.ToString());
		}
		public static void Log(string asdf, object s) {
			Log($"[{asdf}]: {s}");
		}
	}
}
