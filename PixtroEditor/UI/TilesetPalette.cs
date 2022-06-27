using System;
using System.Collections.Generic;
using System.Text;
using Pixtro.Scenes;
using Pixtro.Compiler;
using Monocle;
using Microsoft.Xna.Framework;

namespace Pixtro.UI {
	public class TilesetPalette : Control {

		LevelEditorScene parentScene;
		Image[] images;

		int highlighted = -1;

		const int blockSize = 32;
		const int imageOffset = 4;
		const int buttonSize = blockSize + imageOffset + imageOffset;
		const int bufferSize = buttonSize + 4;
		

		public TilesetPalette(LevelEditorScene parent) {
			parentScene = parent;
			images = new Image[0];
			OnHover += onHover;
			OnClicked += onClick;
		}

		private void onClick(object sender, EventArgs e) {
			if (highlighted >= 0) {
				parentScene.BrushIndex = highlighted;
			}
		}

		private void onHover(object sender, EventArgs e) {

			Point p = new Point((int)MInput.Mouse.Position.X - Position.X, (int)MInput.Mouse.Position.Y - Position.Y);
			int index = p.Y / bufferSize;
			p.Y %= bufferSize;

			highlighted = -1;

			if (p.X < 2 || p.Y < 2 || p.X >= bufferSize - 2 || p.Y >= bufferSize - 2)
				return;

			highlighted = index;
		}

		protected internal override void Update() {
			base.Update();
			if (UIFramework.HoveredControl != this)
				highlighted = -1;
		}

		public void UpdateTo(LevelPack metadata) {
			images = new Image[metadata.CharIndex.Count - 1];
			Transform.Size = new Point(40, 40 * images.Length);

			for (int i = 0; i < metadata.CharIndex.Count - 1; i++) {
				char c = metadata.CharIndex[i + 1];

				if (metadata.VisualData.Wrapping[c].Preview != null) {
					var item = metadata.VisualData.Wrapping[c].Preview.Value;
					images[i] = new Image(metadata.Tilesets[c][item.X, item.Y]);
				}
				else {
					images[i] = new Image(metadata.GetTile(1, 1, 100, 100, (x, y) => (x <= 1) ? c : ' '));
				}
				
			}
		}

		protected internal override void Render() {
			base.Render();

			if (images.Length > 0) {

				Draw.Rect(Transform.X, Transform.Y, bufferSize - 2, bufferSize * images.Length - 2, ColorSchemes.CurrentScheme.Separation);

				Draw.Depth += 2;

				for (int i = 0; i < images.Length; i++) {
					Color c = ColorSchemes.CurrentScheme.ButtonUnselected;

					if (i == parentScene.BrushIndex)
						c = ColorSchemes.CurrentScheme.ButtonHighlighted;
					else if (i == highlighted)
						c = ColorSchemes.CurrentScheme.ButtonHighlighted;

					Draw.Rect(Transform.X, Transform.Y + (i * bufferSize), buttonSize, buttonSize, c);
					
					images[i].drawDepth = Draw.Depth + 1;
					images[i].Position = new Vector2(Transform.X + imageOffset, Transform.Y + imageOffset + (i * bufferSize));
					images[i].Scale = new Vector2(blockSize / 8, blockSize / 8);
					images[i].Render();
				}
			}
		}
	}
}
