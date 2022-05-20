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

		public EditorWindow() : base(1280, 720, 1280, 720, "Pixtro", false) {
			IsMouseVisible = true;
			Window.AllowUserResizing = true;
		}

		protected override void Initialize() {
			base.Initialize();

			fullRenderer = new EditorRenderer();

			EmulationHandler.SetController(new MonoGameController());
			var tempButton = new BarButton("File");
			tempButton.CreateDropdown = () => {
				return new Dropdown(
				("New Project", () => { }),
				("Open Project", () => { }),
				("Save Project", () => { }),
				("----", null),
				("Test Project", () => { }),
				("Exit", Exit)
				);
			};
			UIFramework.AddControl(tempButton);

		}

		protected override void LoadContent() {
			base.LoadContent();

			Atlases.EngineGraphics = Atlas.FromDirectory("Graphics");
			string path = Path.GetDirectoryName(Projects.ProjectInfo.CurrentProject.ProjectPath);
			Atlases.GameSprites = Atlas.FromDirectory(Projects.ProjectInfo.CurrentProject.ProjectPath);
		}

		protected override void Update(GameTime gameTime) {

			base.Update(gameTime);

			EmulationHandler.Update();

			if (MInput.Keyboard.Check(Keys.LeftControl) && MInput.Keyboard.Pressed(Keys.B)) {
				if (MInput.Keyboard.Check(Keys.LeftShift)) {
					Projects.ProjectInfo.BuildRelease();
				}
				else {
					Projects.ProjectInfo.BuildAndRun();
				}
			}
		}
		protected override void RenderCore() {
			ClearColor = ColorSchemes.CurrentScheme.Background;

			base.RenderCore();
		}
	}
}
