using Pixtro.Emulation;
using System;
using System.Collections.Generic;
using System.Text;
using Monocle;
using Microsoft.Xna.Framework.Input;

namespace Pixtro.Editor {
	public class MonoGameController : IController {
		public ControllerDefinition Definition => null;

		VirtualButton A = new VirtualButton(new VirtualButton.KeyboardKey(Keys.NumPad5), new VirtualButton.PadButton(0, Buttons.A)),
			B= new VirtualButton(new VirtualButton.KeyboardKey(Keys.NumPad4), new VirtualButton.PadButton(0, Buttons.X)),
			L= new VirtualButton(new VirtualButton.KeyboardKey(Keys.LeftShift), new VirtualButton.PadButton(0, Buttons.LeftShoulder)),
			R= new VirtualButton(new VirtualButton.KeyboardKey(Keys.NumPad6), new VirtualButton.PadButton(0, Buttons.RightShoulder)),
			Up= new VirtualButton(new VirtualButton.KeyboardKey(Keys.W), new VirtualButton.PadButton(0, Buttons.DPadUp)),
			Down= new VirtualButton(new VirtualButton.KeyboardKey(Keys.S), new VirtualButton.PadButton(0, Buttons.DPadDown)),
			Right= new VirtualButton(new VirtualButton.KeyboardKey(Keys.D), new VirtualButton.PadButton(0, Buttons.DPadRight)),
			Left= new VirtualButton(new VirtualButton.KeyboardKey(Keys.A), new VirtualButton.PadButton(0, Buttons.DPadLeft)),
			Start= new VirtualButton(new VirtualButton.KeyboardKey(Keys.NumPad9), new VirtualButton.PadButton(0, Buttons.Start)),
			Select= new VirtualButton(new VirtualButton.KeyboardKey(Keys.NumPad8), new VirtualButton.PadButton(0, Buttons.Back));


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
					return A.Check;
				case "B":
					return B.Check;
				case "Left":
					return Left.Check;
				case "Right":
					return Right.Check;
				case "Up":
					return Up.Check;
				case "Down":
					return Down.Check;
				case "Start":
					return Start.Check;
				case "Select":
					return Select.Check;
				case "L":
					return L.Check;
				case "R":
					return R.Check;

			}
		}

		public void SetHapticChannelStrength(string name, int strength) {
			
		}
	}
}
