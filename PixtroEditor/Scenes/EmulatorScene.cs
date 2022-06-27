using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Pixtro.Editor;
using Pixtro.Emulation;
using System.Collections.Generic;

namespace Pixtro.Scenes {
	public class EmulatorScene : Scene {



		public EmulatorScene() : base(new Image(Atlases.EngineGraphics["UI/scenes/emulator_icon"])) {

		}

		public override void OnSetWindow(EditorLayout.LayoutWindow window) {
			base.OnSetWindow(window);
			window.MinimumWidth = 240;
			window.MinimumHeight = 160 + EditorWindow.SUB_MENU_BAR;
		}

		public override void LoseFocus() {
			base.LoseFocus();
			EmulationHandler.Focused = false;
		}
		public override void GainFocus() {
			base.GainFocus();
			EmulationHandler.Focused = true;
		}

		//public override void Begin() {
		//	base.Begin();
		//	EmulationHandler.OnScreenRedraw += UpdateScreen;
		//}
		//public override void End() {
		//	base.End();
		//	EmulationHandler.OnScreenRedraw -= UpdateScreen;
		//}


		public override void Update() {
			base.Update();

			float screenWidth = 240;
			float screenHeight = 160;

			float ViewWidth, ViewHeight;

			// get View Size
			if (screenWidth / VisualBounds.Width > screenHeight / VisualBounds.Height) {
				ViewWidth = (int)screenWidth;
				ViewHeight = (int)(screenWidth / VisualBounds.Width * VisualBounds.Height);
			}
			else {
				ViewWidth = (int)(screenHeight / VisualBounds.Height * VisualBounds.Width);
				ViewHeight = (int)screenHeight;
			}

			Camera.Zoom = VisualBounds.Width / ViewWidth;

			Camera.Position = new Vector2((screenWidth - ViewWidth) / 2, (screenHeight - ViewHeight) / 2);

			if (MInput.Keyboard.Pressed(Microsoft.Xna.Framework.Input.Keys.Tab)) {
				litScreen = !litScreen;
			}

		}

		bool litScreen = true;

		public override void DrawGraphics() {
			base.DrawGraphics();

			EmulationHandler.Render();

			//Draw.SpriteBatch.Draw(bufferA, Vector2.Zero, Color.White);
		}
	}
}
