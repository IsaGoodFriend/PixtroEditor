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

		public static bool GameRunning => emulator != null;

		const int size = 240 * 160;

		public static bool Focused = false, PlayUnfocused = false;

		private static bool softPause;

		public static event Action OnScreenRedraw;

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

				OnScreenRedraw();


			}
		}
		public static int[] VideoBuffer() {
			if (buffer == null || buffer.Length != size) {
				SetEmptyBuffer();
			}

			return buffer;
		}

		

		public static byte[] LevelDataInGame(int levelIndex, bool aSection) {
			var gc = GameCommunicator.Instance;

			var map = aSection ? gc.loaded_levels_a : gc.loaded_levels_b;
			int index = map.GetInt(levelIndex) & ~(aSection ? 0x2020000 : 0x2030000);

			int width = gc.LevelRegion.GetUshort(index);
			int height = gc.LevelRegion.GetUshort(index + 2);

			index += 4;

			return gc.LevelRegion.GetByteArray(index, width * height * 2);

			return null;
		}

		private static void SetEmptyBuffer() {
			buffer = new int[size];

			Atlases.EngineGraphics["empty_game"].Texture.GetData(buffer);
		}
	}
}
