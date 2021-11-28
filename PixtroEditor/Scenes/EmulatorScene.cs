using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Pixtro.Editor;
using Pixtro.Emulation;

namespace Pixtro.Scenes {
	public class EmulatorScene : Scene {

		Texture2D texture;

		public EmulatorScene() {
			texture = new Texture2D(Draw.SpriteBatch.GraphicsDevice, 240, 160);

			var image = new Image(new MTexture(texture));
			HelperEntity.Add(image);
		}

		public override void OnSetWindow(EditorLayout.LayoutWindow window) {
			base.OnSetWindow(window);
			window.MinimumWidth = 240;
			window.MinimumHeight = 160;
		}

		public override void LoseFocus() {
			base.LoseFocus();
			EmulationHandler.Focused = false;
		}
		public override void GainFocus() {
			base.GainFocus();
			EmulationHandler.Focused = true;
		}

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


			texture.SetData(EmulationHandler.VideoBuffer());
		}
	}
}
