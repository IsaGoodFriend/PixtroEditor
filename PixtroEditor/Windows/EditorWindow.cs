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
			string path = Path.GetDirectoryName(Projects.ProjectInfo.CurrentProject.ProjectPath);
			Atlases.GameSprites = Atlas.FromDirectory(Projects.ProjectInfo.CurrentProject.ProjectPath);

			EmulationHandler.InitializeGraphics();

			Projects.ProjectInfo.CurrentProject.LoadContent();

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
						Projects.ProjectInfo.BuildAndRun();
					}
					buttons[1].Highlighted = true;

					return null;
				}
			});
			buttons[1] = (BarButton)element;
			element.Transform.Anchor = new Vector2(1, 0);
			element.Transform.Center = new Vector2(1, 0);
			element.Transform.Offset.X = x;

		}

		protected override void Update(GameTime gameTime) {

			base.Update(gameTime);

			EmulationHandler.Update();

			if (EmulationHandler.GameRunning) {
				buttons[2].Highlighted = EmulationHandler.HardPause;
				buttons[3].Highlighted = EmulationHandler.ManualSoftPause;
			}
			else {
				buttons[2].Highlighted = false;
				buttons[3].Highlighted = false;
			}

			//if (!EmulationHandler.GameRunning) {
			//	string test = Directory.GetCurrentDirectory() + "/test.gba";
			//	EmulationHandler.LoadGame(test);
			//}
			if (!EmulationHandler.GameRunning && !Projects.ProjectInfo.Building) {
				//Projects.ProjectInfo.BuildAndRun();
			}

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
