using System.IO;
using System.Text;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Monocle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Pixtro.Editor;
using Pixtro.Compiler;
using Pixtro.UI;
using System.Linq;

namespace Pixtro.Scenes {
	public enum LevelEditorStates {
		None,
		Switch,
		Eyedrop,
		Draw,
		Rectangle,
		Pan,
		Resize,
		MoveEntity,
		Cancel,
	}
	public class LevelPack {
		public readonly Point[] updateOffsets;
		public readonly Dictionary<string, Tileset> StringToTileset;
		public readonly Dictionary<char, Tileset> Tilesets;
		public readonly List<char> CharIndex;
		public readonly VisualPackMetadata VisualData;
		public readonly Dictionary<char, MTexture> Previews;

		public LevelPack(VisualPackMetadata pack) {
			List<Point> offsets = new List<Point>();
			StringToTileset = new Dictionary<string, Tileset>();
			Previews = new Dictionary<char, MTexture>();
			Tilesets = new Dictionary<char, Tileset>();
			Tilesets.Add(' ', null);
			CharIndex = new List<char>();
			CharIndex.Add(' ');

			VisualData = pack;

			foreach (var item in pack.Wrapping) {
				var wrap = item.Value;

				if (wrap.Tileset != null && wrap.Mapping != null) {
					foreach (var p in wrap.Mapping)
						if (!offsets.Contains(new Point(p.X, p.Y)))
							offsets.Add(new Point(p.X, p.Y));

					if (!StringToTileset.ContainsKey(wrap.Tileset)) {
						StringToTileset.Add(wrap.Tileset, new Tileset(Atlases.GameSprites[$"tilesets/{wrap.Tileset}"], 8, 8));
					}

					Tilesets.Add(item.Key, StringToTileset[wrap.Tileset]);
				}
				else {
					Tilesets.Add(item.Key, null);
				}

				MTexture image;
				if (wrap.PreviewSprite != null) {
					image = Atlases.GameSprites[$"{wrap.PreviewSprite}"];
				}
				else if (wrap.Preview != null) {
					var point = wrap.Preview.Value;
					image = Tilesets[item.Key][point.X, point.Y];
				}
				else 
					image = GetTile(1, 1, 100, 100, (x, y) => (x <= 1) ? item.Key : ' ');

				Previews.Add(item.Key, image);
				CharIndex.Add(item.Key);
			}

			updateOffsets = offsets.ToArray();
			
			
		}

