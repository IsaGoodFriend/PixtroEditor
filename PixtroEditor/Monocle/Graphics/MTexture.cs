﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Monocle {
	public class MTexture {
		static public MTexture FromFile(string filename) {
			var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
			var texture = Texture2D.FromStream(Engine.Instance.GraphicsDevice, fileStream);
			fileStream.Close();

			return new MTexture(texture);
		}

		public MTexture() { }

		public MTexture(Texture2D texture) {
			Texture = texture;
			_originalTexture = Texture;
			AtlasPath = null;
			ClipRect = new Rectangle(0, 0, Texture.Width, Texture.Height);
			DrawOffset = Vector2.Zero;
			Width = ClipRect.Width;
			Height = ClipRect.Height;
			SetUtil();
		}

		public MTexture(MTexture parent, int x, int y, int width, int height) {
			Parent = parent;
			AtlasPath = null;

			ClipRect = parent.GetRelativeRect(x, y, width, height);
			DrawOffset = new Vector2(-Math.Min(x - parent.DrawOffset.X, 0), -Math.Min(y - parent.DrawOffset.Y, 0));
			Width = width;
			Height = height;
			SetUtil();
		}

		public MTexture(MTexture parent, Rectangle clipRect)
			: this(parent, clipRect.X, clipRect.Y, clipRect.Width, clipRect.Height) {

		}

		public MTexture(MTexture parent, string atlasPath, Rectangle clipRect, Vector2 drawOffset, int width, int height) {
			Parent = parent;
			AtlasPath = atlasPath;

			ClipRect = parent.GetRelativeRect(clipRect);
			DrawOffset = drawOffset;
			Width = width;
			Height = height;
			SetUtil();
		}

		public MTexture(MTexture parent, string atlasPath, Rectangle clipRect)
			: this(parent, clipRect) {
			AtlasPath = atlasPath;
		}

		public MTexture(Texture2D texture, Vector2 drawOffset, int frameWidth, int frameHeight) {
			Texture = texture;
			_originalTexture = Texture;

			ClipRect = new Rectangle(0, 0, texture.Width, texture.Height);
			DrawOffset = drawOffset;
			Width = frameWidth;
			Height = frameHeight;
			SetUtil();
		}

		public MTexture(int width, int height, Color color) {
			Texture = new Texture2D(Engine.Instance.GraphicsDevice, width, height);
			_originalTexture = Texture;

			var colors = new Color[width * height];
			for (int i = 0; i < width * height; i++)
				colors[i] = color;
			Texture.SetData<Color>(colors);

			ClipRect = new Rectangle(0, 0, width, height);
			DrawOffset = Vector2.Zero;
			Width = width;
			Height = height;
			SetUtil();
		}

		private void SetUtil() {
			Center = new Vector2(Width, Height) * 0.5f;
			LeftUV = ClipRect.Left / (float)Texture.Width;
			RightUV = ClipRect.Right / (float)Texture.Width;
			TopUV = ClipRect.Top / (float)Texture.Height;
			BottomUV = ClipRect.Bottom / (float)Texture.Height;
		}

		public void Unload() {
			Texture.Dispose();
			Texture = null;
			_originalTexture.Dispose();
			_originalTexture = null;
		}

		public MTexture GetSubtexture(int x, int y, int width, int height, MTexture applyTo = null) {
			if (applyTo == null)
				return new MTexture(this, x, y, width, height);
			else {
				applyTo.Texture = Texture;
				applyTo.AtlasPath = null;

				applyTo.ClipRect = GetRelativeRect(x, y, width, height);
				applyTo.DrawOffset = new Vector2(-Math.Min(x - DrawOffset.X, 0), -Math.Min(y - DrawOffset.Y, 0));
				applyTo.Width = width;
				applyTo.Height = height;
				applyTo.SetUtil();

				return applyTo;
			}
		}

		public MTexture GetSubtexture(Rectangle rect) {
			return new MTexture(this, rect);
		}

		public void ApplyPalette(Texture2D palette, Texture2D importPalette) {
			if (Parent != null) {
				Parent.ApplyPalette(palette, importPalette);
				return;
			}
			Texture2D newTex = new Texture2D(_originalTexture.GraphicsDevice, Width, Height);

			uint[] data = new uint[Width * Height], palData = new uint[palette.Width * 2];

			_originalTexture.GetData(data);
			palette.GetData(palData);

			int palSize = palData.Length / 2;

			for (int i = 0; i < data.Length; ++i) {
				for (int j = 0; j < palSize; ++j) {
					if (data[i] == palData[j]) {
						data[i] = palData[j + palSize];
						break;
					}
				}
			}

			newTex.SetData(data);

			Texture = newTex;
		}

		public void Dispose() {
			Texture.Dispose();
		}

		#region Properties

		public MTexture Parent { get; private set; }
		public Texture2D Texture {
			get {
				if (Parent != null)
					return Parent.Texture;
				else
					return texture;
			}
			set {
				if (Parent == null)
					texture = value;
			}
		}
		private Texture2D texture;
		private Texture2D _originalTexture;
		public Rectangle ClipRect { get; private set; }
		public string AtlasPath { get; private set; }
		public Vector2 DrawOffset { get; private set; }
		public int Width { get; private set; }
		public int Height { get; private set; }
		public Vector2 Center { get; private set; }
		public float LeftUV { get; private set; }
		public float RightUV { get; private set; }
		public float TopUV { get; private set; }
		public float BottomUV { get; private set; }

		#endregion

		#region Helpers

		public override string ToString() {
			if (AtlasPath != null)
				return AtlasPath;
			else
				return "MTexture [" + Texture.Width + " x " + Texture.Height + "]";
		}

		public Rectangle GetRelativeRect(Rectangle rect) {
			return GetRelativeRect(rect.X, rect.Y, rect.Width, rect.Height);
		}

		public Rectangle GetRelativeRect(int x, int y, int width, int height) {
			int atX = (int)(ClipRect.X - DrawOffset.X + x);
			int atY = (int)(ClipRect.Y - DrawOffset.Y + y);

			int rX = (int)MathHelper.Clamp(atX, ClipRect.Left, ClipRect.Right);
			int rY = (int)MathHelper.Clamp(atY, ClipRect.Top, ClipRect.Bottom);
			int rW = Math.Max(0, Math.Min(atX + width, ClipRect.Right) - rX);
			int rH = Math.Max(0, Math.Min(atY + height, ClipRect.Bottom) - rY);

			return new Rectangle(rX, rY, rW, rH);
		}


		public int TotalPixels {
			get { return Width * Height; }
		}

		#endregion

		#region Draw

		protected virtual void DrawLocal(Texture2D tex, Vector2 position, Rectangle clip, Color color, float rotation, Vector2 offset, Vector2 scale, SpriteEffects effect) {
			var sb = Monocle.Draw.SpriteBatch;

			Monocle.Draw.SpriteBatch.Draw(tex, position, clip, color, rotation, offset, scale, effect, Monocle.Draw.RealDepth);
			
		}

		private void DrawLocal(Texture2D tex, Vector2 position, Rectangle clip, Color color, float rotation, Vector2 offset, float scale, SpriteEffects effect) {
			DrawLocal(tex, position, clip, color, rotation, offset, Vector2.One * scale, effect);
		}

		public void Draw(Vector2 position) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, Color.White, 0, -DrawOffset, 1f, SpriteEffects.None);
		}

		public void Draw(Vector2 position, Vector2 origin) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, Color.White, 0, origin - DrawOffset, 1f, SpriteEffects.None);
		}

		public void Draw(Vector2 position, Vector2 origin, Color color) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, 0, origin - DrawOffset, 1f, SpriteEffects.None);
		}

		public void Draw(Vector2 position, Vector2 origin, Color color, float scale) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, 0, origin - DrawOffset, scale, SpriteEffects.None);
		}

		public void Draw(Vector2 position, Vector2 origin, Color color, float scale, float rotation) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, rotation, origin - DrawOffset, scale, SpriteEffects.None);
		}

		public void Draw(Vector2 position, Vector2 origin, Color color, float scale, float rotation, SpriteEffects flip) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, rotation, origin - DrawOffset, scale, flip);
		}

		public void Draw(Vector2 position, Vector2 origin, Color color, Vector2 scale) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, 0, origin - DrawOffset, scale, SpriteEffects.None);
		}

		public void Draw(Vector2 position, Vector2 origin, Color color, Vector2 scale, float rotation) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, rotation, origin - DrawOffset, scale, SpriteEffects.None);
		}

		public void Draw(Vector2 position, Vector2 origin, Color color, Vector2 scale, float rotation, SpriteEffects flip) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, rotation, origin - DrawOffset, scale, flip);
		}

		public void Draw(Vector2 position, Vector2 origin, Color color, Vector2 scale, float rotation, Rectangle clip) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, GetRelativeRect(clip), color, rotation, origin - DrawOffset, scale, SpriteEffects.None);
		}

		#endregion

		#region Draw Centered

		public void DrawCentered(Vector2 position) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, Color.White, 0, Center - DrawOffset, 1f, SpriteEffects.None);
		}

		public void DrawCentered(Vector2 position, Color color) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, 0, Center - DrawOffset, 1f, SpriteEffects.None);
		}

		public void DrawCentered(Vector2 position, Color color, float scale) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, 0, Center - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawCentered(Vector2 position, Color color, float scale, float rotation) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, rotation, Center - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawCentered(Vector2 position, Color color, float scale, float rotation, SpriteEffects flip) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, rotation, Center - DrawOffset, scale, flip);
		}

		public void DrawCentered(Vector2 position, Color color, Vector2 scale) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, 0, Center - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawCentered(Vector2 position, Color color, Vector2 scale, float rotation) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, rotation, Center - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawCentered(Vector2 position, Color color, Vector2 scale, float rotation, SpriteEffects flip) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, rotation, Center - DrawOffset, scale, flip);
		}

		#endregion

		#region Draw Justified

		public void DrawJustified(Vector2 position, Vector2 justify) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, Color.White, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, 1f, SpriteEffects.None);
		}

		public void DrawJustified(Vector2 position, Vector2 justify, Color color) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, 1f, SpriteEffects.None);
		}

		public void DrawJustified(Vector2 position, Vector2 justify, Color color, float scale) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawJustified(Vector2 position, Vector2 justify, Color color, float scale, float rotation) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawJustified(Vector2 position, Vector2 justify, Color color, float scale, float rotation, SpriteEffects flip) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, flip);
		}

		public void DrawJustified(Vector2 position, Vector2 justify, Color color, Vector2 scale) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawJustified(Vector2 position, Vector2 justify, Color color, Vector2 scale, float rotation) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawJustified(Vector2 position, Vector2 justify, Color color, Vector2 scale, float rotation, SpriteEffects flip) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif
			DrawLocal(Texture, position, ClipRect, color, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, flip);
		}

		#endregion

		#region Draw Outline

		public void DrawOutline(Vector2 position) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, -DrawOffset, 1f, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, Color.White, 0, -DrawOffset, 1f, SpriteEffects.None);
		}

		public void DrawOutline(Vector2 position, Vector2 origin) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, origin - DrawOffset, 1f, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, Color.White, 0, origin - DrawOffset, 1f, SpriteEffects.None);
		}

		public void DrawOutline(Vector2 position, Vector2 origin, Color color) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, origin - DrawOffset, 1f, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, color, 0, origin - DrawOffset, 1f, SpriteEffects.None);
		}

		public void DrawOutline(Vector2 position, Vector2 origin, Color color, float scale) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, origin - DrawOffset, scale, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, color, 0, origin - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawOutline(Vector2 position, Vector2 origin, Color color, float scale, float rotation) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, origin - DrawOffset, scale, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, color, rotation, origin - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawOutline(Vector2 position, Vector2 origin, Color color, float scale, float rotation, SpriteEffects flip) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, origin - DrawOffset, scale, flip);

			DrawLocal(Texture, position, ClipRect, color, rotation, origin - DrawOffset, scale, flip);
		}

		public void DrawOutline(Vector2 position, Vector2 origin, Color color, Vector2 scale) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, origin - DrawOffset, scale, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, color, 0, origin - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawOutline(Vector2 position, Vector2 origin, Color color, Vector2 scale, float rotation) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, origin - DrawOffset, scale, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, color, rotation, origin - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawOutline(Vector2 position, Vector2 origin, Color color, Vector2 scale, float rotation, SpriteEffects flip) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, origin - DrawOffset, scale, flip);

			DrawLocal(Texture, position, ClipRect, color, rotation, origin - DrawOffset, scale, flip);
		}

		#endregion

		#region Draw Outline Centered

		public void DrawOutlineCentered(Vector2 position) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, Center - DrawOffset, 1f, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, Color.White, 0, Center - DrawOffset, 1f, SpriteEffects.None);
		}

		public void DrawOutlineCentered(Vector2 position, Color color) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, Center - DrawOffset, 1f, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, color, 0, Center - DrawOffset, 1f, SpriteEffects.None);
		}

		public void DrawOutlineCentered(Vector2 position, Color color, float scale) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, Center - DrawOffset, scale, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, color, 0, Center - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawOutlineCentered(Vector2 position, Color color, float scale, float rotation) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, Center - DrawOffset, scale, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, color, rotation, Center - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawOutlineCentered(Vector2 position, Color color, float scale, float rotation, SpriteEffects flip) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, Center - DrawOffset, scale, flip);

			DrawLocal(Texture, position, ClipRect, color, rotation, Center - DrawOffset, scale, flip);
		}

		public void DrawOutlineCentered(Vector2 position, Color color, Vector2 scale) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, Center - DrawOffset, scale, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, color, 0, Center - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawOutlineCentered(Vector2 position, Color color, Vector2 scale, float rotation) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, Center - DrawOffset, scale, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, color, rotation, Center - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawOutlineCentered(Vector2 position, Color color, Vector2 scale, float rotation, SpriteEffects flip) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, Center - DrawOffset, scale, flip);

			DrawLocal(Texture, position, ClipRect, color, rotation, Center - DrawOffset, scale, flip);
		}

		#endregion

		#region Draw Outline Justified

		public void DrawOutlineJustified(Vector2 position, Vector2 justify) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, 1f, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, Color.White, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, 1f, SpriteEffects.None);
		}

		public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, 1f, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, color, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, 1f, SpriteEffects.None);
		}

		public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color, float scale) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, color, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color, float scale, float rotation) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, color, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color, float scale, float rotation, SpriteEffects flip) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, flip);

			DrawLocal(Texture, position, ClipRect, color, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, flip);
		}

		public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color, Vector2 scale) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, color, 0, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color, Vector2 scale, float rotation) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None);

			DrawLocal(Texture, position, ClipRect, color, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, SpriteEffects.None);
		}

		public void DrawOutlineJustified(Vector2 position, Vector2 justify, Color color, Vector2 scale, float rotation, SpriteEffects flip) {
#if DEBUG
			if (Texture.IsDisposed)
				throw new Exception("Texture2D Is Disposed");
#endif

			for (var i = -1; i <= 1; i++)
				for (var j = -1; j <= 1; j++)
					if (i != 0 || j != 0)
						DrawLocal(Texture, position + new Vector2(i, j), ClipRect, Color.Black, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, flip);

			DrawLocal(Texture, position, ClipRect, color, rotation, new Vector2(Width * justify.X, Height * justify.Y) - DrawOffset, scale, flip);
		}

		#endregion
	}
}
