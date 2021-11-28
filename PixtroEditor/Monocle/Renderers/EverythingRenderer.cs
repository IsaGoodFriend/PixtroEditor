using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Monocle {

	public class EverythingRenderer : Renderer {

		public BlendState Blending;
		public SamplerState Sampling;
		public Effect Effect;
		public Camera Camera;

		public EverythingRenderer() {

			Blending = BlendState.AlphaBlend;
			Sampling = SamplerState.PointClamp;

			Camera = new Camera();
			Camera.Origin = Vector2.Zero;
		}

		public override void Render(Scene scene) {

			Draw.UpdatePerFrame();

			//Render everything on a moving camera
			Draw.SpriteBatch.Begin(SpriteSortMode.FrontToBack, Blending, Sampling, DepthStencilState.DepthRead, RasterizerState.CullNone, null, Camera.Matrix * Engine.ScreenMatrix);

			scene.Entities.Render();

			Draw.SpriteBatch.End();

			// Render everything on a still camera
			Draw.SpriteBatch.Begin(SpriteSortMode.FrontToBack, Blending, Sampling, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

			scene.Entities.RenderUI();

			Draw.Depth = Draw.CLOSEST_DEPTH - 10;

			Draw.SpriteBatch.End();
		}

	}
}
