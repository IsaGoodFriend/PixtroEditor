using System;
using System.Collections.Generic;
using System.Text;
using Monocle;
using Microsoft.Xna.Framework;

namespace Pixtro.UI {
	public class SceneBounds : Control {
		private Scene scene;

		public SceneBounds(Scene scene) {
			this.scene = scene;
			Interactable = false;

			Transform.Bounds = scene.VisualBounds;
		}
	}
}
