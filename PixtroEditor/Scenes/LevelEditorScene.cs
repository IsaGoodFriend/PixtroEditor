﻿using System.IO;
using System.Text;
using System.Collections.Generic;
using System;
using Monocle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Pixtro.Editor;

namespace Pixtro.Scenes {
	public enum LevelEditorStates {
		None,
		Draw,
		Erase,
		Rectangle,
		Pan,
	}
	public class LevelContainer {
		public enum LevelSaveType {
			TextFile,
			Binary,
			Json,
			Tiled_XML,
		}
		public class TileLayer {
			public VirtualMap<char> tiles;

			public TileLayer(int width, int height) : this(width, height, ' ') {
			}
			public TileLayer(int width, int height, char empty) {
				tiles = new VirtualMap<char>(width, height, empty);
			}
			public TileLayer(char[,] data) {
				tiles = new VirtualMap<char>(data);
			}
		}
		public class EntityLayer {
			public class Entity {

				public string entType;

				public int X, Y;

				public List<string> metaData;
			}

			public List<Entity> entities;
		}

		private LevelSaveType saveType;
		private string savePath;

		public Dictionary<int, string> metaData;
		public List<TileLayer> tileMaps;
		public EntityLayer entities;

		public int Width { get; private set; }
		public int Height { get; private set; }

		//public LevelContainer(int level, bool aSection) {

		//}
		public LevelContainer(string path) {
			string ext = Path.GetExtension(path);

			metaData = new Dictionary<int, string>();
			tileMaps = new List<TileLayer>();
			entities = new EntityLayer();

			switch (ext) {
				case ".txt":
					saveType = LevelSaveType.TextFile;

					break;
			}
		}
		public LevelContainer(LevelSaveType saveType, int width, int height, int layerCount) {
			this.saveType = saveType;

			metaData = new Dictionary<int, string>();
			
			tileMaps = new List<TileLayer>();
			for (int i = 0; i < layerCount; ++i)
				tileMaps.Add(new TileLayer(width, height));

			entities = new EntityLayer();

			Width = width;
			Height = height;

		}

		private void Save(LevelSaveType saveType) {

			using (Stream s = File.Open(savePath, FileMode.Create)) {

				switch (saveType) {
					case LevelSaveType.TextFile: {

						StreamWriter sw = new StreamWriter(s);

						sw.WriteLine($"{Width} - {Height} - {tileMaps.Count}");

						sw.WriteLine("meta");
						foreach (var m in metaData) {
							sw.WriteLine($"{m.Key} ; {m.Value}");
						}
						sw.WriteLine("end");

						for (int i = 0; i < tileMaps.Count; ++i) {

							var map = tileMaps[i];
							sw.WriteLine($"layer - {i}");

							for (int y = 0; y < Height; ++y) {
								StringBuilder sb = new StringBuilder();

								for (int x = 0; x < Width; ++x) {
									sb.Append(map.tiles[x, y]);
								}

								sw.Write(sb.ToString());
							}

							sw.WriteLine("end");
						}

						sw.WriteLine("entities");

						foreach (var ent in entities.entities) {
							sw.Write($"{ent.entType};{ent.X};{ent.Y}");

							foreach (var m in ent.metaData) {
								sw.Write($";{m}");
							}
							sw.WriteLine();
						}

						sw.WriteLine("end");

						break;
					}
					
				}
			}
		}
		public void Save() {
			if (savePath == null)
				return;
			SaveAs(savePath);
		}
		public void SaveAs(string path) {
			savePath = path;

			Save(saveType);
		}
	}
	public class LevelEditorScene : Scene {

		private static readonly float[] ZOOM_LEVELS = {
			0.25f,
			0.5f,
			1f,
			2f,
			3f,
			4f,
			6f,
			8f,
		};
		//public static LevelContainer CurrentLevel { get; private set; }

		private const int ZOOM_START = 2;
		private const int TileSize = 8;
		private const int tempCountW = 30, tempCountH = 20;

		private LevelEditorStates baseState, heldState;

		private VirtualMap<int> rawTilemap;
		private TileGrid visualGrid;
		private Tileset testTileset;
		private int zoomIndex;

