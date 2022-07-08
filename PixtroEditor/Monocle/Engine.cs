//#define CONST
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Reflection;
using System.Runtime;
using Pixtro.Projects;
using Pixtro.UI;
using Pixtro.Scenes;
using Pixtro.Editor;
using Microsoft.Xna.Framework.Input;

namespace Monocle {
	public class Engine : Game {
		public string Title;
		public Version Version;

		// references
		public static Engine Instance { get; private set; }
		public static GraphicsDeviceManager Graphics { get; private set; }
		public static Pooler Pooler { get; private set; }
		public static Action OverloadGameLoop;
		public static StateMachine GameState { get; internal set; }

		// screen size
		public static int Width { get; private set; }
		public static int Height { get; private set; }
		public static int ViewWidth { get; private set; }
		public static int ViewHeight { get; private set; }
		public static EditorLayout Layout { get; private set; }
		public static int ViewPadding {
			get { return viewPadding; }
			set {
				viewPadding = value;
				Instance.UpdateView();
			}
		}
		private static int viewPadding = 0;
		private static bool resizing;

		public static event Action<int, int> ResizeEnd;

		// time
		public static float DeltaTime { get; private set; }
		public static float TimeAlive { get; private set; }
		public static float TimeRate = 1f;
		public static float FreezeTimer;
		public static int FPS;
		private TimeSpan counterElapsed = TimeSpan.Zero;
		private int fpsCounter = 0;

		// content directory
#if !CONSOLE
		private static string AssemblyDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
#endif

		public static string ContentDirectory {
#if PS4
			get { return Path.Combine("/app0/", Instance.Content.RootDirectory); }
#elif NSWITCH
			get { return Path.Combine("rom:/", Instance.Content.RootDirectory); }
#elif XBOXONE
			get { return Instance.Content.RootDirectory; }
#else
			get { return Path.Combine(AssemblyDirectory, Instance.Content.RootDirectory); }
#endif
		}

		// scenes and layout
		private Scene activeScene;
		protected Renderer fullRenderer;
		private bool sceneChanged = false;
		private Rectangle previousBounds;

		// util
		public static Color ClearColor;

		public static event Action<int, int> OnMouseDown, OnMouseDrag, OnMouseUp;

		public static float UpdateFrameData;

		public Engine(int width, int height, int windowWidth, int windowHeight, string windowTitle, bool fullscreen) {
			Instance = this;

			Title = Window.Title = windowTitle;
			Width = width;
			Height = height;
			ClearColor = Color.Black;

			Graphics = new GraphicsDeviceManager(this);
			Graphics.DeviceReset += OnGraphicsReset;
			Graphics.DeviceCreated += OnGraphicsCreate;
			Graphics.SynchronizeWithVerticalRetrace = true;
			Graphics.PreferMultiSampling = false;
			Graphics.GraphicsProfile = GraphicsProfile.Reach;
			Graphics.PreferredBackBufferFormat = SurfaceFormat.Color;
			Graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;

#if PS4 || XBOXONE
			Graphics.PreferredBackBufferWidth = 1920;
			Graphics.PreferredBackBufferHeight = 1080;
#elif NSWITCH
			Graphics.PreferredBackBufferWidth = 1280;
			Graphics.PreferredBackBufferHeight = 720;
#else
			Window.AllowUserResizing = true;
			Window.ClientSizeChanged += OnClientSizeChanged;

			if (fullscreen) {
				Graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
				Graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
				Graphics.IsFullScreen = true;
			}
			else {
				Graphics.PreferredBackBufferWidth = windowWidth;
				Graphics.PreferredBackBufferHeight = windowHeight;
				Graphics.IsFullScreen = false;
			}
#endif

			Content.RootDirectory = @"Content";

			IsMouseVisible = false;
			IsFixedTimeStep = false;

			GCSettings.LatencyMode = GCLatencyMode.LowLatency;
		}

#if !CONSOLE
		protected virtual void OnClientSizeChanged(object sender, EventArgs e) {
			if (Window.ClientBounds.Width > 0 && Window.ClientBounds.Height > 0 && !resizing) {
				resizing = true;

				Graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
				Graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
				UpdateView();

				resizing = false;

				if (Layout != null)
					foreach (var window in Layout) {
						window.RootScene.OnResize();
						window.RootScene.PreviousBounds = window.RootScene.VisualBounds;
					}
			}
		}
#endif

		protected virtual void OnGraphicsReset(object sender, EventArgs e) {
			UpdateView();

			if (Layout != null)
				foreach (var window in Layout)
					window.RootScene.HandleGraphicsReset();
		}

