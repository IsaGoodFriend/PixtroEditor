using Pixtro.Emulation;
using System;
using System.Collections.Generic;
using System.Text;
using Monocle;
using Microsoft.Xna.Framework.Input;

namespace Pixtro.Editor {
	public class MonoGameController : IController {
		public ControllerDefinition Definition => null;

		public int AxisValue(string name) {
			return 0;
		}

		public IReadOnlyCollection<(string Name, int Strength)> GetHapticsSnapshot() {
			throw new NotImplementedException();
		}

		public bool IsPressed(string button) {
			
			switch (button) {
				default:
					return false;
				case "A":
					return MInput.Keyboard.Check(Keys.NumPad5);
				case "B":
					return MInput.Keyboard.Check(Keys.NumPad4);
				case "Left":
					return MInput.Keyboard.Check(Keys.A);
				case "Right":
					return MInput.Keyboard.Check(Keys.D);
				case "Up":
					return MInput.Keyboard.Check(Keys.W);
				case "Down":
					return MInput.Keyboard.Check(Keys.S);
				case "Start":
					return MInput.Keyboard.Check(Keys.NumPad8);
				case "Select":
					return MInput.Keyboard.Check(Keys.NumPad9);
				case "L":
					return MInput.Keyboard.Check(Keys.LeftShift);
				case "R":
					return MInput.Keyboard.Check(Keys.NumPad6);

			}
		}

		public void SetHapticChannelStrength(string name, int strength) {
			
		}
	}
}