		public LevelContainer MainLevel { get; private set; }

		public LevelEditorScene() {
			Camera.Zoom = ZOOM_START;
			zoomIndex = Array.IndexOf(ZOOM_LEVELS, ZOOM_START);

			rawTilemap = new VirtualMap<int>(tempCountW, tempCountH, 0);

			HelperEntity.Add(visualGrid = new TileGrid(TileSize, TileSize, tempCountW, tempCountH));
			testTileset = new Tileset(Atlases.EngineGraphics["test tileset"], 8, 8);

			baseState = LevelEditorStates.Draw;

			OnMouseDown += MouseDown;
			OnMouseUp += MouseUp;
		}

		public void Extend(int left, int right, int up, int down) {
			Camera.Position -= new Vector2(left * TileSize, up * TileSize);

			int TilesX = rawTilemap.Columns,
				TilesY = rawTilemap.Rows;

			int newWidth = TilesX + left + right;
			int newHeight = TilesY + up + down;
			if (newWidth <= 0 || newHeight <= 0) {
				rawTilemap = new VirtualMap<int>(0, 0);
				return;
			}

			var newTiles = new VirtualMap<int>(newWidth, newHeight);

			//Center
			for (int x = 0; x < TilesX; x++) {
				for (int y = 0; y < TilesY; y++) {
					int atX = x + left;
					int atY = y + up;

					if (atX >= 0 && atX < newWidth && atY >= 0 && atY < newHeight)
						newTiles[atX, atY] = rawTilemap[x, y];
				}
			}

			//Left
			for (int x = 0; x < left; x++)
				for (int y = 0; y < newHeight; y++)
					newTiles[x, y] = rawTilemap[0, Calc.Clamp(y - up, 0, TilesY - 1)];

			//Right
			for (int x = newWidth - right; x < newWidth; x++)
				for (int y = 0; y < newHeight; y++)
					newTiles[x, y] = rawTilemap[TilesX - 1, Calc.Clamp(y - up, 0, TilesY - 1)];

			//Top
			for (int y = 0; y < up; y++)
				for (int x = 0; x < newWidth; x++)
					newTiles[x, y] = rawTilemap[Calc.Clamp(x - left, 0, TilesX - 1), 0];

			//Bottom
			for (int y = newHeight - down; y < newHeight; y++)
				for (int x = 0; x < newWidth; x++)
					newTiles[x, y] = rawTilemap[Calc.Clamp(x - left, 0, TilesX - 1), TilesY - 1];

			rawTilemap = newTiles;

			visualGrid.Extend(left, right, up, down);
			visualGrid.Position = Vector2.Zero;
		}

		private void MouseDown(int x, int y, bool newScene) {
			if (MInput.Keyboard.Check(Keys.Space)) {
				heldState = LevelEditorStates.Pan;
				OnMouseDrag += MouseDragged;
				return;
			}
			if (newScene)
				return;

			OnMouseDrag += MouseDragged;


			var state = baseState;
			if (heldState != LevelEditorStates.None) {
				state = heldState;
			}

			switch (state) {
				case LevelEditorStates.Draw:
					SetTile(MInput.Mouse.Position, 1);
					break;
				case LevelEditorStates.Erase:
					SetTile(MInput.Mouse.Position, 0);
					break;
			}
		}

		private void MouseUp(int x, int y, bool newScene) {
			heldState = LevelEditorStates.None;
			OnMouseDrag -= MouseDragged;
		}

		private void MouseDragged(int x, int y, bool newScene) {

			var state = baseState;
			if (heldState != LevelEditorStates.None) {
				state = heldState;
			}

			switch (state) {
				case LevelEditorStates.Pan:
					if (MInput.Mouse.WasMoved)
						Camera.Position -= MInput.Mouse.PositionDelta / Camera.Zoom;
					break;
				case LevelEditorStates.Draw:
					DrawLine(MInput.Mouse.Position, new Vector2(MInput.Mouse.PreviousState.X, MInput.Mouse.PreviousState.Y), 1);
					break;
				case LevelEditorStates.Erase:
					DrawLine(MInput.Mouse.Position, new Vector2(MInput.Mouse.PreviousState.X, MInput.Mouse.PreviousState.Y), 0);
					break;
			}

		}

