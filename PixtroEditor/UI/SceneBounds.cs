using System;
using System.Collections.Generic;
using System.Text;
using Monocle;

namespace Pixtro.UI {
	public class SceneBounds : Control {
		private Scene scene;

		public SceneBounds(Scene scene) {
			this.scene = scene;
			Interactable = false;
		}

		protected internal override void Update() {
			base.Update();

			LocalBounds = scene.VisualBounds;
		}
	}
}
