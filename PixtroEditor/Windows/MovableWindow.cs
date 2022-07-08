using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using System;
using Pixtro.Emulation;
using Pixtro.UI;
using System.Reflection;
using System.Timers;
using Monocle;


namespace Pixtro.Windows {
	internal class MovableWindow : Game {

		public MovableWindow() {
			Window.IsBorderless = true;
		}
	}
}
