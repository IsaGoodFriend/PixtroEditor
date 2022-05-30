using System;
using System.Collections.Generic;
using System.Text;
using Monocle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pixtro.Editor {
	public class SceneRenderer : Renderer {
		public static BlendState Blending = BlendState.AlphaBlend;
		public static SamplerState Sampling = SamplerState.PointClamp;

		public RenderTarget2D target { get; private set; }
		public SceneRenderer(Scene scene) {
		}

		public static void BeginGraphics(Scene scene, Effect eff = null) {

			var viewport = new Viewport(scene.VisualBounds);

			//viewport.Y += EditorWindow.SUB_MENU_BAR;
			//viewport.Height -= EditorWindow.SUB_MENU_BAR;
			Engine.Instance.GraphicsDevice.Viewport = viewport;

			Draw.SpriteBatch.GraphicsDevice.SetRenderTarget(null);
			Draw.SpriteBatch.Begin(SpriteSortMode.FrontToBack, Blending, Sampling, DepthStencilState.DepthRead, RasterizerState.CullNone, eff, scene.Camera.Matrix);

		}
		public static void EndGraphics() {

			Draw.SpriteBatch.End();
		}

		public override void Render(Scene scene) {
			base.Render(scene);

			BeginGraphics(scene);

			scene.DrawGraphics();

			EndGraphics();


			var viewport = new Viewport(scene.VisualBounds);
			viewport.Y -= EditorWindow.SUB_MENU_BAR;
			viewport.Height += EditorWindow.SUB_MENU_BAR;
			Engine.Instance.GraphicsDevice.Viewport = viewport;

			// Render everything on a still camera
			Draw.SpriteBatch.Begin(SpriteSortMode.FrontToBack, Blending, Sampling, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

			scene.Entities.RenderUI();

			Draw.Depth = Draw.FARTHEST_DEPTH + 10;
			Draw.Rect(0, 0, viewport.Width, EditorWindow.SUB_MENU_BAR, ColorSchemes.CurrentScheme.MenuBar, -1);
			Draw.Rect(0, EditorWindow.SUB_MENU_BAR - 2, viewport.Width, 2, ColorSchemes.CurrentScheme.Separation);

			Draw.SpriteBatch.End();
		}
	}
}