		public MTexture GetTile(int x, int y, VirtualMap<char> tiles) {

			var value = tiles[x, y];
			if (value == ' ')
				return null;

			var tileset = Tilesets[value];
			if (tileset == null)
				return Previews[value];

			var points = VisualData.Wrapping[value].GetWrapping((x, y) => tiles[x, y], x, y, tiles.Columns, tiles.Rows);

			if (points == null)
				return null;

			var point = points[CompiledLevel.RandomFromPoint(x, y, 0, points.Length)];

			return tileset[point.X, point.Y];
		}
		private MTexture GetTile(int x, int y, int width, int height, Func<int, int, char> getTile) {

			var value = getTile(x, y);
			var tileset = Tilesets[value];
			if (tileset == null)
				return null;

			var points = VisualData.Wrapping[value].GetWrapping(getTile, x, y, width, height);

			if (points == null)
				return null;

			var point = points[0];

			return tileset[point.X, point.Y];
		}
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
				tiles = new VirtualMap<char>(data, ' ');
			}
		}
		public class Entity {

			public Rectangle bounds => new Rectangle((X + Offset.X) * LevelEditorScene.TileSize, (Y + Offset.Y) * LevelEditorScene.TileSize, Width * LevelEditorScene.TileSize, Height * LevelEditorScene.TileSize);

			public string entType;

			public int X, Y, Width, Height;
			public Point RenderOffset, Offset;

			public List<string> metaData;

			public Image sprite;

			public Entity(string[] data) {
				metaData = new List<string>();
				entType = data[0];
				X = int.Parse(data[1]);
				Y = int.Parse(data[2]);
				Width = 1;
				Height = 1;
				for (int i = 3; i < data.Length; ++i) {
					metaData.Add(data[i]);
				}
			}
			public void MoveTo(Point p) {
				X = p.X;
				Y = p.Y;
			}
		}

		private LevelSaveType saveType;
		private string savePath;

		public Dictionary<int, string> metaData;
		public List<TileLayer> tileMaps;
		public List<Entity> entities;

		public int Width { get; private set; }
		public int Height { get; private set; }

		public LevelContainer(string path, VisualPackMetadata meta) {
			string ext = Path.GetExtension(path);

			metaData = new Dictionary<int, string>();
			tileMaps = new List<TileLayer>();
			entities = new List<Entity>();

			path = Path.Combine(Projects.ProjectInfo.CurrentProject.ProjectDirectory, "levels", path);

			switch (ext) {
				case ".txt":
					saveType = LevelSaveType.TextFile;

					savePath = path;

					using (StreamReader reader = new StreamReader(File.OpenRead(path))) {
						string readNextSafe() {
							string l;
							do {
								l = reader.ReadLine();
							}
							while (!reader.EndOfStream && string.IsNullOrWhiteSpace(l));

							return l;
						}

						string line = reader.ReadLine();
						Match match = Regex.Match(line, @"(\d+)\s*-\s*(\d+)\s*-\s*(\d+)");
						Width = int.Parse(match.Groups[1].Value);
						Height = int.Parse(match.Groups[2].Value);

						int layerCount = int.Parse(match.Groups[3].Value);

						while (!reader.EndOfStream) {
							match = Regex.Match(readNextSafe(), @"(\w+)(?:\s*-\s*(\d))*");
							line = match.Groups[1].Value;
							if (line == null)
								break;
							switch (line) {
								case "meta":

									while ((line = readNextSafe()) != "end") {

										match = Regex.Match(line, @"(\d+)\s*;\s*(.+)\s*");

										metaData.Add(int.Parse(match.Groups[1].Value), match.Groups[2].Value);
									}

									break;
								case "layer":
									int layerIndex = int.Parse(match.Groups[2].Value);
									while (tileMaps.Count < layerIndex + 1)
										tileMaps.Add(null);

									char[,] array = new char[Width, Height];

									for (int y = 0; y < Height; ++y) {
										line = reader.ReadLine();
										for (int x = 0; x < Width; ++x) {
											array[x, y] = line[x];
										}
									}

									tileMaps[layerIndex] = new TileLayer(array);

									break;
								case "entities":
									while ((line = readNextSafe()) != "end") {
										string[] split = line.Split(';');

										var ent = new Entity(split);
										entities.Add(ent);

										int index;
										if (!int.TryParse(ent.entType, out index)) {
											index = meta.EntityIndex[ent.entType];
										}
										if (meta.EntitySprites.ContainsKey(index)) {

											var data = meta.EntitySprites[index];

											ent.sprite = new Image(Atlases.GameSprites[$"{data.Sprite}"]);
											ent.Width = data.Width;
											ent.Height = data.Height;
											ent.RenderOffset = new Point(data.RenderOffset.X, data.RenderOffset.Y);
											ent.Offset = new Point(data.Offset.X, data.Offset.Y);
										}
									}
									break;
							}
						}
					}

					break;
				
				default:
					throw new Exception();
			}
		}
		public LevelContainer(LevelSaveType saveType, int width, int height, int layerCount) {
			this.saveType = saveType;

			metaData = new Dictionary<int, string>();
			
			tileMaps = new List<TileLayer>();
			for (int i = 0; i < layerCount; ++i)
				tileMaps.Add(new TileLayer(width, height));

			entities = new List<Entity>();

			Width = width;
			Height = height;

		}

		public void Extend(int left, int right, int up, int down) {
			Width += left + right;
			Height += up + down;

			foreach (var ent in entities) {
				ent.X += left;
				ent.Y += up;
			}
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

								sw.WriteLine(sb.ToString());
							}

							sw.WriteLine("end");
						}

						sw.WriteLine("entities");

						foreach (var ent in entities) {
							sw.Write($"{ent.entType};{ent.X};{ent.Y}");

							foreach (var m in ent.metaData) {
								sw.Write($";{m}");
							}
							sw.WriteLine();
						}

						sw.WriteLine("end");

						sw.Flush();
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
		public const int TileSize = 8;

		private LevelEditorStates baseState, dragState;

		private VirtualMap<char> currentLayer;
		private VirtualMap<char>[] editingGrids;
		private TileGrid[] visualGrids;
		private TileGrid currentVisuals;
		private int layerIndex;
		private int zoomIndex;
		private char brushValue;
		private int brushIndex;
		private bool erasing => brushIndex == 0;

		private LevelContainer.Entity selectedEntity;

		private TilesetPalette palette;

		private Vector2 mouseDownPosition;
		private Point resizeDirection;

		public int BrushIndex {
			get => brushIndex;
			set {
				brushIndex = Math.Clamp(value, 0, VisualData.CharIndex.Count - 1);
				if (brushIndex == 0) {
				}
				else {
					brushValue = VisualData.CharIndex[brushIndex];
				}
			}
		}
		public char BrushValue {
			get => brushValue;
			set {
				if (value == ' ') {
					brushIndex = 0;
				}
				else {
					brushIndex = VisualData.CharIndex.IndexOf(value);
					brushValue = value;
				}
			}
		}

		public LevelContainer MainLevel { get; private set; }
		public LevelPack VisualData { get; private set; }

		public LevelEditorScene() : base(new Image(Atlases.EngineGraphics["UI/scenes/level_editor_icon"])) {
			Camera.Zoom = ZOOM_START;
			zoomIndex = Array.IndexOf(ZOOM_LEVELS, ZOOM_START);

			baseState = LevelEditorStates.Draw;

			OnMouseDown += MouseDown;
			OnMouseUp += MouseUp;


			palette = UIBounds.AddChild(new TilesetPalette(this)) as TilesetPalette;

			LoadLevel("prologue/lvl1s.txt");

			//UIBounds.AddChild(new IconBarButton(new Image(Atlases.EngineGraphics["UI/folder"])) {
			//	OnClick = () => {

			//		var task = new Task(OpenNew);
			//		task.Wait();

			//		return null;
			//	}
			//});



		}

		private void LoadLevelLocal(string file) {

			var visual = Projects.ProjectInfo.CurrentProject.GetPack(file);

			if (visual == null)
				return;

			VisualData = visual;

			selectedEntity = null;

			while (HelperEntity.Get<TileGrid>() != null) {
				HelperEntity.Remove(HelperEntity.Get<TileGrid>());
			}

			MainLevel = new LevelContainer(file, visual.VisualData);

			LoadTilesets();
			palette.UpdateTo(VisualData);

			BrushValue = VisualData.CharIndex[1];

			UndoStates.Clear();
		}

		public void LoadLevel(string file) {
			if (UndoStates.Dirty) {

				switch (PromptHandler.AskForSaving("Warning!  Your level hasn't been saved!  Do you want to save before continuing?")) {
					case "saveContinue":
						MainLevel.Save();
						goto case "continue";
					case "continue":
						LoadLevelLocal(file);
						break;
				}
				//task.Start();
			}
			else {
				LoadLevelLocal(file);
			}

		}

		private void LoadTilesets() {
			visualGrids = new TileGrid[MainLevel.tileMaps.Count];
			editingGrids = new VirtualMap<char>[MainLevel.tileMaps.Count];

			for (int i = 0; i < visualGrids.Length; ++i) {

				currentLayer = MainLevel.tileMaps[i].tiles;
				editingGrids[i] = currentLayer;

				HelperEntity.Add(visualGrids[i] = new TileGrid(TileSize, TileSize, MainLevel.Width, MainLevel.Height));
				currentVisuals = visualGrids[i];

				currentVisuals.Color = Color.White * 0.4f;

				currentVisuals.Depth = -i * 10;


				SettleAllTiles();
			}

			SwitchToLayer(0);

		}
		public void SwitchToLayer(int layer) {

			currentVisuals.Color = Color.White * 0.4f;

			currentLayer = MainLevel.tileMaps[layer].tiles;
			currentVisuals = visualGrids[layer];

			currentVisuals.Color = Color.White;

			layerIndex = layer;
		}

		public void Extend(int left, int right, int up, int down) {

			int index = layerIndex;

			MainLevel.Extend(left, right, up, down);

			for (int i = 0; i < visualGrids.Length; ++i) {
				Extend(i, left, right, up, down);

				if (left > 0) {
					for (int x = 0; x < left; ++x)
						for (int y = 0; y < MainLevel.Height; y++)
							SetTileLocal(x, y, currentLayer[x, y]);

					SettleTiles();
				}
				if (right > 0) {
					for (int x = MainLevel.Width - right; x < MainLevel.Width; ++x)
						for (int y = 0; y < MainLevel.Height; y++)
							SetTileLocal(x, y, currentLayer[x, y]);

					SettleTiles();
				}
				if (up > 0) {
					for (int y = 0; y < up; ++y)
						for (int x = 0; x < MainLevel.Width; x++)
							SetTileLocal(x, y, currentLayer[x, y]);

					SettleTiles();
				}
				if (down > 0) {
					for (int y = MainLevel.Height - down; y < MainLevel.Height; ++y)
						for (int x = 0; x < MainLevel.Width; x++)
							SetTileLocal(x, y, currentLayer[x, y]);

					SettleTiles();
				}
				//SettleAllTiles();
			}

			Camera.Position += new Vector2(left * TileSize, up * TileSize);

			SwitchToLayer(index);

		}
		private void Extend(int layer, int left, int right, int up, int down) {

			SwitchToLayer(layer);

			int TilesX = currentLayer.Columns,
				TilesY = currentLayer.Rows;

			int newWidth = TilesX + left + right;
			int newHeight = TilesY + up + down;
			if (newWidth <= 0 || newHeight <= 0) {
				throw new Exception();
			}

			var newTiles = new VirtualMap<char>(newWidth, newHeight, ' ');

			//Center
			for (int x = 0; x < TilesX; x++) {
				for (int y = 0; y < TilesY; y++) {
					int atX = x + left;
					int atY = y + up;

					if (atX >= 0 && atX < newWidth && atY >= 0 && atY < newHeight)
						newTiles[atX, atY] = currentLayer[x, y];
				}
			}

			//Left
			for (int x = 0; x < left; x++)
				for (int y = 0; y < newHeight; y++)
					newTiles[x, y] = currentLayer[0, Calc.Clamp(y - up, 0, TilesY - 1)];

			//Right
			for (int x = newWidth - right; x < newWidth; x++)
				for (int y = 0; y < newHeight; y++)
					newTiles[x, y] = currentLayer[TilesX - 1, Calc.Clamp(y - up, 0, TilesY - 1)];

			//Top
			for (int y = 0; y < up; y++)
				for (int x = 0; x < newWidth; x++)
					newTiles[x, y] = currentLayer[Calc.Clamp(x - left, 0, TilesX - 1), 0];

			//Bottom
			for (int y = newHeight - down; y < newHeight; y++)
				for (int x = 0; x < newWidth; x++)
					newTiles[x, y] = currentLayer[Calc.Clamp(x - left, 0, TilesX - 1), TilesY - 1];

			MainLevel.tileMaps[layer].tiles = newTiles;
			currentLayer = newTiles;

			currentVisuals.Extend(left, right, up, down);
			currentVisuals.Position = Vector2.Zero;
		}

		private void MouseDown(int x, int y, bool newScene) {
			if (UIFramework.HoveredControl != null) {
				dragState = LevelEditorStates.Cancel;
				return;
			}

			if (MInput.Keyboard.Check(Keys.Space)) {
				dragState = LevelEditorStates.Pan;
				OnMouseDrag += MouseDragged;
				return;
			}
			if (newScene)
				return;

			Rectangle bounds = new Rectangle(0, 0, MainLevel.Width * TileSize, MainLevel.Height * TileSize);
			Vector2 canvasPosition = WindowToCanvas(new Vector2(x, y));

			mouseDownPosition = canvasPosition;

			if (!bounds.Contains((int)canvasPosition.X, (int)canvasPosition.Y) && !MInput.ShiftsCheck) {

				bounds.Inflate(20, 20);

				if (bounds.Contains((int)canvasPosition.X, (int)canvasPosition.Y)) {

					dragState = LevelEditorStates.Resize;
					resizeDirection = Point.Zero;

					if (canvasPosition.X < 0)
						resizeDirection.X = -1;
					else if (canvasPosition.X > MainLevel.Width * TileSize)
						resizeDirection.X = 1;
					if (canvasPosition.Y < 0)
						resizeDirection.Y = -1;
					else if (canvasPosition.Y > MainLevel.Height * TileSize)
						resizeDirection.Y = 1;

					OnMouseDrag += MouseDragged;
				}


				return;
			}



			OnMouseDrag += MouseDragged;

			var state = baseState;

			LevelContainer.Entity entity = null;
			bool reselect = false;

			foreach (var ent in MainLevel.entities) {
				if (ent.bounds.Contains((int)canvasPosition.X, (int)canvasPosition.Y)) {
					if (ent == selectedEntity) {
						reselect = true;
					}
					if (entity != null && !reselect) 
						continue;
					entity = ent;
				}
			}

			selectedEntity = entity;

			if (entity != null) {

				resizeDirection = new Point(entity.X, entity.Y);
				dragState = LevelEditorStates.MoveEntity;
			}
			else {
				if (MInput.ControlsCheck) {
					dragState = LevelEditorStates.Switch;
				}
				else if (MInput.AltsCheck) {
					dragState = LevelEditorStates.Eyedrop;
				}
				else if (MInput.ShiftsCheck) {
					if (state == LevelEditorStates.Draw)
						dragState = LevelEditorStates.Rectangle;
				}
			}
			if (dragState != LevelEditorStates.None) {
				state = dragState;
			}

			switch (state) {
				case LevelEditorStates.Draw:
					SetTile(MInput.Mouse.Position, erasing ? ' ' : brushValue);
					break;
				case LevelEditorStates.Switch: {

					Vector2 mouse = WindowToGrid(MInput.Mouse.Position);
					for (int i = 0; i < visualGrids.Length; ++i) {
						if (MainLevel.tileMaps[i].tiles[(int)mouse.X, (int)mouse.Y] != ' ') {
							SwitchToLayer(i);
							break;
						}
					}
					break;
				}
				case LevelEditorStates.Eyedrop: {
					Vector2 mouse = WindowToGrid(MInput.Mouse.Position);

					baseState = LevelEditorStates.Draw;
					BrushValue = currentLayer[(int)mouse.X, (int)mouse.Y];

					break;
				}
			}
		}

		private void MouseUp(int x, int y, bool newScene) {

			var state = baseState;
			if (dragState != LevelEditorStates.None) {
				state = dragState;
			}
			if (newScene)
				state = LevelEditorStates.None;

			switch (state) {
				case LevelEditorStates.Rectangle:
					Vector2 start = new Vector2((int)(mouseDownPosition.X / TileSize), (int)(mouseDownPosition.Y / TileSize));
					Vector2 end = WindowToGrid(MInput.Mouse.Position);

					DrawRectangle((int)Math.Min(start.X, end.X), (int)Math.Min(start.Y, end.Y), (int)Math.Abs(start.X - end.X) + 1, (int)Math.Abs(start.Y - end.Y) + 1, erasing ? ' ' : brushValue);

					break;
				case LevelEditorStates.Resize:
					Vector2 offset = ResizeOffset();

					Extend(resizeDirection.X < 0 ? (int)-offset.X : 0, resizeDirection.X > 0 ? (int)offset.X : 0,
						   resizeDirection.Y < 0 ? (int)-offset.Y : 0, resizeDirection.Y > 0 ? (int)offset.Y : 0);

					for (int i = 0; i < visualGrids.Length; ++i) {
						visualGrids[i].ClipRectangle = null;
					}

					break;
			}

			SetUndoState(state);

			dragState = LevelEditorStates.None;
			OnMouseDrag -= MouseDragged;
		}

		private void MouseDragged(int x, int y, bool newScene) {

			var state = baseState;
			if (dragState != LevelEditorStates.None) {
				state = dragState;
			}

			switch (state) {
				case LevelEditorStates.Pan:
					if (MInput.Mouse.WasMoved)
						Camera.Position -= MInput.Mouse.PositionDelta / Camera.Zoom;
					break;
				case LevelEditorStates.Draw:
					DrawLine(MInput.Mouse.Position, new Vector2(MInput.Mouse.PreviousState.X, MInput.Mouse.PreviousState.Y), erasing ? ' ' : brushValue);
					break;
				case LevelEditorStates.MoveEntity:
					Vector2 offset = GridDragOffset();
					selectedEntity.X = Calc.Clamp(resizeDirection.X + (int)offset.X, 0, 255);
					selectedEntity.Y = Calc.Clamp(resizeDirection.Y + (int)offset.Y, 0, 255);
					break;
			}

		}

		private Vector2 GridDragOffset() {
			Vector2 offset = WindowToCanvas(MInput.Mouse.Position);
			offset = new Vector2(Calc.Round((offset.X - mouseDownPosition.X) / TileSize), Calc.Round((offset.Y - mouseDownPosition.Y) / TileSize));

			return offset;
		}
		private Vector2 ResizeOffset() {
			Vector2 offset = WindowToCanvas(MInput.Mouse.Position);
			offset = new Vector2(Calc.Round((offset.X - mouseDownPosition.X) / TileSize), Calc.Round((offset.Y - mouseDownPosition.Y) / TileSize));

			offset.X = Math.Max(offset.X * resizeDirection.X, 32 - MainLevel.Width) * resizeDirection.X;
			offset.Y = Math.Max(offset.Y * resizeDirection.Y, 22 -MainLevel.Height) * resizeDirection.Y;

			return offset;
		}
		private Vector2 WindowToCanvas(Vector2 vector) {

			vector.X -= VisualBounds.X;
			vector.Y -= VisualBounds.Y;
			vector /= Camera.Zoom;
			vector += Camera.Position;

			return vector;
		}
		private Vector2 WindowToGrid(Vector2 vector) {

			vector.X -= VisualBounds.X;
			vector.Y -= VisualBounds.Y;
			vector /= Camera.Zoom;
			vector += Camera.Position;

			vector /= TileSize;

			vector.X = (int)vector.X;
			vector.Y = (int)vector.Y;

			return vector;
		}

		#region Set Tiles
		List<Point> points = new List<Point>();
		Dictionary<Point, char> editedValues = new Dictionary<Point, char>();
		Dictionary<Point, char> oldValues = new Dictionary<Point, char>();

		private void UndoGrid(Dictionary<Point, char> states) {
			foreach (var item in states) {
				SetTile(item.Key, item.Value);
			}
			editedValues.Clear();
			oldValues.Clear();

			SettleTiles();
		}

		private void SetTileLocal(int x, int y, char value) {
			if (x < 0 || y < 0 || x >= MainLevel.Width || y >= MainLevel.Height) {
				return;
			}
			if (currentLayer[x, y] != value) {
				Point p = new Point(x, y);
				if (!points.Contains(p))
					points.Add(p);

				foreach (var item in VisualData.updateOffsets) {
					Point po = p + item;
					if (po.X < 0 || po.Y < 0 || po.X >= MainLevel.Width || po.Y >= MainLevel.Height)
						continue;

					if (!points.Contains(p + item))
						points.Add(p + item);
				}

				editedValues.Add(new Point(x, y), value);
				oldValues.Add(new Point(x, y), currentLayer[x, y]);

				currentLayer[x, y] = value;
			}
		}
		private void SettleTiles() {

			foreach (var item in points) {
				
				currentVisuals.Tiles[item.X, item.Y] = VisualData.GetTile(item.X, item.Y, currentLayer);
			}
			points.Clear();
		}
		private void SettleAllTiles() {

			for (int x = 0; x < MainLevel.Width; ++x)
				for (int y = 0; y < MainLevel.Height; ++y)
					currentVisuals.Tiles[x, y] = VisualData.GetTile(x, y, currentLayer);

			points.Clear();
		}

		private void SetUndoState(LevelEditorStates state) {

			if (editedValues.Count > 0) {

				UndoStates.Push(new UndoState() {
					Undo = (obj) => UndoGrid((Dictionary<Point, char>)obj),
					Redo = (obj) => UndoGrid((Dictionary<Point, char>)obj),
					UndoValue = oldValues,
					RedoValue = editedValues
				});

				editedValues = new Dictionary<Point, char>();
				oldValues = new Dictionary<Point, char>();
			}
			else if (state == LevelEditorStates.MoveEntity) {

				var ent = selectedEntity;

				UndoStates.Push(new UndoState() {
					Undo = (obj) => ent.MoveTo((Point)obj),
					Redo = (obj) => ent.MoveTo((Point)obj),
					UndoValue = resizeDirection,
					RedoValue = new Point(selectedEntity.X, selectedEntity.Y),
				});
			}
		}

		public void SetTile(int x, int y, char value) {
			SetTileLocal(x, y, value);
			SettleTiles();
		}
		public void SetTile(Vector2 position, char value) {
			position = WindowToGrid(position);

			SetTile((int)position.X, (int)position.Y, value);
		}
		public void SetTile(Point position, char value) {

			SetTile(position.X, position.Y, value);
		}

		public void DrawLine(Point start, Point end, char value) {
			int stride;
			if (Math.Abs(start.X - end.X) > Math.Abs(start.Y - end.Y))
				stride = Math.Abs(start.X - end.X);
			else
				stride = Math.Abs(start.Y - end.Y);

			for (int i = 0; i <= stride; ++i) {
				Vector2 lerped = Vector2.Lerp(new Vector2(start.X, start.Y), new Vector2(end.X, end.Y), (float) i / stride);
				SetTileLocal((int)Math.Round(lerped.X), (int)Math.Round(lerped.Y), value);
			}
			SettleTiles();
		}
		public void DrawLine(Vector2 start, Vector2 end, char value) {
			start = WindowToGrid(start);
			end = WindowToGrid(end);

			DrawLine(
				new Point((int)start.X, (int)start.Y),
				new Point((int)end.X, (int)end.Y), value);
		}

		public void DrawRectangle(Rectangle rect, char value) {
			DrawRectangle(rect.X, rect.Y, rect.Width, rect.Height, value);
		}
		public void DrawRectangle(int x, int y, int width, int height, char value) {
			if (width <= 0 || height <= 0)
				return;

			x = Math.Max(x, 0);
			y = Math.Max(y, 0);

			width = Math.Min(x + width, MainLevel.Width);
			height = Math.Min(y + height, MainLevel.Height);

			for (; x < width; ++x) {
				for (int yy = y; yy < height; ++yy) {
					SetTileLocal(x, yy, value);
				}
			}
			SettleTiles();
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
				BrushValue = brushValue;
				baseState = MInput.ShiftsCheck ? LevelEditorStates.Rectangle : LevelEditorStates.Draw;
			}
			if (MInput.Keyboard.Pressed(Keys.E)) {
				brushIndex = 0;
				baseState = MInput.ShiftsCheck ? LevelEditorStates.Rectangle : LevelEditorStates.Draw;
				
			}
			if (MInput.ControlsCheck && MInput.Keyboard.Pressed(Keys.S)) {
				MainLevel.Save();
				UndoStates.SetNotDirty();
			}

			if (MInput.Mouse.CheckLeftButton && MInput.Keyboard.Pressed(Keys.Escape)) {
				for (int i = 0; i < visualGrids.Length; ++i) {
					visualGrids[i].ClipRectangle = null;
				}
				switch (dragState) {
					case LevelEditorStates.MoveEntity:

						selectedEntity.X = Calc.Clamp(resizeDirection.X, 0, 255);
						selectedEntity.Y = Calc.Clamp(resizeDirection.Y, 0, 255);
						break;
				}

				dragState = LevelEditorStates.Cancel;
			}

			if (MInput.Keyboard.Pressed(Keys.Delete) || MInput.Keyboard.Pressed(Keys.Back)) {
				if (selectedEntity != null) {
					MainLevel.entities.Remove(selectedEntity);
				}
			}

			if (MInput.Keyboard.Pressed(Keys.D1)) {
				baseState = LevelEditorStates.Draw;
				BrushIndex = 0;
			}
			else if (MInput.Keyboard.Pressed(Keys.D2)) {
				baseState = LevelEditorStates.Draw;
				BrushIndex = 1;
			}
			else if (MInput.Keyboard.Pressed(Keys.D3)) {
				baseState = LevelEditorStates.Draw;
				BrushIndex = 2;
			}
			else if (MInput.Keyboard.Pressed(Keys.D4)) {
				baseState = LevelEditorStates.Draw;
				BrushIndex = 3;
			}
		}

		public override void Update() {
			base.Update();
			if (VisualBounds.Contains(MInput.Mouse.X, MInput.Mouse.Y) && MInput.Mouse.WheelDelta != 0) {

				if (MInput.AltsCheck) {
					BrushIndex -= Math.Sign(MInput.Mouse.WheelDelta);
				}
				else {
					Camera.Position -= (new Vector2(VisualBounds.X, VisualBounds.Y) - MInput.Mouse.Position) / Camera.Zoom;

					zoomIndex = Calc.Clamp(zoomIndex + Math.Sign(MInput.Mouse.WheelDelta), 0, ZOOM_LEVELS.Length - 1);

					Camera.Zoom = ZOOM_LEVELS[zoomIndex];

					Camera.Position += (new Vector2(VisualBounds.X, VisualBounds.Y) - MInput.Mouse.Position) / Camera.Zoom;
				}
				
			}
		}

		public override void DrawGraphics() {


			var state = baseState;
			if (dragState != LevelEditorStates.None) {
				state = dragState;
			}

			int left = 0, up = 0, width = MainLevel.Width, height = MainLevel.Height;

			if (MInput.Mouse.CheckLeftButton && state == LevelEditorStates.Resize) {
				Vector2 offset = ResizeOffset();

				if (resizeDirection.X > 0) {
					width += (int)offset.X;
				}
				else if (resizeDirection.X < 0) {
					left += (int)offset.X;
					width -= (int)offset.X;
				}
				if (resizeDirection.Y > 0) {
					height += (int)offset.Y;
				}
				else if (resizeDirection.Y < 0) {
					up += (int)offset.Y;
					height -= (int)offset.Y;
				}

				for (int i = 0; i < visualGrids.Length; ++i) {
					visualGrids[i].ClipRectangle = new Rectangle(left, up, width + (left * 2), height + (up * 2));
				}
			}

			base.DrawGraphics();

			Draw.Depth += 10;

			if (MInput.Mouse.CheckLeftButton && state == LevelEditorStates.Rectangle) {
				Vector2 start = new Vector2(Calc.SnapFloor(mouseDownPosition.X, TileSize), Calc.SnapFloor(mouseDownPosition.Y, TileSize));
				Vector2 end = WindowToGrid(MInput.Mouse.Position) * TileSize;

				Rectangle rect = new Rectangle((int)Math.Min(start.X, end.X), (int)Math.Min(start.Y, end.Y), (int)Math.Abs(start.X - end.X) + TileSize, (int)Math.Abs(start.Y - end.Y) + TileSize);

				Draw.Rect(rect, Color.White * 0.3f);
				Draw.Depth += 2;
				Draw.HollowRect(rect, Color.White, 1);
			}

			Draw.Depth += 100;

			foreach (var ent in MainLevel.entities) {
				if (ent.sprite != null) {
					ent.sprite.drawDepth = Draw.Depth;
					ent.sprite.Position = new Vector2(ent.X * TileSize + ent.RenderOffset.X, ent.Y * TileSize + ent.RenderOffset.Y);
					ent.sprite.Render();
				}
				Draw.HollowRect(ent.bounds, ent == selectedEntity ? Color.White : Color.Magenta, 1, 2);
			}

			Draw.Depth = Draw.FARTHEST_DEPTH;
			Draw.Rect(left * TileSize, up * TileSize, width * TileSize, height * TileSize, ColorSchemes.CurrentScheme.CanvasBackground);

			int right = width + left;
			int down = height + up;

			Draw.Depth++;
			for (int i = left + 1; i < right; ++i)
				Draw.Rect(TileSize * i, up * TileSize, 1 / Camera.Zoom, height * TileSize, Color.Black);
			for (int i = up + 1; i < down; ++i)
				Draw.Rect(left * TileSize, TileSize * i, width * TileSize, 1 / Camera.Zoom, Color.Black);

		}
	}
}
