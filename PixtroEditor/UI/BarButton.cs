using System;
using System.Collections.Generic;
using System.Text;
using Monocle;
using Microsoft.Xna.Framework;

namespace Pixtro.UI {
	public class BarButton : Control {

		public static string Text { get; private set; }

		public Func<Dropdown> CreateDropdown;

		public BarButton(string text) {

			Text = text;

			LocalBounds.Width = (int)Draw.MeasureText(text).X + 12;
			LocalBounds.Height = Editor.EditorWindow.SUB_MENU_BAR;

			OnClicked += Clicked;
		}

		private void Clicked(object sender, EventArgs e) {

			if (children.Count > 0) {
				UIFramework.RemoveControl(children[0]);
				return;
			}

			if (CreateDropdown == null)
				return;

			var dropdown = CreateDropdown();

			dropdown.Position = new Point(Position.X, Bounds.Bottom);

			AddChild(dropdown);
		}

		protected internal override void Render() {

			Color back = ColorSchemes.CurrentScheme.ButtonUnselected;

			if (UIFramework.HoveredControl == this)
				back = ColorSchemes.CurrentScheme.ButtonHighlighted;
			
			base.Render();
			Draw.Rect(Bounds, back);
			var rect = Bounds;
			rect.X = rect.Right;
			rect.Width = 2;
			Draw.Rect(rect, ColorSchemes.CurrentScheme.Separation);

			rect = Bounds;
			rect.X -= 2;
			rect.Width = 2;
			Draw.Rect(rect, ColorSchemes.CurrentScheme.Separation);

			Draw.Depth++;
			Draw.TextCentered(Text, new Vector2(Position.X + (LocalBounds.Width / 2), Position.Y + (LocalBounds.Height / 2)), Color.White);
		}
	}
}
