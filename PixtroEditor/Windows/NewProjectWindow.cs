using System;
using System.Collections.Generic;
using System.Text;
using Monocle;
using Microsoft.Xna.Framework;

namespace Pixtro.Editor {
	public class NewProjectWindow : Engine {
		public NewProjectWindow() : base(600, 400, 600, 400, "", false) {
			Window.AllowUserResizing = false;
		}
	}
}
