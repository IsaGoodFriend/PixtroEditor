using System;
using System.IO;

namespace Pixtro.Editor {
	public static class Program {
		[STAThread]
		static void Main(string[] args) {

			if (args.Length > 0 && File.Exists(args[0])) {
				Projects.ProjectInfo.OpenProject(args[0]);
			}

			using (var game = new EditorWindow())
				game.Run();
		}
	}
}
