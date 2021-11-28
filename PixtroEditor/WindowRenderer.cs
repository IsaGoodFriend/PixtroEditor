using System;
using System.Collections.Generic;
using System.Text;
using Monocle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pixtro.Editor;

namespace Pixtro.Editor {
	public class WindowRenderer : Renderer {
		public BlendState Blending;
		public SamplerState Sampling;

		public WindowRenderer() {
			Blending = BlendState.AlphaBlend;
			Sampling = SamplerState.PointClamp;

		}
		public override void Render(Scene scene) {

			//Render everything on a moving camera
			Draw.SpriteBatch.Begin(SpriteSortMode.FrontToBack, Blending, Sampling, DepthStencilState.DepthRead, RasterizerState.CullNone, null, Matrix.Identity);

			foreach (var split in Engine.GetWindowSplits()) {
				Rectangle rect = new Rectangle(
					split.Item1.BoundingRect.X, split.Item1.BoundingRect.Y,
					split.Item1.BoundingRect.Width, split.Item1.BoundingRect.Height);

				if (split.Direction == EditorLayout.SplitDirection.Horizontal) {
					rect.X = rect.Right;
					rect.Width = EditorLayout.LayoutSplit.SPLIT_PIXEL_SIZE;
				}
				else {
					rect.Y = rect.Bottom;
					rect.Height = EditorLayout.LayoutSplit.SPLIT_PIXEL_SIZE;
				}

				Draw.Rect(rect, ColorSchemes.CurrentScheme.Separation);
			}

			Draw.Rect(0, EditorWindow.TOP_MENU_BAR - 2, Engine.ViewWidth, 2, ColorSchemes.CurrentScheme.Separation);
			Draw.Rect(0, Engine.ViewHeight - EditorWindow.BOTTOM_MENU_BAR, Engine.ViewWidth, 2, ColorSchemes.CurrentScheme.Separation);

			Draw.SpriteBatch.End();
		}
	}
}
