using System;
using System.Collections.Generic;
using System.Text;
using Monocle;
using Microsoft.Xna.Framework;

namespace Pixtro.UI {
	public class Dropdown : Control {

		public const int BUFFER = 12;
		public const int HEIGHT_BUFFER = 2;
		public const int OPTION_SIZE = 24;
		public const int SEPARATE_SIZE = 6;

		(string text, Action<int> action)[] options;
		Rectangle[] boundRects;

		int? selectedOption;
		Rectangle highlightRect;

		public Dropdown(params (string str, Action<int> act)[] dropOptions) {
			List<(string, Action<int>)[]> tempList = new List<(string, Action<int>)[]>();
			List<(string, Action<int>)> tempMidList = new List<(string, Action<int>)>();
			List<(string, Action<int>)> totalList = new List<(string, Action<int>)>();

			float maxSize = 0;

			foreach (var op in dropOptions) {
				if (op.str == null || op.act == null) {
					tempList.Add(tempMidList.ToArray());
					tempMidList.Clear();
				}
				else {
					tempMidList.Add(op);
					totalList.Add(op);
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

			Transform.Bounds = new Rectangle(0, 0, (int)maxSize, lastRect.Bottom + HEIGHT_BUFFER);

			options = totalList.ToArray();

			OnHover += onHover;
			OnClicked += onClick;
		}

		public void CancelDropdown() {
			UIFramework.RemoveControl(this);
		}
		private void onClick(object sender, EventArgs e) {

			Point p = new Point((int)MInput.Mouse.Position.X - Position.X, (int)MInput.Mouse.Position.Y - Position.Y);
			if (highlightRect.Contains(p)) {
				options[selectedOption.Value].action(selectedOption.Value);
				CancelDropdown();
			}
		}

		private void onHover(object sender, EventArgs e) {

			var option =  FindOption();

			if (option == null) {
				highlightRect = Rectangle.Empty;
				selectedOption = null;
			}
			else {
				selectedOption = option.Value.Item3;

				highlightRect = boundRects[option.Value.Item1];
				highlightRect.Height = OPTION_SIZE;
				highlightRect.Y += OPTION_SIZE * option.Value.Item2;
			}
		}

		private (int, int, int)? FindOption() {
			Point p = new Point((int)MInput.Mouse.Position.X - Position.X, (int)MInput.Mouse.Position.Y - Position.Y);

			int value = 0;
			for (int i = 0; i < boundRects.Length; ++i) {
				if (boundRects[i].Contains(p)) {
					p.Y -= boundRects[i].Y;
					return (i, (p.Y / OPTION_SIZE), value + (p.Y / OPTION_SIZE));
				}
				else {
					value += boundRects[i].Height / OPTION_SIZE;
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

			if (!Transform.Bounds.Contains(p) && !Parent.Transform.Bounds.Contains(p))
				CancelDropdown();
		}

		protected internal override void Render() {
			base.Render();
			var rect = Transform.Bounds;
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

			int area = 0;
			for (int i = 0; i < options.Length;) {
				for (int j = 0; j < boundRects[area].Height / OPTION_SIZE; ++j) {

					Draw.Text(options[i].text, position - new Vector2(0, 2), Color.White);
					position.Y += OPTION_SIZE;

					i++;
				}
				area++;

				if (i >= options.Length - 1)
					break;

				Draw.Rect(position + new Vector2(4 - BUFFER, -2), Transform.Bounds.Width - 8, 2, ColorSchemes.CurrentScheme.Separation);
				position.Y += SEPARATE_SIZE;
			}
		}
	}
}