		protected virtual void OnGraphicsCreate(object sender, EventArgs e) {
			UpdateView();

			if (Layout != null)
				foreach (var window in Layout)
					window.RootScene.HandleGraphicsCreate();
		}

		protected override void OnActivated(object sender, EventArgs args) {
			base.OnActivated(sender, args);

			if (activeScene != null)
				activeScene.GainFocus();
		}

		protected override void OnDeactivated(object sender, EventArgs args) {
			base.OnDeactivated(sender, args);

			if (activeScene != null)
				activeScene.LoseFocus();
		}

		protected override void Initialize() {
			base.Initialize();

			MInput.Initialize();
			Tracker.Initialize();
			Pooler = new Pooler();

			Graphics.PreferredBackBufferWidth = 1280;
			Graphics.PreferredBackBufferHeight = 720;
			Graphics.ApplyChanges();

			UpdateView();

			Layout = new EditorLayout(LayoutJson.ParseData(File.ReadAllText("layouts/default.json")));
		}


		protected override void LoadContent() {
			base.LoadContent();

			Monocle.Draw.Initialize(GraphicsDevice);

			previousBounds = Window.ClientBounds;
		}

		protected override void Update(GameTime gameTime) {

			if (activeScene == null) {
				SetActiveScene(new Vector2(0, EditorWindow.TOP_MENU_BAR + 1));
			}

			

			Scene previousScene = activeScene;

			DeltaTime = 1 / 60.0f;
			TimeAlive += DeltaTime;

			//Update input
			MInput.Update();
			// Update UI
			UIFramework.Update();

			if (OverloadGameLoop != null) {
				OverloadGameLoop();
				base.Update(gameTime);
				return;
			}

			// Get the current layout element under the mouse
			var item = Layout.GetElementAt(new Point((int)MInput.Mouse.X, (int)MInput.Mouse.Y));

			if (MInput.Mouse.PressedLeftButton) {
				SetActiveScene(MInput.Mouse.Position);
				if (activeScene != previousScene)
					sceneChanged = true;
			}
			if (MInput.Mouse.ReleasedLeftButton) {
				sceneChanged = false;
			}

			if (UIFramework.SelectedControl == null) {

				if (OnMouseDown != null && MInput.Mouse.PressedLeftButton)
					OnMouseDown((int)MInput.Mouse.X, (int)MInput.Mouse.Y);
				if (OnMouseDrag != null && MInput.Mouse.CheckLeftButton && !MInput.Mouse.PressedLeftButton && MInput.Mouse.WasMoved)
					OnMouseDrag((int)MInput.Mouse.X, (int)MInput.Mouse.Y);
				if (OnMouseUp != null && MInput.Mouse.ReleasedLeftButton)
					OnMouseUp((int)MInput.Mouse.X, (int)MInput.Mouse.Y);

				if (!EditorLayout.Resizing)
					activeScene.UpdateMouse(sceneChanged);
			}

			foreach (var window in Layout)
				window.RootScene.Update();

			if (activeScene != null) {
				activeScene.BeforeUpdate();
				activeScene.FocusedUpdate();
				activeScene.AfterUpdate();
			}
				
			if (GameState != null)
				GameState.Update();

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime) {
			RenderCore();

			base.Draw(gameTime);

			//Frame counter
			fpsCounter++;
			counterElapsed += gameTime.ElapsedGameTime;
			if (counterElapsed >= TimeSpan.FromSeconds(1)) {
#if DEBUG
				Window.Title = $"{Title} {(ProjectInfo.CurrentProject != null ? ProjectInfo.CurrentProject.Name : "")} {fpsCounter}fps - { GC.GetTotalMemory(false) / 1048576f:F}MB";
#else
				Window.Title = $"{Title} {(ProjectInfo.CurrentProject != null ? ProjectInfo.CurrentProject.Name : "")}";
#endif
				FPS = fpsCounter;
				fpsCounter = 0;
				counterElapsed -= TimeSpan.FromSeconds(1);
			}
		}

		/// <summary>
		/// Override if you want to change the core rendering functionality of Monocle Engine.
		/// By default, this simply sets the render target to null, clears the screen, and renders the current Scene
		/// </summary>
		protected virtual void RenderCore() {
			Monocle.Draw.UpdatePerFrame();

			GraphicsDevice.Clear(ClearColor);
			GraphicsDevice.SetRenderTarget(null);

			foreach (var window in Layout)
				window.RootScene.BeforeRender();
			fullRenderer.BeforeRender(null);

			foreach (var window in Layout) {

				window.RootScene.PrepRendering();
				window.RootScene.Render();
				window.RootScene.AfterRender();
			}

			GraphicsDevice.Viewport = Viewport;

			fullRenderer.Render(null);
			fullRenderer.AfterRender(null);

		}

