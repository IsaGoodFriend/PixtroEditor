using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Monocle;

namespace Pixtro.UI {
	public static class UIFramework {
		private static List<Control> controls = new List<Control>();
		private static List<Control> toRemove = new List<Control>(), toAdd = new List<Control>();

		// The control that has the UI's focus, so to speak.  
		public static Control SelectedControl { get; private set; }
		public static Control HoveredControl { get; private set; }
		public static Control ClickedControl { get; private set; }

		public static void AddControl(Control control) {
			if (!controls.Contains(control) || !toAdd.Contains(control))
				controls.Add(control);

			foreach (var child in control.children)
				if (!controls.Contains(child) || !toAdd.Contains(child))
					controls.Add(child);
			
		}
		public static void RemoveControl(Control control) {
			if (!controls.Contains(control) || toRemove.Contains(control))
				return;
			
			if (control.Parent != null) {
				control.Parent.RemoveChild(control);
			}
			toRemove.Add(control);
			foreach (var child in control.children)
				toRemove.Add(child);
		}
		public static bool HasControl(Control control) => controls.Contains(control);

		public static void Update() {

			Point mouse = new Point((int)MInput.Mouse.X, (int)MInput.Mouse.Y);

			if (MInput.Mouse.ReleasedLeftButton) {
				if (ClickedControl != null && ClickedControl.Bounds.Contains(mouse))
					ClickedControl.Click();
				ClickedControl = null;
			}

			if (!MInput.Mouse.CheckLeftButton || MInput.Mouse.PressedLeftButton) {

				Control currentHover = null;

				foreach (var cnt in controls) {
					if (!cnt.Interactable)
						continue;

					if (cnt.Bounds.Contains(mouse)) {
						if (currentHover == null || cnt.Depth < currentHover.Depth)
							currentHover = cnt;
					}
				}

				SelectedControl = currentHover;
				if (MInput.Mouse.PressedLeftButton) {
					ClickedControl = currentHover;
				}
			}
			if (SelectedControl != null && SelectedControl.Bounds.Contains(mouse)) {
				HoveredControl = SelectedControl;

				if (!MInput.Mouse.CheckLeftButton) {
					HoveredControl.Hover();
				}
			}
			else {
				HoveredControl = null;
			}

			foreach (var cnt in controls) {
				cnt.Update();
			}
			if (ClickedControl != null) {
				ClickedControl.ClickHeldUpdate();
			}

			foreach (var cnt in toAdd)
				controls.Add(cnt);
			foreach (var cnt in toRemove)
				controls.Remove(cnt);
		}

		public static void Render() {
			foreach (var control in controls) {
				Draw.Depth = control.Depth;
				control.Render();
			}
		}
	}
}
