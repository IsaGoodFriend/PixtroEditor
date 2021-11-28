using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;

namespace Pixtro.UI {
	public enum Positioning {
		Center = 0,
		Top = 1,
		Bottom = 2,
		Left = 4,
		Right = 8,
	}
	public sealed class PositionAssist : Control {

		private Positioning anchor;
		public Positioning Anchor { get => anchor;
			set {

				anchor = value;

				int width, height;

				if (Parent == null) {
					width = Engine.ViewWidth;
					height = Engine.ViewHeight;
				}
				else {
					width = Parent.Bounds.Width;
					height = Parent.Bounds.Height;
				}

				Point newRoot = new Point(width / 2, height / 2);

				if ((anchor & Positioning.Left) != Positioning.Center) {
					newRoot.X = 0;
				}
				if ((anchor & Positioning.Right) != Positioning.Center) {
					newRoot.X = width;
				}
				if ((anchor & Positioning.Top) != Positioning.Center) {
					newRoot.Y = 0;
				}
				if ((anchor & Positioning.Bottom) != Positioning.Center) {
					newRoot.X = height;
				}

				AnchorOffset += (root - newRoot);
				root = newRoot;
			}
		}

		public Point AnchorOffset;
		private Point root;

		public PositionAssist(Positioning positioning, params Control[] children) : this(positioning, (IEnumerable<Control>)children) { }
		public PositionAssist(Positioning positioning, IEnumerable<Control> children) {
			foreach (var item in children) {
				AddChild(item);
			}
			Anchor = positioning;

			AnchorOffset = Point.Zero;

			LocalBounds = Rectangle.Empty;
			Interactable = false;
		}

		protected internal override void Update() {
			base.Update();

			root += AnchorOffset;

			LocalBounds.X = root.X;
			LocalBounds.Y = root.Y;
		}
	}
}
