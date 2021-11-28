using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Pixtro.Editor {
	public struct ColorSchemes {
		public static ColorSchemes CurrentScheme { get; set; } = DarkTheme;

		public static ColorSchemes DarkTheme => new ColorSchemes() {
			Background = Calc.HexToColor("2c2c2c"),
			Separation = Calc.HexToColor("1e1e1e"),
			MenuBar = Calc.HexToColor("393939"),

			ButtonUnselected = Calc.HexToColor("2c2c2c"),
			ButtonHighlighted = Calc.HexToColor("e05da2"),

			CanvasBackground = Calc.HexToColor("3c3c3c"),
		};

		public Color Background, Separation, MenuBar;

		public Color ButtonUnselected, ButtonHighlighted;

		public Color CanvasBackground;
	}
}
