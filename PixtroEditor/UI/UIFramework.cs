using System;
using System.Collections.Generic;
using System.Text;

namespace Pixtro.UI {
	public static class UIFramework {
		private static List<Control> controls;

		public static void AddControl(Control control) {
			controls.Add(control);

		}

		public static void Update() {

		}
	}
}