		#region Set Tiles
		public void SetTile(int x, int y, int value) {
			if (x < 0 || y < 0 || x >= rawTilemap.Columns || y >= rawTilemap.Rows) {
				return;
			}
			rawTilemap[x, y] = value;
			if (value == 0)
				visualGrid.Tiles[x, y] = null;
			else
				visualGrid.Tiles[x, y] = testTileset[value - 1];
		}
		public void SetTile(Vector2 position, int value) {
			position.X -= VisualBounds.X;
			position.Y -= VisualBounds.Y;
			position /= Camera.Zoom;
			position += Camera.Position;

			SetTile((int)(position.X / TileSize), (int)(position.Y / TileSize), value);
		}

		public void DrawLine(Point start, Point end, int value) {
			int stride;
			if (Math.Abs(start.X - end.X) > Math.Abs(start.Y - end.Y))
				stride = Math.Abs(start.X - end.X);
			else
				stride = Math.Abs(start.Y - end.Y);

			for (int i = 0; i <= stride; ++i) {
				Vector2 lerped = Vector2.Lerp(new Vector2(start.X, start.Y), new Vector2(end.X, end.Y), (float) i / stride);
				SetTile((int)Math.Round(lerped.X), (int)Math.Round(lerped.Y), value);
			}
		}
		public void DrawLine(Vector2 start, Vector2 end, int value) {
			start.X -= VisualBounds.X;
			start.Y -= VisualBounds.Y;
			start /= Camera.Zoom;
			start += Camera.Position;

			end.X -= VisualBounds.X;
			end.Y -= VisualBounds.Y;
			end /= Camera.Zoom;
			end += Camera.Position;

			DrawLine(
				new Point((int)(start.X / TileSize), (int)(start.Y / TileSize)),
				new Point((int)(end.X / TileSize), (int)(end.Y / TileSize)), value);
		}

		#endregion

		public override void OnResize() {
			base.OnResize();


			Camera.Position -= new Vector2((VisualBounds.Width - PreviousBounds.Width) / 2f, (VisualBounds.Height - PreviousBounds.Height) / 2f) / Camera.Zoom;
		}

		public override void Begin() {
			base.Begin();
		}

		public override void FocusedUpdate() {
			base.FocusedUpdate();

			if (MInput.Keyboard.Pressed(Keys.B)) {
				baseState = LevelEditorStates.Draw;
			}
			if (MInput.Keyboard.Pressed(Keys.E)) {
				baseState = LevelEditorStates.Erase;
			}
		}

		public override void Update() {
			base.Update();
			if (VisualBounds.Contains(MInput.Mouse.X, MInput.Mouse.Y) && MInput.Mouse.WheelDelta != 0) {

				Camera.Position -= (new Vector2(VisualBounds.X, VisualBounds.Y) - MInput.Mouse.Position) / Camera.Zoom;

				zoomIndex = Calc.Clamp(zoomIndex + Math.Sign(MInput.Mouse.WheelDelta), 0, ZOOM_LEVELS.Length - 1);

				Camera.Zoom = ZOOM_LEVELS[zoomIndex];

				Camera.Position += (new Vector2(VisualBounds.X, VisualBounds.Y) - MInput.Mouse.Position) / Camera.Zoom;
				
			}
		}

		public override void DrawGraphics() {
			base.DrawGraphics();
			Draw.Depth = Draw.FARTHEST_DEPTH;
			Draw.Rect(0, 0, rawTilemap.Columns * TileSize, rawTilemap.Rows * TileSize, ColorSchemes.CurrentScheme.CanvasBackground);

			Draw.Depth++;
			for (int i = 1; i < rawTilemap.Columns; ++i)
				Draw.Rect(8 * i, 0, 1 / Camera.Zoom, rawTilemap.Rows * TileSize, Color.Black);
			for (int i = 1; i < rawTilemap.Rows; ++i)
				Draw.Rect(0, 8 * i, rawTilemap.Columns * TileSize, 1 / Camera.Zoom, Color.Black);
		}
	}
}
