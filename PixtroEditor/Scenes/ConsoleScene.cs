using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System.IO;

namespace Pixtro.Scenes {
	public class ConsoleScene : Scene {

		static ConsoleScene() {
			outputFile = new StreamWriter(File.Open("output.log", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read));
			outputFile.AutoFlush = true;
		}

		const int CONSOLE_LINES = 512;
		const int LINE_SPACE = 24;

		static string[] ConsoleLines = new string[CONSOLE_LINES];

		static int ConsoleIndex;

		static StreamWriter outputFile;

		public ConsoleScene() {
			Camera.Position = new Vector2(0, CONSOLE_LINES * LINE_SPACE);
		}

		public static void Log(string text) {
			ConsoleLines[ConsoleIndex++] = text;
			ConsoleIndex %= CONSOLE_LINES;
			outputFile.WriteLine(text);
		}

		public override void Update() {
			base.Update();
			Camera.Origin = new Vector2(0, VisualBounds.Y);
		}

		public override void DrawGraphics() {
			base.DrawGraphics();

			for (int i = CONSOLE_LINES - 1; i >= 0; i--) {
				int index = (i + ConsoleIndex) % CONSOLE_LINES;
				if (ConsoleLines[index] == null)
					break;

				Draw.Text(ConsoleLines[index], new Vector2(0, i * LINE_SPACE - (LINE_SPACE + 4)), Color.White);
			}
		}
	}
}
