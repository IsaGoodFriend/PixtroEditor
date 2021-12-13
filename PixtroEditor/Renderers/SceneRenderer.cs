using System;
using System.Collections.Generic;
using System.Text;
using Monocle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pixtro.Editor {
	public class SceneRenderer : Renderer {
		public BlendState Blending;
		public SamplerState Sampling;

		public RenderTarget2D target { get; private set; }
		public SceneRenderer(Scene scene) {
			Blending = BlendState.AlphaBlend;
			Sampling = SamplerState.PointClamp;
		}

		public override void Render(Scene scene) {
			base.Render(scene);

			var viewport = Engine.Instance.GraphicsDevice.Viewport;

			viewport.Y += EditorWindow.SUB_MENU_BAR;
			viewport.Height -= EditorWindow.SUB_MENU_BAR;
			Engine.Instance.GraphicsDevice.Viewport = viewport;

			//Render everything on a moving camera
			Draw.SpriteBatch.Begin(SpriteSortMode.FrontToBack, Blending, Sampling, DepthStencilState.DepthRead, RasterizerState.CullNone, null, scene.Camera.Matrix);

			scene.Entities.Render();
			scene.DrawGraphics();

			Draw.SpriteBatch.End();


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
