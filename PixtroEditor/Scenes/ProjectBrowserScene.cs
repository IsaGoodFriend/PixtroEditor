using System.IO;
using System.Text;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Monocle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Pixtro.Editor;
using Pixtro.Projects;
using Pixtro.UI;
using System.Linq;


namespace Pixtro.Scenes {
	public class ProjectBrowserScene : Scene {

		const int LINE_SPACE = 28;

		string folder;

		List<string> folders;
		List<string> files;

		int size = 0;

		int highlighted;

		Dictionary<string, Image> icons;

		float lastLeftClick;

		public ProjectBrowserScene(): base(new Image(Atlases.EngineGraphics["UI/scenes/folder_icon"])) {

			LoadFolder("");
			folder = "";

			icons = new Dictionary<string, Image>();

			icons.Add("folder", new Image(Atlases.EngineGraphics["UI/folder"]));

		}

		private void Clamp() {
			int bottom = (size * LINE_SPACE) - VisualBounds.Height;
			if (Camera.Y > bottom)
				Camera.Y = bottom;
			if (Camera.Y < 0)
				Camera.Y = 0;
		}

		public override void OnResize() {
			base.OnResize();

			if (Camera.Y > 0) {
				Camera.Y += (VisualBounds.Y - PreviousBounds.Y);
				Clamp();
			}
		}

		public void LoadFolder(string folder) {

			folders = new List<string>();
			files = new List<string>();

			Camera.Y = 0;

			if (folder != "") {
				folders.Add("..");
			}


			folder = Path.Combine(ProjectInfo.CurrentProject.ProjectDirectory, folder);

			foreach (var directory in Directory.EnumerateDirectories(folder)) {
				string name = Path.GetFileName(directory);
				if (name.StartsWith('.') || name == "build")
					continue;
				folders.Add(name);
			}
			foreach (var file in Directory.EnumerateFiles(folder)) {
				string ext = Path.GetExtension(file);
				string name = Path.GetFileName(file);

				if (ext == ".elf" || ext == ".gba" || ext == ".pxprj")
					continue;
				if (name.StartsWith('.'))
					continue;

				files.Add(name);
			}

			size = folders.Count + files.Count;
		}

		private void OpenFile(string file) {
			string fullpath = Path.Combine(ProjectInfo.CurrentProject.ProjectDirectory, file);
			string rootFolder = Path.GetDirectoryName(file);

			while (rootFolder.Contains('/') || rootFolder.Contains('\\')) {
				rootFolder = Path.GetDirectoryName(rootFolder);
			}

			switch (rootFolder) {
				case "levels":
					if (Path.GetExtension(file) == ".txt") {
						foreach (var window in Engine.Layout) {
							if (window.RootScene is LevelEditorScene) {
								var editor = window.RootScene as LevelEditorScene;
								editor.LoadLevel(file.Substring(7));
								break;
							}
						}
					}
					break;
			}
		}

		public override void Update() {
			base.Update();

			lastLeftClick += Engine.DeltaTime;

			if (UIFramework.HoveredControl == null && UIBounds.Transform.Bounds.Contains((int)MInput.Mouse.X, (int)MInput.Mouse.Y)) {
				int pos = (int)(MInput.Mouse.Y + Camera.Y - ( UIBounds.Transform.Y));

				Camera.Y -= LINE_SPACE * Math.Sign(MInput.Mouse.WheelDelta);

				highlighted = pos / LINE_SPACE;

				if (MInput.Mouse.PressedLeftButton) {

					if (lastLeftClick < 0.2f) {
						if (highlighted < folders.Count) {

							if (folders[highlighted] == "..") {
								folder = Path.GetDirectoryName(folder);
							}
							else {
								folder = Path.Combine(folder, folders[highlighted]);
							}
							LoadFolder(folder);
						}
						else if (highlighted < folders.Count + files.Count) {
							OpenFile(Path.Combine(folder, files[highlighted - folders.Count]));
						}
					}
					lastLeftClick = 0;
				}
			}
			else {
				highlighted = -1;
			}

			Clamp();
		}

		

		public override void DrawGraphics() {
			base.DrawGraphics();

			int yPos = 0;

			for (int i = 0; i < folders.Count; ++i) {

				int pos = (yPos / LINE_SPACE);


				if (highlighted == pos) {
					Draw.Rect(0, yPos, VisualBounds.Width, LINE_SPACE, ColorSchemes.CurrentScheme.ButtonHighlighted, -1);
				}
				else if ((yPos / LINE_SPACE) % 2 == 1) {
					Draw.Rect(0, yPos, VisualBounds.Width, LINE_SPACE, ColorSchemes.CurrentScheme.MenuBar, -1);
				}

				var img = icons["folder"];
				img.Position = new Vector2(0, yPos + 2);
				img.Render();
				Draw.Text(folders[i], new Vector2(28, yPos + 3), Color.White);
				yPos += LINE_SPACE;
			}
			for (int i = 0; i < files.Count; ++i) {

				int pos = (yPos / LINE_SPACE);


				if (highlighted == pos) {
					Draw.Rect(0, yPos, VisualBounds.Width, LINE_SPACE, ColorSchemes.CurrentScheme.ButtonHighlighted, -1);
				}
				else if ((yPos / LINE_SPACE) % 2 == 1) {
					Draw.Rect(0, yPos, VisualBounds.Width, LINE_SPACE, ColorSchemes.CurrentScheme.MenuBar, -1);
				}

				//var img = icons["folder"];
				//img.Position = new Vector2(0, yPos + 2);
				//img.Render();
				Draw.Text(files[i], new Vector2(28, yPos + 3), Color.White);
				yPos += LINE_SPACE;
			}
		}
	}
}
