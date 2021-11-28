using System;
using System.Collections.Generic;
using System.Text;
using Monocle;
using Microsoft.Xna.Framework;

namespace Pixtro.UI {
	public class Dropdown : Control {

		public const int BUFFER = 12;
		public const int HEIGHT_BUFFER = 6;
		public const int OPTION_SIZE = 24;
		public const int SEPARATE_SIZE = 6;

		(string text, Action action)[][] options;
		Rectangle[] boundRects;

		(int, int)? selectedOption;
		Rectangle highlightRect;

		public Dropdown(params (string str, Action act)[] dropOptions) {
			List<(string, Action)[]> tempList = new List<(string, Action)[]>();
			List<(string, Action)> tempMidList = new List<(string, Action)>();

			float maxSize = 0;

			foreach (var op in dropOptions) {
				if (op.str == null || op.act == null) {
					tempList.Add(tempMidList.ToArray());
					tempMidList.Clear();
				}
				else {
					tempMidList.Add(op);
					maxSize = Math.Max(maxSize, Draw.MeasureText(op.str).X);
				}
			}
			maxSize += BUFFER * 2;
			tempList.Add(tempMidList.ToArray());

			Rectangle lastRect = new Rectangle(0, -SEPARATE_SIZE, (int)maxSize, 0);

			boundRects = new Rectangle[tempList.Count];
			for (int i = 0; i < boundRects.Length; ++i) {
				lastRect.Y = lastRect.Bottom + SEPARATE_SIZE;
				lastRect.Height = tempList[i].Length * OPTION_SIZE;
				boundRects[i] = lastRect;
			}

			LocalBounds = new Rectangle(0, 0, (int)maxSize, lastRect.Bottom + HEIGHT_BUFFER);

			options = tempList.ToArray();

			OnHover += onHover;
			OnClicked += onClick;
		}

		private void onClick(object sender, EventArgs e) {

			Point p = new Point((int)MInput.Mouse.Position.X - Position.X, (int)MInput.Mouse.Position.Y - Position.Y);
			if (highlightRect.Contains(p)) {
				options[selectedOption.Value.Item1][selectedOption.Value.Item2].action();
				UIFramework.RemoveControl(this);
			}
		}

		private void onHover(object sender, EventArgs e) {

			selectedOption = FindOption();

			if (selectedOption == null) {
				highlightRect = Rectangle.Empty;
			}
			else {
				highlightRect = boundRects[selectedOption.Value.Item1];
				highlightRect.Height = OPTION_SIZE;
				highlightRect.Y += OPTION_SIZE * selectedOption.Value.Item2;
			}
		}

		private (int, int)? FindOption() {
			Point p = new Point((int)MInput.Mouse.Position.X - Position.X, (int)MInput.Mouse.Position.Y - Position.Y);

			for (int i = 0; i < boundRects.Length; ++i) {
				if (boundRects[i].Contains(p)) {
					p.Y -= boundRects[i].Y;
					return (i, p.Y / OPTION_SIZE);
				}
			}

			return null;
		}

		protected internal override void Update() {
			base.Update();

			if (UIFramework.ClickedControl == this || Parent == null)
				return;

			Point p = new Point((int)MInput.Mouse.Position.X, (int)MInput.Mouse.Position.Y);

			if (UIFramework.HoveredControl != this)
				highlightRect = Rectangle.Empty;

			if (!Bounds.Contains(p) && !Parent.Bounds.Contains(p))
				UIFramework.RemoveControl(this);
		}

		protected internal override void Render() {
			base.Render();
			var rect = Bounds;
			Draw.Rect(rect, ColorSchemes.CurrentScheme.Separation);
			rect.Inflate(-2, 0);
			rect.Height -= 2;
			Draw.Rect(rect, ColorSchemes.CurrentScheme.MenuBar, 1);

			rect = highlightRect;
			rect.Inflate(-2, 0);
			rect.X += Position.X;
			rect.Y += Position.Y;
			Draw.Rect(rect, ColorSchemes.CurrentScheme.ButtonHighlighted, 2);

			Draw.Depth += 3;

			Vector2 position = new Vector2(Position.X + BUFFER, Position.Y + 4);

			for (int i = 0; i < options.Length; ++i) {
				for (int j = 0; j < options[i].Length; ++j) {

					Draw.Text(options[i][j].text, position, Color.White);
					position.Y += OPTION_SIZE;
				}
				if (i == options.Length - 1)
					break;

				Draw.Rect(position + new Vector2(4 - BUFFER, -2), Bounds.Width - 8, 2, ColorSchemes.CurrentScheme.Separation);
				position.Y += SEPARATE_SIZE;
			}
		}
	}
}
