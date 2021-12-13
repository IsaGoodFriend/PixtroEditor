using System;
using System.IO;

namespace Pixtro.Editor {
	public enum WindowState {
		Quit,
		Editor,
		ProjectSelection,
		NewProject,
	}
	public static class Program {
		public static WindowState CurrentState;

		[STAThread]
		static void Main(string[] args) {

			if (args.Length > 0 && File.Exists(args[0])) {
				Projects.ProjectInfo.OpenProject(args[0]);
				CurrentState = WindowState.Editor;
			}
			else {
				// TODO: Create new project and project select windows to use here
				CurrentState = WindowState.ProjectSelection;
			}

			while (CurrentState != WindowState.Quit) {
				var state = CurrentState;
				CurrentState = WindowState.Quit;

				switch (state) {
					case WindowState.Editor:

#if DEBUG
						using (var game = new EditorWindow()) {
							game.Run();
						}
#else
						try {
							using (var game = new EditorWindow()) {
								game.Run();
							}
						}
						catch (Exception e) {

						}
#endif
						break;

					default:
					case WindowState.NewProject:

						using (var game = new NewProjectWindow()) {
							game.Run();
						}

						break;
				}
			}
		}
	}
}