		protected override void OnExiting(object sender, EventArgs args) {
			base.OnExiting(sender, args);
			MInput.Shutdown();
		}

		public void RunWithLogging() {
			try {
				Run();
			}
			catch (Exception e) {
				ErrorLog.Write(e);
				ErrorLog.Open();
			}
		}

		#region Scene

		/// <summary>
		/// The currently active Scene. Note that if set, the Scene will not actually change until the end of the Update
		/// </summary>
		public static Scene CurrentScene {
			get { return Instance.activeScene; }
		}

		public static Scene GetScene(int sceneLocation) {
			var item = Layout.GetWindow(sceneLocation);

			return item.RootScene;
		}

		public static void ChangeScene(int sceneLocation, Scene newScene) {
			var item = Layout.GetWindow(sceneLocation);

			item.ChangeRootScene(newScene);
		}

		public static void SetActiveScene(Vector2 point) {
			var item = Layout.GetElementAt(new Point((int)point.X, (int)point.Y));

			if (item == null || item is EditorLayout.LayoutSplit)
				return;
			var window = item as EditorLayout.LayoutWindow;

			//Changing scenes
			if (window.RootScene != Instance.activeScene) {

				if (Instance.activeScene != null) {
					Instance.activeScene.LoseFocus();
				}
				Instance.activeScene = window.RootScene;

				if (Instance.activeScene != null) {
					Instance.activeScene.GainFocus();

					Instance.activeScene.RendererList.UpdateLists();
				}
			}
		}

		public static IEnumerable<EditorLayout.LayoutSplit> GetWindowSplits() {
			if (Layout.layout is EditorLayout.LayoutWindow) {
				yield break;
			}

			yield return Layout.layout as EditorLayout.LayoutSplit;
			foreach (var item in GetSplits(Layout.layout as EditorLayout.LayoutSplit))
				yield return item;
		}
		private static IEnumerable<EditorLayout.LayoutSplit> GetSplits(EditorLayout.LayoutSplit split) {
			if (split.Item1 is EditorLayout.LayoutSplit) {
				yield return split.Item1 as EditorLayout.LayoutSplit;

				foreach (var item in GetSplits(split.Item1 as EditorLayout.LayoutSplit))
					yield return item;
			}
			if (split.Item2 is EditorLayout.LayoutSplit) {
				yield return split.Item2 as EditorLayout.LayoutSplit;

				foreach (var item in GetSplits(split.Item2 as EditorLayout.LayoutSplit))
					yield return item;
			}
		}

		#endregion

		#region Screen

		public static Viewport Viewport { get; private set; }
		public static Matrix ScreenMatrix, MouseMatrix;
		public static float Scaling { get; private set; }
		public static float InvertScaling { get; private set; }

		public static void SetWindowed(int width, int height) {
#if !CONSOLE
			if (width > 0 && height > 0) {
				resizing = true;
				Graphics.PreferredBackBufferWidth = width;
				Graphics.PreferredBackBufferHeight = height;
				Graphics.IsFullScreen = false;
				Graphics.ApplyChanges();
				resizing = false;
			}
#endif
		}

		public static void SetFullscreen() {
#if !CONSOLE
			resizing = true;
			Graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
			Graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
			Graphics.IsFullScreen = true;
			Graphics.ApplyChanges();
			resizing = false;
#endif
		}

		private void UpdateView() {

			float screenWidth = Graphics.PreferredBackBufferWidth;
			float screenHeight = Graphics.PreferredBackBufferHeight;

			ViewWidth = (int)screenWidth;
			ViewHeight = (int)screenHeight;

			Scaling = 1;
			InvertScaling = 1;

			// update screen matrix
			ScreenMatrix = Matrix.CreateScale(ViewWidth / (float)Width, ViewWidth / (float)Width, 1);
			MouseMatrix = Matrix.CreateTranslation(((int)screenWidth - ViewWidth) >> 1, ((int)screenHeight - ViewHeight) >> 1, 0);

			// update viewport
			Viewport = new Viewport {
				X = 0,
				Y = 0,
				Width = ViewWidth,
				Height = ViewHeight,
				MinDepth = 0,
				MaxDepth = 1
			};

			if (ResizeEnd != null)
				ResizeEnd(ViewWidth, ViewHeight);

			//Debug Log
			//Calc.Log("Update View - " + screenWidth + "x" + screenHeight + " - " + viewport.Width + "x" + viewport.GuiHeight + " - " + viewport.X + "," + viewport.Y);
		}

		#endregion
	}
}
