using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Monocle {
	public struct DrawRect {
		public static explicit operator Rectangle(DrawRect r) {
			return new Rectangle((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
		}
		public static explicit operator DrawRect(Rectangle r) {
			return new DrawRect(r.X, r.Y, r.Width, r.Height);
		}
		public float X, Y, Width, Height;

		public DrawRect(float _x, float _y, float _width, float _height) {
			X = _x;
			Y = _y;
			Width = _width;
			Height = _height;
		}

		public void Inflate(float x, float y) {
			Width += x;
			Height += y;
			X -= x / 2;
			Y -= y / 2;
		}
	}
	public enum WindowType {
		XOnly, Normal, Maximized
	}
	public static class Draw {
		/// <summary>
		/// The currently-rendering Renderer
		/// </summary>
		public static Renderer Renderer { get; internal set; }

		/// <summary>
		/// All 2D rendering is done through this SpriteBatch instance
		/// </summary>
		public static SpriteBatch SpriteBatch { get; private set; }

		/// <summary>
		/// A subtexture used to draw particle systems.
		/// Will be generated at startup, but you can replace this with a subtexture from your Atlas to reduce texture swaps.
		/// Should be a 2x2 white pixel
		/// </summary>
		public static MTexture Particle;

		/// <summary>
		/// A subtexture used to draw rectangles and lines. 
		/// Will be generated at startup, but you can replace this with a subtexture from your Atlas to reduce texture swaps.
		/// Use the top left pixel of your Particle Subtexture if you replace it!
		/// Should be a 1x1 white pixel
		/// </summary>
		public static MTexture Pixel;

		public static Matrix WorldBase, WorldSecondary;

		public const int CLOSEST_DEPTH = DepthPrecision >> 1;
		public const int FARTHEST_DEPTH = -(DepthPrecision >> 1);

		private const int DepthPrecision = 1 << 15;

		private const float SB_DEPTH_DIV = 1f / DepthPrecision;

		public static int Depth {
			get { return entityDepth; }
			set {
				entityDepth = value;
				RealDepth = (value * SB_DEPTH_DIV) + 0.5f;
			}
		}
		public static float RealDepth;
		private static Rectangle rect;
		private static int entityDepth;

		public static SpriteFont DefaultFont;

		enum VertType {
			Position,
			PositionColor,
			PositionColorTexture,
			PositionNormalTexture,
			PositionTexture,
		}
		struct TransparentDraw {
			public object points;
			public int startIndex, count;
			public float depth;
			public VertType type;
			public Effect material;

			public Action<Effect> onrender;
		}

		private static List<TransparentDraw> transparent = new List<TransparentDraw>();

		internal static void UpdatePerFrame() {

		}
		internal static void Initialize(GraphicsDevice graphicsDevice) {
			SpriteBatch = new SpriteBatch(graphicsDevice);
			UseDebugPixelTexture();

			DefaultFont = Engine.Instance.Content.Load<SpriteFont>("Fonts/DefaultFont");
		}

		public static void UseDebugPixelTexture() {
			MTexture texture = new MTexture(2, 2, Color.White);
			Pixel = new MTexture(texture, 0, 0, 1, 1);
			Particle = new MTexture(texture, 0, 0, 2, 2);
		}

		public static void Point(Vector2 at, Color color) {
			SpriteBatch.Draw(Pixel.Texture, at, Pixel.ClipRect, color, 0, Vector2.Zero, 1f, SpriteEffects.None, 0);
		}

		public static void TriangleList<T>(T[] array, int index, int amount, Effect material) where T : struct, IVertexType {
			if (array == null || amount == 0 || material == null)
				return;



			Engine.Graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
			Engine.Graphics.GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;// = BlendState.AlphaBlend;// = RasterizerState.CullNone;

			var gd = Engine.Instance.GraphicsDevice;

			foreach (var mat in material.Techniques) {

				foreach (var pass in mat.Passes) {
					pass.Apply();
					gd.DrawUserPrimitives(PrimitiveType.TriangleList, array, index, amount);
				}
			}


		}

		public static void TriangleListTransparent<T>(T[] array, int index, int amount, Effect material, Action<Effect> onRender = null) where T : struct, IVertexType {
			if (array == null || amount == 0 || material == null)
				return;

			var obj = new TransparentDraw() {
				points = array,
				startIndex = index,
				count = amount,
				material = material,
				onrender = onRender,
			};

			switch (array[0]) {
				case VertexPosition v:
					obj.type = VertType.Position;
					break;
				case VertexPositionColor v:
					obj.type = VertType.PositionColor;
					break;
				case VertexPositionColorTexture v:
					obj.type = VertType.PositionColorTexture;
					break;
				case VertexPositionNormalTexture v:
					obj.type = VertType.PositionNormalTexture;
					break;
				case VertexPositionTexture v:
					obj.type = VertType.PositionTexture;
					break;
			}

			transparent.Add(obj);
		}

		public static void FinalizeDraw() {
			foreach (var draw in transparent) {
				if (draw.onrender != null) {
					draw.onrender(draw.material);
				}

				switch (draw.type) {
					case VertType.Position:
						TriangleList((VertexPosition[])draw.points, draw.startIndex, draw.count, draw.material);
						break;
					case VertType.PositionColor:
						TriangleList((VertexPositionColor[])draw.points, draw.startIndex, draw.count, draw.material);
						break;
					case VertType.PositionColorTexture:
						TriangleList((VertexPositionColorTexture[])draw.points, draw.startIndex, draw.count, draw.material);
						break;
					case VertType.PositionNormalTexture:
						TriangleList((VertexPositionNormalTexture[])draw.points, draw.startIndex, draw.count, draw.material);
						break;
					case VertType.PositionTexture:
						TriangleList((VertexPositionTexture[])draw.points, draw.startIndex, draw.count, draw.material);
						break;
				}
					
			}

			transparent.Clear();
		}

		#region Line

		public static void Line(Vector2 start, Vector2 end, Color color, int _depthOffset = 0) {
			LineAngle(start, Calc.Angle(start, end), Vector2.Distance(start, end), color, _depthOffset);
		}

		public static void Line(Vector2 start, Vector2 end, Color color, float thickness, int _depthOffset = 0) {
			LineAngle(start, Calc.Angle(start, end), Vector2.Distance(start, end), color, thickness, _depthOffset);
		}

		public static void Line(float x1, float y1, float x2, float y2, Color color, int _depthOffset = 0) {
			Line(new Vector2(x1, y1), new Vector2(x2, y2), color, _depthOffset);
		}

		#endregion

		#region Line Angle

		public static void LineAngle(Vector2 start, float angle, float length, Color color) {
			SpriteBatch.Draw(Pixel.Texture, start, Pixel.ClipRect, color, angle, Vector2.Zero, new Vector2(length, 1), SpriteEffects.None, RealDepth);
		}
		public static void LineAngle(Vector2 start, float angle, float length, Color color, int _depthOffset) {
			SpriteBatch.Draw(Pixel.Texture, start, Pixel.ClipRect, color, angle, Vector2.Zero, new Vector2(length, 1), SpriteEffects.None, RealDepth + (_depthOffset * SB_DEPTH_DIV));
		}

		public static void LineAngle(Vector2 start, float angle, float length, Color color, float thickness, int _depthOffset = 0) {
			SpriteBatch.Draw(Pixel.Texture, start, Pixel.ClipRect, color, angle, new Vector2(0, .5f), new Vector2(length, thickness), SpriteEffects.None, RealDepth + (_depthOffset * SB_DEPTH_DIV));
		}

		public static void LineAngle(float startX, float startY, float angle, float length, Color color, int _depthOffset = 0) {
			LineAngle(new Vector2(startX, startY), angle, length, color, _depthOffset);
		}

		#endregion

		#region Arrow

		public static void Arrow(Vector2 start, Vector2 end, Color color, float thickness, float pointLength = 32, float pointAngle = 0.3f, int _depthOffset = 0) {
			Line(start, end, color, thickness, _depthOffset);
			float angle = Calc.Angle(start - end);
			LineAngle(end, angle + pointAngle, pointLength, color, thickness);
			LineAngle(end, angle - pointAngle, pointLength, color, thickness);
		}
		public static void Arrow(Vector2 start, Vector2 end, Color color, float pointLength = 32, float pointAngle = 0.3f, int _depthOffset = 0) {
			Arrow(start, end, color, 2, pointLength, pointAngle, _depthOffset);
		}

		#endregion

		#region Circle

		public static void Circle(Vector2 position, float radius, Color color, int resolution) {
			Vector2 last = Vector2.UnitX * radius;
			Vector2 lastP = last.Perpendicular();
			for (int i = 1; i <= resolution; i++) {
				Vector2 at = Calc.AngleToVector(i * MathHelper.PiOver2 / resolution, radius);
				Vector2 atP = at.Perpendicular();

				Line(position + last, position + at, color);
				Line(position - last, position - at, color);
				Line(position + lastP, position + atP, color);
				Line(position - lastP, position - atP, color);

				last = at;
				lastP = atP;
			}
		}

		public static void Circle(float x, float y, float radius, Color color, int resolution) {
			Circle(new Vector2(x, y), radius, color, resolution);
		}

		public static void Circle(Vector2 position, float radius, Color color, float thickness, int resolution) {
			Vector2 last = Vector2.UnitX * radius;
			Vector2 lastP = last.Perpendicular();
			for (int i = 1; i <= resolution; i++) {
				Vector2 at = Calc.AngleToVector(i * MathHelper.PiOver2 / resolution, radius);
				Vector2 atP = at.Perpendicular();

				Line(position + last, position + at, color, thickness);
				Line(position - last, position - at, color, thickness);
				Line(position + lastP, position + atP, color, thickness);
				Line(position - lastP, position - atP, color, thickness);

				last = at;
				lastP = atP;
			}
		}

		public static void Circle(float x, float y, float radius, Color color, float thickness, int resolution) {
			Circle(new Vector2(x, y), radius, color, thickness, resolution);
		}

		#endregion

		#region Rect

		public static void Rect(float x, float y, float width, float height, Color color, int _depthOffset = 0) {
			//rect.X = (int)x;
			//rect.Y = (int)y;
			//rect.Width = (int)width;
			//rect.Height = (int)height;
			SpriteBatch.Draw(Pixel.Texture, new Vector2(x, y), Pixel.ClipRect, color, 0, Vector2.Zero, new Vector2(width, height), SpriteEffects.None, RealDepth + (_depthOffset * SB_DEPTH_DIV));
		}

		public static void Rect(Vector2 position, float width, float height, Color color, int _depthOffset = 0) {
			Rect(position.X, position.Y, width, height, color, _depthOffset);
		}

		public static void Rect(Rectangle rect, Color color) {
			Draw.rect = rect;

			SpriteBatch.Draw(Pixel.Texture, rect, Pixel.ClipRect, color, 0, Vector2.Zero, SpriteEffects.None, RealDepth);
		}
		public static void Rect(Rectangle rect, Color color, int _depthOffset) {
			Draw.rect = rect;

			SpriteBatch.Draw(Pixel.Texture, rect, Pixel.ClipRect, color, 0, Vector2.Zero, SpriteEffects.None, RealDepth + (_depthOffset * SB_DEPTH_DIV));
		}
		public static void Rect(DrawRect rect, Color color, int _depthOffset = 0) {
			Rect(rect.X, rect.Y, rect.Width, rect.Height, color, _depthOffset);
		}

		#endregion

		#region Hollow Rect

		public static void HollowRect(float x, float y, float width, float height, Color color) {
			rect.X = (int)x;
			rect.Y = (int)y;
			rect.Width = (int)width;
			rect.Height = 1;

			SpriteBatch.Draw(Pixel.Texture, rect, Pixel.ClipRect, color, 0, Vector2.Zero, SpriteEffects.None, RealDepth);

			rect.Y += (int)height - 1;

			SpriteBatch.Draw(Pixel.Texture, rect, Pixel.ClipRect, color, 0, Vector2.Zero, SpriteEffects.None, RealDepth);

			rect.Y -= (int)height - 1;
			rect.Width = 1;
			rect.Height = (int)height;

			SpriteBatch.Draw(Pixel.Texture, rect, Pixel.ClipRect, color, 0, Vector2.Zero, SpriteEffects.None, RealDepth);

			rect.X += (int)width - 1;

			SpriteBatch.Draw(Pixel.Texture, rect, Pixel.ClipRect, color, 0, Vector2.Zero, SpriteEffects.None, RealDepth);
		}
		public static void HollowRect(float x, float y, float width, float height, Color color, int size = 1, int _depthOffset = 0) {
			float d = RealDepth + (_depthOffset * SB_DEPTH_DIV);

			rect.X = (int)x;
			rect.Y = (int)y;
			rect.Width = (int)width;
			rect.Height = size;

			SpriteBatch.Draw(Pixel.Texture, rect, Pixel.ClipRect, color, 0, Vector2.Zero, SpriteEffects.None, d);

			rect.Y += (int)height - size;

			SpriteBatch.Draw(Pixel.Texture, rect, Pixel.ClipRect, color, 0, Vector2.Zero, SpriteEffects.None, d);

			rect.Y -= (int)height - size;
			rect.Width = size;
			rect.Height = (int)height;

			SpriteBatch.Draw(Pixel.Texture, rect, Pixel.ClipRect, color, 0, Vector2.Zero, SpriteEffects.None, d);

			rect.X += (int)width - size;

			SpriteBatch.Draw(Pixel.Texture, rect, Pixel.ClipRect, color, 0, Vector2.Zero, SpriteEffects.None, d);
		}

		public static void HollowRect(Vector2 position, float width, float height, Color color, int size = 1, int _depthOffset = 0) {
			HollowRect(position.X, position.Y, width, height, color, size, _depthOffset);
		}

		public static void HollowRect(Rectangle rect, Color color, int size = 1, int _depthOffset = 0) {
			HollowRect(rect.X, rect.Y, rect.Width, rect.Height, color, size, _depthOffset);
		}

		#endregion

		#region Text

		public static Vector2 MeasureText(string text, SpriteFont font = null) {
			font = font ?? DefaultFont;
			return font.MeasureString(text);
		}
		public static void Text(string text, Vector2 position, Color color, SpriteFont font = null, int _depthOffset = 0) {
			SpriteBatch.DrawString(font??DefaultFont, text, Calc.Floor(position), color, 0, Vector2.Zero, 1, SpriteEffects.None, RealDepth + (_depthOffset * SB_DEPTH_DIV));
		}	
		public static void Text(string text, Vector2 position, Color color, Vector2 origin, Vector2 scale, float rotation, SpriteFont font = null, int _depthOffset = 0) {
			SpriteBatch.DrawString(font??DefaultFont, text, Calc.Floor(position), color, rotation, origin, scale, SpriteEffects.None, RealDepth + (_depthOffset * SB_DEPTH_DIV));
		}

		public static void TextOutline(SpriteFont font, string text, Vector2 position, Color color, Color bgColor, float _offset = 1, int _depthOffset = 0) {

			position = Calc.Floor(position);

			SpriteBatch.DrawString(font, text, position, color, 0, Vector2.Zero, 1, SpriteEffects.None, RealDepth + (_depthOffset * SB_DEPTH_DIV));

			float depth = RealDepth + ((_depthOffset - 1) * SB_DEPTH_DIV);

			SpriteBatch.DrawString(font, text, (position + new Vector2(_offset, 0)), bgColor, 0, Vector2.Zero, 1, SpriteEffects.None, depth);
			SpriteBatch.DrawString(font, text, (position + new Vector2(-_offset, 0)), bgColor, 0, Vector2.Zero, 1, SpriteEffects.None, depth);
			SpriteBatch.DrawString(font, text, (position + new Vector2(0, _offset)), bgColor, 0, Vector2.Zero, 1, SpriteEffects.None, depth);
			SpriteBatch.DrawString(font, text, (position + new Vector2(0, -_offset)), bgColor, 0, Vector2.Zero, 1, SpriteEffects.None, depth);
		}
		public static void TextOutline(SpriteFont font, string text, Vector2 position, Color color, Color bgColor, int _count, float _offset = 1, int _depthOffset = 0) {

			SpriteBatch.DrawString(font, text, Calc.Floor(position), color, 0, Vector2.Zero, 1, SpriteEffects.None, RealDepth + (_depthOffset * SB_DEPTH_DIV));

			float depth = RealDepth + ((_depthOffset - 1) * SB_DEPTH_DIV);

			for (float f = 0; f < MathHelper.TwoPi; f += (MathHelper.TwoPi / _count))
				SpriteBatch.DrawString(font, text, Calc.Floor(position + Calc.AngleToVector(f, _offset)), bgColor, 0, Vector2.Zero, 1, SpriteEffects.None, depth);
		}

		public static void TextJustified(SpriteFont font, string text, Vector2 position, Color color, Vector2 justify) {
			Vector2 origin = font.MeasureString(text);
			origin.X *= justify.X;
			origin.Y *= justify.Y;

			SpriteBatch.DrawString(font, text, Calc.Floor(position), color, 0, origin, 1, SpriteEffects.None, RealDepth);
		}

		public static void TextJustified(SpriteFont font, string text, Vector2 position, Color color, float scale, Vector2 justify) {
			Vector2 origin = font.MeasureString(text);
			origin.X *= justify.X;
			origin.Y *= justify.Y;
			SpriteBatch.DrawString(font, text, Calc.Floor(position), color, 0, origin, scale, SpriteEffects.None, RealDepth);
		}

		public static void TextCentered(string text, Vector2 position, SpriteFont font = null, int _depthOffset = 0) {
			font = font??DefaultFont;
			Text(text, position - font.MeasureString(text) * .5f, Color.White, font, _depthOffset: _depthOffset);
		}

		public static void TextCentered(string text, Vector2 position, Color color, SpriteFont font = null, int _depthOffset = 0) {
			font = font??DefaultFont;
			Text(text, position - MeasureText(text, font: font) * .5f, color, font, _depthOffset: _depthOffset);
		}

		public static void TextCentered(string text, Vector2 position, Color color, float scale, float rotation, SpriteFont font = null) {
			Text(text, position, color, font.MeasureString(text) * .5f, Vector2.One * scale, rotation, font);
		}

		public static void OutlineTextCentered(SpriteFont font, string text, Vector2 position, Color color, float scale) {
			Vector2 origin = font.MeasureString(text) / 2;

			for (int i = -1; i < 2; i++)
				for (int j = -1; j < 2; j++)
					if (i != 0 || j != 0)
						SpriteBatch.DrawString(font, text, Calc.Floor(position) + new Vector2(i, j), Color.Black, 0, origin, scale, SpriteEffects.None, 0);
			SpriteBatch.DrawString(font, text, Calc.Floor(position), color, 0, origin, scale, SpriteEffects.None, 0);
		}

		public static void OutlineTextCentered(SpriteFont font, string text, Vector2 position, Color color, Color outlineColor) {
			Vector2 origin = font.MeasureString(text) / 2;

			for (int i = -1; i < 2; i++)
				for (int j = -1; j < 2; j++)
					if (i != 0 || j != 0)
						SpriteBatch.DrawString(font, text, Calc.Floor(position) + new Vector2(i, j), outlineColor, 0, origin, 1, SpriteEffects.None, 0);
			SpriteBatch.DrawString(font, text, Calc.Floor(position), color, 0, origin, 1, SpriteEffects.None, 0);
		}

		public static void OutlineTextCentered(SpriteFont font, string text, Vector2 position, Color color, Color outlineColor, float scale) {
			Vector2 origin = font.MeasureString(text) / 2;

			for (int i = -1; i < 2; i++)
				for (int j = -1; j < 2; j++)
					if (i != 0 || j != 0)
						SpriteBatch.DrawString(font, text, Calc.Floor(position) + new Vector2(i, j), outlineColor, 0, origin, scale, SpriteEffects.None, 0);
			SpriteBatch.DrawString(font, text, Calc.Floor(position), color, 0, origin, scale, SpriteEffects.None, 0);
		}

		public static void OutlineTextJustify(SpriteFont font, string text, Vector2 position, Color color, Color outlineColor, Vector2 justify) {
			Vector2 origin = font.MeasureString(text) * justify;

			for (int i = -1; i < 2; i++)
				for (int j = -1; j < 2; j++)
					if (i != 0 || j != 0)
						SpriteBatch.DrawString(font, text, Calc.Floor(position) + new Vector2(i, j), outlineColor, 0, origin, 1, SpriteEffects.None, 0);
			SpriteBatch.DrawString(font, text, Calc.Floor(position), color, 0, origin, 1, SpriteEffects.None, 0);
		}

		public static void OutlineTextJustify(SpriteFont font, string text, Vector2 position, Color color, Color outlineColor, Vector2 justify, float scale) {
			Vector2 origin = font.MeasureString(text) * justify;

			for (int i = -1; i < 2; i++)
				for (int j = -1; j < 2; j++)
					if (i != 0 || j != 0)
						SpriteBatch.DrawString(font, text, Calc.Floor(position) + new Vector2(i, j), outlineColor, 0, origin, scale, SpriteEffects.None, 0);
			SpriteBatch.DrawString(font, text, Calc.Floor(position), color, 0, origin, scale, SpriteEffects.None, 0);
		}

		#endregion

		#region Weird Textures

		public static void SineTextureH(MTexture tex, Vector2 position, Vector2 origin, Vector2 scale, float rotation, Color color, SpriteEffects effects, float sineCounter, float amplitude = 2, int sliceSize = 2, float sliceAdd = MathHelper.TwoPi / 8) {
			position = Calc.Floor(position);
			Rectangle clip = tex.ClipRect;
			clip.Width = sliceSize;

			int num = 0;
			while (clip.X < tex.ClipRect.X + tex.ClipRect.Width) {
				Vector2 add = new Vector2(sliceSize * num, (float)Math.Round(Math.Sin(sineCounter + sliceAdd * num) * amplitude));
				SpriteBatch.Draw(tex.Texture, position, clip, color, rotation, origin - add, scale, effects, 0);

				num++;
				clip.X += sliceSize;
				clip.Width = Math.Min(sliceSize, tex.ClipRect.X + tex.ClipRect.Width - clip.X);
			}
		}

		public static void SineTextureV(MTexture tex, Vector2 position, Vector2 origin, Vector2 scale, float rotation, Color color, SpriteEffects effects, float sineCounter, float amplitude = 2, int sliceSize = 2, float sliceAdd = MathHelper.TwoPi / 8) {
			position = Calc.Floor(position);
			Rectangle clip = tex.ClipRect;
			clip.Height = sliceSize;

			int num = 0;
			while (clip.Y < tex.ClipRect.Y + tex.ClipRect.Height) {
				Vector2 add = new Vector2((float)Math.Round(Math.Sin(sineCounter + sliceAdd * num) * amplitude), sliceSize * num);
				SpriteBatch.Draw(tex.Texture, position, clip, color, rotation, origin - add, scale, effects, 0);

				num++;
				clip.Y += sliceSize;
				clip.Height = Math.Min(sliceSize, tex.ClipRect.Y + tex.ClipRect.Height - clip.Y);
			}
		}

		public static void TextureBannerV(MTexture tex, Vector2 position, Vector2 origin, Vector2 scale, float rotation, Color color, SpriteEffects effects, float sineCounter, float amplitude = 2, int sliceSize = 2, float sliceAdd = MathHelper.TwoPi / 8) {
			position = Calc.Floor(position);
			Rectangle clip = tex.ClipRect;
			clip.Height = sliceSize;

			int num = 0;
			while (clip.Y < tex.ClipRect.Y + tex.ClipRect.Height) {
				float fade = (clip.Y - tex.ClipRect.Y) / (float)tex.ClipRect.Height;
				clip.Height = (int)MathHelper.Lerp(sliceSize, 1, fade);
				clip.Height = Math.Min(sliceSize, tex.ClipRect.Y + tex.ClipRect.Height - clip.Y);

				Vector2 add = new Vector2((float)Math.Round(Math.Sin(sineCounter + sliceAdd * num) * amplitude * fade), clip.Y - tex.ClipRect.Y);
				SpriteBatch.Draw(tex.Texture, position, clip, color, rotation, origin - add, scale, effects, 0);

				num++;
				clip.Y += clip.Height;
			}
		}

		#endregion
	}
}
