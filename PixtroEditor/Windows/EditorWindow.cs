using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using System;
using Pixtro.Emulation;
using Pixtro.UI;
using System.Reflection;
using System.Timers;
using Monocle;

namespace Pixtro.Editor {
	public class EditorWindow : Engine {

		public const int TOP_MENU_BAR = 25;
		public const int SUB_MENU_BAR = 25;

		public const int BOTTOM_MENU_BAR = 17;
		public const int HEIGHT_SUB = TOP_MENU_BAR + BOTTOM_MENU_BAR;

		BarButton[] buttons;
		Dropdown rightClickMenu;
		bool romDirty;

		public EditorWindow() : base(1280, 720, 1280, 720, "Pixtro", false) {
			IsMouseVisible = true;
			Window.AllowUserResizing = true;
		}

		protected override void Initialize() {
			base.Initialize();

			fullRenderer = new EditorRenderer();

			EmulationHandler.SetController(new MonoGameController());

		}

		protected override void LoadContent() {
			base.LoadContent();

			Atlases.EngineGraphics = Atlas.FromDirectory("Graphics");
			string path = Projects.ProjectInfo.CurrentProject.ProjectDirectory;
			Atlases.GameSprites = Atlas.FromDirectory(Path.Combine(path, "art"));

			FileUpdateManager.fileModified += Atlases.GameSprites.OnFileUpdated;
			FileUpdateManager.fileAdded += Atlases.GameSprites.OnFileAdded;
			FileUpdateManager.fileDeleted += Atlases.GameSprites.OnFileDeleted;

			FileUpdateManager.fileModified += OnFileChanges;
			FileUpdateManager.fileAdded += OnFileChanges;
			FileUpdateManager.fileDeleted += OnFileChanges;

			EmulationHandler.InitializeGraphics();

			Projects.ProjectInfo.CurrentProject.LoadContent();

			#region UI

			buttons = new BarButton[10];

			Control element;

			buttons[0] = (BarButton)UIFramework.AddControl(new TextBarButton("File") {
				OnClick = () => {
					return new Dropdown(
					("New Project", (i) => { }
					),
					("Open Project", (i) => { }
					),
					("Save Project", (i) => { }
					),
					("----", null),
					("Test Project", (i) => { }
					),
					("Exit", (i) => { Exit();  } )
					);
				}

			});

			element = UIFramework.AddControl(new IconBarButton(new Image(Atlases.EngineGraphics["UI/button_stop"])) {
				OnClick = () => {
					EmulationHandler.ClearGame();
					buttons[1].Highlighted = false;
					return null;
				}
			});
			buttons[4] = (BarButton)element;
			element.Transform.Anchor = new Vector2(1, 0);
			element.Transform.Center = new Vector2(1, 0);
			element.Transform.Offset.X = 0;


			int x = -(element.Transform.Size.X + 2);

			element = UIFramework.AddControl(new IconBarButton(new Image(Atlases.EngineGraphics["UI/button_softpause"])) {
				OnClick = () => {
					if (EmulationHandler.GameRunning) {
						EmulationHandler.ManualSoftPause = !EmulationHandler.ManualSoftPause;
						EmulationHandler.HardPause = false;
					}
					return null;
				}
			});
			buttons[3] = (BarButton)element;
			element.Transform.Anchor = new Vector2(1, 0);
			element.Transform.Center = new Vector2(1, 0);
			element.Transform.Offset.X = x;

			x -= element.Transform.Size.X + 2;

			element = UIFramework.AddControl(new IconBarButton(new Image(Atlases.EngineGraphics["UI/button_hardpause"])) {
				OnClick = () => {
					if (EmulationHandler.GameRunning) {
						EmulationHandler.HardPause = !EmulationHandler.HardPause;
						EmulationHandler.ManualSoftPause = false;
					}
					return null;
				}

			});
			buttons[2] = (BarButton)element;
			element.Transform.Anchor = new Vector2(1, 0);
			element.Transform.Center = new Vector2(1, 0);
			element.Transform.Offset.X = x;

			x -= element.Transform.Size.X + 2;

			element = UIFramework.AddControl(new IconBarButton(new Image(Atlases.EngineGraphics["UI/button_play"])) {
				OnClick = () => {
					if (!EmulationHandler.GameRunning) {
						Projects.ProjectInfo.RunBuild();
					}

					return null;
				}
			});
			buttons[1] = (BarButton)element;
			element.Transform.Anchor = new Vector2(1, 0);
			element.Transform.Center = new Vector2(1, 0);
			element.Transform.Offset.X = x;

#endregion
		}

