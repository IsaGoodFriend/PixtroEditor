using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Pixtro.Scenes {
	public class ConsoleScene : Scene {

		static ConsoleScene() {
			outputFile = new StreamWriter(File.Open("output.log", FileMode.Create, FileAccess.Write, FileShare.Read));
			outputFile.AutoFlush = true;
		}

		const int CONSOLE_LINES = 512;
		const int LINE_SPACE = 28;

		static string[] ConsoleLines = new string[CONSOLE_LINES];
		static int ConsoleIndex;
		static StreamWriter outputFile;

		float scrollOffset;

		public ConsoleScene() {
			Camera.Position = new Vector2(0, CONSOLE_LINES * LINE_SPACE);
		}

		public override void OnResize() {
			base.OnResize();

			Camera.Origin = new Vector2(0, VisualBounds.Height);
		}

		public static void Log(string text) {
			ConsoleLines[ConsoleIndex++] = text;
			ConsoleIndex %= CONSOLE_LINES;
			outputFile.WriteLine(text);
		}
		internal static void CompilerWarning(string file, int line, string warning) {
			var match = Regex.Match(warning, @"([\s\S]+) \[[\w-]+\]$");
			string displayWarning = warning;

			if (match.Success)
				displayWarning = match.Groups[1].Value;

			Log($"WARNING :: {file} {line} -- {displayWarning}");
		}
		internal static void CompilerError(string file, int line, string warning) {
			var match = Regex.Match(warning, @"([\s\S]+) \[[\w-]+\]$");
			string displayWarning = warning;

			if (match.Success)
				displayWarning = match.Groups[1].Value;

			Log($"ERROR :: {file} {line} -- {displayWarning}");
		}

		public override void Update() {
			base.Update();

			if (VisualBounds.Contains(MInput.Mouse.X, MInput.Mouse.Y)) {
				scrollOffset += MInput.Mouse.WheelDelta / 2;
			}

			if (scrollOffset < 0)
				scrollOffset = 0;

			Camera.Position = new Vector2(0, CONSOLE_LINES * LINE_SPACE - (float)Math.Floor(scrollOffset / LINE_SPACE) * LINE_SPACE);
		}

		public override void DrawGraphics() {
			base.DrawGraphics();

			for (int i = CONSOLE_LINES - 1; i >= 0; i--) {
				int index = (i + ConsoleIndex) % CONSOLE_LINES;
				if (ConsoleLines[index] == null)
					break;

				Draw.Text(ConsoleLines[index], new Vector2(0, i * LINE_SPACE - 8), Color.White);
			}
		}
	}
}
