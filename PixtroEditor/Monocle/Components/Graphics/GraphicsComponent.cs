using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Monocle {
	public abstract class GraphicsComponent : Component {
		public Vector2 Position;
		public Vector2 Origin;
		public Vector2 Scale = Vector2.One;
		public float Rotation;
		public Color Color = Color.White;
		public SpriteEffects Effects = SpriteEffects.None;
		public int RenderOffset = 0;

		public GraphicsComponent(bool active)
			: base(active, true) {

		}

		public float X {
			get { return Position.X; }
			set { Position.X = value; }
		}

		public float Y {
			get { return Position.Y; }
			set { Position.Y = value; }
		}

		public bool FlipX {
			get {
				return (Effects & SpriteEffects.FlipHorizontally) == SpriteEffects.FlipHorizontally;
			}

			set {
				Effects = value ? (Effects | SpriteEffects.FlipHorizontally) : (Effects & ~SpriteEffects.FlipHorizontally);
			}
		}

		public bool FlipY {
			get {
				return (Effects & SpriteEffects.FlipVertically) == SpriteEffects.FlipVertically;
			}

			set {
				Effects = value ? (Effects | SpriteEffects.FlipVertically) : (Effects & ~SpriteEffects.FlipVertically);
			}
		}

		public Vector2 RenderPosition {
			get {
				var renderPos = (Entity == null ? Vector2.Zero : Entity.Position) + Position;

				return renderPos;
			}

			set {
				Position = value - (Entity == null ? Vector2.Zero : Entity.Position);
			}
		}

		public override void Render() {
			Draw.Depth += RenderOffset;
		}

		public void DrawOutline(int offset = 1) {
			DrawOutline(Color.Black, offset);
		}

		public void DrawOutline(Color color, int offset = 1) {
			Vector2 pos = Position;
			Color was = Color;
			Color = color;

			for (int i = -1; i < 2; i++) {
				for (int j = -1; j < 2; j++) {
					if (Math.Abs(i) == Math.Abs(j)) {
						Position = pos + new Vector2(i * (offset * Engine.InvertScaling), j * (offset * Engine.InvertScaling));
						Render();
					}
				}
			}

			Position = pos;
			Color = was;
		}
	}
}
