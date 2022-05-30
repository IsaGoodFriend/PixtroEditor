using System;
using System.Collections.Generic;
using System.Text;
using Monocle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pixtro.Editor;

namespace Pixtro.Editor {
	public class EditorRenderer : Renderer {
		public BlendState Blending;
		public SamplerState Sampling;

		public EditorRenderer() {
			Blending = BlendState.AlphaBlend;
			Sampling = SamplerState.PointClamp;

		}
		public override void Render(Scene scene) {

			//Render everything on a moving camera
			Draw.SpriteBatch.Begin(SpriteSortMode.FrontToBack, Blending, Sampling, DepthStencilState.DepthRead, RasterizerState.CullNone, null, Matrix.Identity);

			Draw.Depth = Draw.CLOSEST_DEPTH - 10;

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

			if (Emulation.EmulationHandler.Communication != null) {
				uint flags = (uint)Emulation.EmulationHandler.Communication.GetIntFromRam("debug_flags");

				Rectangle rect = new Rectangle(2, Engine.ViewHeight - 14, 24, 12);

				for (int i = 0; i < 32; i++) {
					Draw.Rect(rect, (flags & 0x80000000) > 0 ? Color.LightGreen : Color.Maroon);
					flags <<= 1;
					rect.X += rect.Width + 6;
					if ((i & 0x3) == 3)
						rect.X += 16;
				}
			}

			Draw.Depth = Draw.FARTHEST_DEPTH;

			Draw.Rect(0, 0, Engine.ViewWidth, EditorWindow.TOP_MENU_BAR, ColorSchemes.CurrentScheme.MenuBar);
			Draw.Rect(0, Engine.ViewHeight - EditorWindow.BOTTOM_MENU_BAR, Engine.ViewWidth, EditorWindow.BOTTOM_MENU_BAR, ColorSchemes.CurrentScheme.MenuBar);

			UI.UIFramework.Render();

			if (Projects.ProjectInfo.Building) {

				Draw.Depth = Draw.CLOSEST_DEPTH;
				Draw.Rect(0, 0, Engine.ViewWidth, Engine.ViewHeight, Color.Black * 0.3f);
			}

			Draw.SpriteBatch.End();
		}
	}
}
