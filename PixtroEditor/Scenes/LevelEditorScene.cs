﻿using System.Text;
using System;
using Monocle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Pixtro.Editor;

namespace Pixtro.Scenes {
	public enum LevelEditorStates {
		None,
		Draw,
		Pan,
	}
	public class LevelEditorScene : Scene {

		private const int TileSize = 8;
		private const int tempCountW = 30, tempCountH = 20;

		private LevelEditorStates baseState, heldState;

		private VirtualMap<int> rawTilemap;
		private TileGrid visualGrid;
		private Tileset testTileset;

		public LevelEditorScene() {
			Camera.Zoom = 2;
			rawTilemap = new VirtualMap<int>(tempCountW, tempCountH, 0);

			HelperEntity.Add(visualGrid = new TileGrid(TileSize, TileSize, tempCountW, tempCountH));
			testTileset = new Tileset(Atlases.EngineGraphics["test tileset"], 8, 8);

			baseState = LevelEditorStates.Draw;
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

		private void OnMouseDown(int x, int y, bool newScene) {
			if (MInput.Keyboard.Check(Keys.Space)) {
				heldState = LevelEditorStates.Pan;
				Engine.OnMouseDrag += MouseDragged;
				return;
			}
			if (newScene)
				return;

			Engine.OnMouseDrag += MouseDragged;

			SetTile(MInput.Mouse.Position, 1);
		}

		private void OnMouseUp(int x, int y) {
			heldState = LevelEditorStates.None;
			Engine.OnMouseDrag -= MouseDragged;
		}

		private void MouseDragged(int x, int y) {

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
			}

		}

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

		public override void FocusedUpdate() {
			base.FocusedUpdate();

			if (MInput.Keyboard.Check(Keys.Q)) {
				visualGrid.Clear();
				for (int tx = 0; tx < rawTilemap.Columns; tx++)
					for (int ty = 0; ty < rawTilemap.Rows; ty++)
						rawTilemap[tx, ty] = 0;
			}

			if (MInput.Mouse.WheelDelta != 0) {
				Camera.Position -= (new Vector2(VisualBounds.X, VisualBounds.Y) - MInput.Mouse.Position) / Camera.Zoom;

				if (Camera.Zoom <= 1) {
					Camera.Zoom *= MInput.Mouse.WheelDelta > 0 ? 2 : 0.5f;
				}
				else {
					Camera.Zoom += MInput.Mouse.WheelDelta > 0 ? 1 : -1;
				}

				Camera.Zoom = Calc.Clamp(Camera.Zoom, 0.125f, 6);

				Camera.Position += (new Vector2(VisualBounds.X, VisualBounds.Y) - MInput.Mouse.Position) / Camera.Zoom;
			}
		}

		public override void GainFocus() {
			base.GainFocus();
			Engine.OnMouseDown += OnMouseDown;
			Engine.OnMouseUp += OnMouseUp;
		}

		public override void LoseFocus() {
			base.LoseFocus();
			Engine.OnMouseDown -= OnMouseDown;
		}

		public override void DrawGraphics() {
			base.DrawGraphics();
			Draw.Depth = Draw.FARTHEST_DEPTH;
			Draw.Rect(0, 0, rawTilemap.Columns * TileSize, rawTilemap.Rows * TileSize, ColorSchemes.CurrentScheme.CanvasBackground);
		}
	}
}
