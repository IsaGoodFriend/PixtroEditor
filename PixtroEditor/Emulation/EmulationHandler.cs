using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Pixtro.Emulation.GBA;
using Monocle;
using Pixtro.Editor;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Pixtro.Emulation {
	public static class EmulationHandler {

		private static NullController nullController = new NullController();
		private static MGBAHawk emulator;
		private static IController humanController;
		//private static int[] buffer;

		static Texture2D texture;
		static RenderTarget2D bufferA;

		static Effect colorFix;
		static Dictionary<string, Effect> effects;


		public static GameCommunicator Communication { get; private set; }

		public static bool GameRunning => emulator != null;

		const int size = 240 * 160;

		public static bool Focused = false, PlayUnfocused = false;

		public static bool SoftPause {
			get => ManualSoftPause || (!Focused && !PlayUnfocused);
		}
		public static bool ManualSoftPause;

		public static bool HardPause { get; set; }

		public static void InitializeGraphics() {
			texture = new Texture2D(Draw.SpriteBatch.GraphicsDevice, 240, 160);
			bufferA = new RenderTarget2D(Draw.SpriteBatch.GraphicsDevice, 240, 160, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

			colorFix = Engine.Instance.Content.Load<Effect>("Shaders/main");

			effects = new Dictionary<string, Effect>();
			effects.Add("gba_on", Engine.Instance.Content.Load<Effect>("Shaders/gba_on"));

			UpdateScreen(false);
		}

		public static void SetController(IController controller) {
			humanController = controller;
		}
		public static void LoadGame(byte[] data) {
			if (emulator != null)
				emulator.Dispose();

			emulator = new MGBAHawk(data);

			Communication = null;

			ApiManager.Restart(new BasicServiceProvider(emulator), null, emulator, new GameInfo() { });

			string path = Path.Combine(Projects.ProjectInfo.CurrentProject.ProjectDirectory, "build", "output.map");
			if (File.Exists(path)) {

				using (var fs = File.Open(path, FileMode.Open))
					Communication = new GameCommunicator(new StreamReader(fs));

				ServiceInjector.UpdateServices(emulator.ServiceProvider, Communication);

				Communication.RomLoaded();
			}
		}
		public static void LoadGame(string path) {
			LoadGame(File.ReadAllBytes(path));
		}
		public static void ClearGame() {
			if (emulator == null)
				return;
			emulator.Dispose();
			emulator = null;

			HardPause = false;
			ManualSoftPause = false;

			Communication = null;

			UpdateScreen(false);
		}

		public static void Update() {

			if (emulator == null) {

				return;
			}

			if (effects != null) {
				effects["gba_on"].SetParameter("time", Engine.TimeAlive);
				effects["gba_on"].SetParameter("on", 1);
			}

			if (!HardPause) {

				emulator.FrameAdvance(Focused ? humanController : nullController, true);

				UpdateScreen(true);
			}
		}

		static void UpdateScreen(bool value) {


			Draw.SpriteBatch.GraphicsDevice.SetRenderTarget(bufferA);

			if (value && emulator != null) {

				Draw.SpriteBatch.Begin(effect: colorFix);

				texture.SetData(emulator.GetVideoBuffer());

				Draw.SpriteBatch.Draw(texture, new Rectangle(0, 0, 240, 160), Color.White);
			}
			else {
				Draw.SpriteBatch.Begin();

				Draw.SpriteBatch.Draw(Atlases.EngineGraphics["UI/empty_game"].Texture,  new Rectangle(0, 0, 240, 160), Color.White);
			}

			Draw.SpriteBatch.End();

			Draw.SpriteBatch.GraphicsDevice.SetRenderTarget(null);
		}

		public static void Render() {

			Draw.SpriteBatch.Draw(bufferA, Vector2.Zero, Color.White);
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

	}
}