		private void OnFileChanges(string directory, string fullPath) {
			bool dirty = false;

			string local = Path.GetRelativePath(Path.Combine(Projects.ProjectInfo.CurrentProject.ProjectDirectory, directory), fullPath),
				ext = Path.GetExtension(fullPath);

			switch (directory) {
				case "levels":
					if (local == "meta_level.json" || local.Contains('\\')) {
						dirty = true;
					}
					break;
				case "audio":
					dirty = true;
					break;
				case "dialogue":
				case "source":
					dirty = true;
					break;
				case "art":
					if (!local.Contains('\\') ||(ext != ".ase" && ext != ".aseprite" && ext != ".png" && ext != ".bmp"))
						break;
					string[] subfolder = local.Split('\\');

					switch (subfolder[0]) {
						case "backgrounds":
						case "fonts":
						case "particles":
						case "sprites":
						case "tilesets":
						case "titlecards":
						case "transitions":
							dirty = true;
							break;
					}
					break;
			}

			if (dirty) {
				romDirty = true;
			}
		}

		protected override void Update(GameTime gameTime) {

			base.Update(gameTime);

			EmulationHandler.Update();

			if (IsActive) {

				buttons[1].Highlighted = EmulationHandler.GameRunning;

				if (EmulationHandler.GameRunning) {

					buttons[2].Highlighted = EmulationHandler.HardPause;
					buttons[3].Highlighted = EmulationHandler.ManualSoftPause;
				}
				else {
					buttons[2].Highlighted = false;
					buttons[3].Highlighted = false;
				}

				if (romDirty) {
					Projects.ProjectInfo.BuildProject(false);
				}
			}


			//Point p = new Point((int)MInput.Mouse.X, (int)MInput.Mouse.Y);

			//if (rightClickMenu != null) {
			//	if (!rightClickMenu.Transform.Bounds.Contains(p)) {
			//		UIFramework.RemoveControl(rightClickMenu);
			//		rightClickMenu = null;
			//	}
			//}

			//if (MInput.Mouse.PressedRightButton) {
			//	var item = Layout.GetElementAt(p);

			//	if (UIFramework.HoveredControl != null) {

			//	}
			//	else if (item is EditorLayout.LayoutSplit) {

			//		rightClickMenu = new Dropdown(
			//			("Aa", (i) => { })
			//		);
			//	}
			//	else {

			//		rightClickMenu = new Dropdown(
			//			("Vertical Split Here", (i) => { Layout.SplitAt(p, EditorLayout.SplitDirection.Vertical); }),
			//			("Horizontal Split Here", (i) => { Layout.SplitAt(p, EditorLayout.SplitDirection.Vertical); } )
			//		);
			//	}

			//	if (rightClickMenu != null) {

			//		rightClickMenu.Position = p - new Point(2);
			//		rightClickMenu.Depth = 100;
			//		UIFramework.AddControl(rightClickMenu);
			//	}
			//}

			if (MInput.Keyboard.Check(Keys.LeftControl) && MInput.Keyboard.Pressed(Keys.B)) {
				if (MInput.Keyboard.Check(Keys.LeftShift)) {
					Projects.ProjectInfo.BuildRelease();
				}
				else {
					Projects.ProjectInfo.RunBuild();
				}
			}
		}
		protected override void RenderCore() {
			ClearColor = ColorSchemes.CurrentScheme.Background;

			base.RenderCore();
		}
	}
}
