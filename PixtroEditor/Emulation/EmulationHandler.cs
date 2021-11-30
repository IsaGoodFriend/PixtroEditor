using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Pixtro.Emulation.GBA;
using Monocle;
using Pixtro.Editor;

namespace Pixtro.Emulation {
	public static class EmulationHandler {

		private static NullController nullController = new NullController();
		private static MGBAHawk emulator;
		private static IController humanController;
		private static int[] buffer;

		public static GameCommunicator Communication { get; private set; }

		const int size = 240 * 160;

		public static bool Focused = false, PlayUnfocused = false;

		private static bool softPause;

		public static bool SoftPause {
			get => softPause || (!Focused && !PlayUnfocused);
			set => softPause = value;
		}

		public static bool HardPause { get; set; }

		public static void SetController(IController controller) {
			humanController = controller;
		}
		public static void LoadGame(byte[] data) {
			if (emulator != null)
				emulator.Dispose();
			emulator = null;
			emulator = new MGBAHawk(data);

			ApiManager.Restart(new BasicServiceProvider(emulator), null, emulator, new GameInfo() { });

			using (var fs = File.Open(Path.Combine(Projects.ProjectInfo.CurrentProject.ProjectDirectory, "build", "output.map"), FileMode.Open))
				Communication = new GameCommunicator(new StreamReader(fs));

			ServiceInjector.UpdateServices(emulator.ServiceProvider, Communication);

			Communication.RomLoaded();
		}
		public static void LoadGame(string path) {
			LoadGame(File.ReadAllBytes(path));
		}
		public static void ClearGame() {
			emulator.Dispose();
			emulator = null;

			if (buffer == null || buffer.Length != size) {
				SetEmptyBuffer();
			}
		}

		public static void Update() {

			if (emulator == null) {

				if (buffer == null || buffer.Length != size) {
					SetEmptyBuffer();
				}

				return;
			}

			if (!HardPause) {

				emulator.FrameAdvance(Focused ? humanController : nullController, true);

				buffer = emulator.GetVideoBuffer();

				for (int i = 0; i < size; ++i) {
					int val = buffer[i] & 0xFF00FF;
					buffer[i] &= ~0xFF00FF;
					buffer[i] |= (val & 0xFF) << 16;
					buffer[i] |= (val & 0xFF0000) >> 16;
				}

				if (Communication.debug_engine_flags.GetFlag(GameToEditorFlags.PrintLevelData)) {

				}
			}
		}
		public static int[] VideoBuffer() {
			if (buffer == null || buffer.Length != size) {
				SetEmptyBuffer();
			}

			return buffer;
		}

		private static void SetEmptyBuffer() {
			buffer = new int[size];

			Atlases.EngineGraphics["empty_game"].Texture.GetData(buffer);
		}
	}
}
