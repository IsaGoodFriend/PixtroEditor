using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Pixtro.Editor;
using Pixtro.Emulation;
using System.Collections.Generic;

namespace Pixtro.Scenes {
	public class EmulatorScene : Scene {

		static List<Effect> effects = new List<Effect>();

		static EmulatorScene() {
			effects.Add(Engine.Instance.Content.Load<Effect>("Shaders/main"));
			effects.Add(Engine.Instance.Content.Load<Effect>("Shaders/gba_on"));
		}

		Texture2D texture;

		RenderTarget2D bufferA, bufferB;

		public EmulatorScene() {
			texture = new Texture2D(Draw.SpriteBatch.GraphicsDevice, 240, 160);
			bufferA = new RenderTarget2D(Draw.SpriteBatch.GraphicsDevice, 240, 160);

			var image = new Image(new MTexture(texture));
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

		public override void Begin() {
			base.Begin();
			EmulationHandler.OnScreenRedraw += UpdateScreen;
		}
		public override void End() {
			base.End();
			EmulationHandler.OnScreenRedraw -= UpdateScreen;
		}

		void UpdateScreen() {

			texture.SetData(EmulationHandler.VideoBuffer());
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

			if (MInput.Keyboard.Pressed(Microsoft.Xna.Framework.Input.Keys.Tab)) {
				litScreen = !litScreen;
			}

		}

		bool litScreen = true;

		public override void DrawGraphics() {
			//base.DrawGraphics();

			effects[1].SetParameter("time", Engine.TimeAlive);
			effects[1].SetParameter("on", litScreen ? 1 : 0);

			SceneRenderer.EndGraphics();

			Draw.SpriteBatch.Begin(effect: effects[0]);
			Draw.SpriteBatch.GraphicsDevice.SetRenderTarget(bufferA);

			Draw.SpriteBatch.Draw(texture, new Rectangle(0, 0, 240, 160), Color.White);

			Draw.SpriteBatch.End();


			SceneRenderer.BeginGraphics(this); // effects[1]

			Draw.SpriteBatch.Draw(bufferA, Vector2.Zero, Color.White);
		}
	}
}
