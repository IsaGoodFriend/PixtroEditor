using System;
using System.Collections.Generic;
using System.Text;
using Monocle;
using Microsoft.Xna.Framework;

namespace Pixtro.UI {
	public class IconBarButton : BarButton {
		private Image image;

		public IconBarButton(Image image) : base() {

			this.image = image;
			Transform.Size.X = (int)image.Width;
		}

		protected internal override void Render() {
			base.Render();

			Draw.Depth++;
			image.Position = new Vector2(Transform.X, Transform.Y);
			image.drawDepth = Draw.Depth;
			image.Render();
		}

	}
	public class TextBarButton : BarButton {
		public string Text { get; private set; }

		public TextBarButton(string text) : base() {

			Text = text;

			Transform.Size.X = (int)Draw.MeasureText(text).X + 12;
		}

		protected internal override void Render() {
			base.Render();

			Draw.Depth++;
			Draw.TextCentered(Text, new Vector2(Position.X + (Transform.Size.X / 2), Position.Y + (Transform.Size.Y / 2)), Color.White);
		}
	}
	public abstract class BarButton : Control {

		public Func<Dropdown> OnClick;
		bool stayHighlighted;
		public bool Highlighted {
			get => UIFramework.HoveredControl == this || child != null || stayHighlighted;
			set { stayHighlighted = value; }
		}
		
		Dropdown child;

		public BarButton() {

			Transform.Size.Y = Editor.EditorWindow.SUB_MENU_BAR;

			OnClicked += Clicked;
		}

		private void Clicked(object sender, EventArgs e) {

			if (children.Count > 0) {
				UIFramework.RemoveControl(children[0]);
				return;
			}

			if (OnClick == null)
				return;

			child = OnClick();

			if (child == null)
				return;

			child.Position = new Point(0, Transform.Size.Y);

			AddChild(child);
		}

		protected internal override void Update() {
			base.Update();
			if (!UIFramework.HasControl(child)) {
				child = null;
			}
		}

		protected internal override void Render() {

			Color back = ColorSchemes.CurrentScheme.ButtonUnselected;

			if (Highlighted)
				back = ColorSchemes.CurrentScheme.ButtonHighlighted;
			
			base.Render();

			var rect = Transform.Bounds;
			rect.Height -= 2;
			Draw.Rect(rect, back);

			rect.X = rect.Right;
			rect.Width = 2;
			Draw.Rect(rect, ColorSchemes.CurrentScheme.Separation);

			rect.X = Transform.X - 2;
			Draw.Rect(rect, ColorSchemes.CurrentScheme.Separation);

		}
	}
}
