using System;
using System.Collections.Generic;
using System.Text;
using Monocle;
using Microsoft.Xna.Framework;

namespace Pixtro.Windows {
	public class DialogBox : Game {

		private static void UnloadProperly(DialogBox box) {
			box.Dispose();
		}

		public DialogBox(params (string, Action)[] buttons) {

		}

		protected override void Update(GameTime gameTime) {
			base.Update(gameTime);

			if (IsActive) {
				if (MInput.Mouse.PressedLeftButton) {
					Exit();
				}
			}
		}

		protected override void Draw(GameTime gameTime) {
			base.Draw(gameTime);

			GraphicsDevice.Clear(ColorSchemes.CurrentScheme.Background);

		}

		protected override void OnExiting(object sender, EventArgs args) {
			base.OnExiting(sender, args);

			UnloadProperly(this);
		}
	}
}
