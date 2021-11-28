using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Monocle;

namespace Pixtro.UI {
	public abstract class Control : Entity {
		private List<Control> children = new List<Control>();

		public Control() {
			
		}

		protected Control Parent;

		public Rectangle LocalBounds;
		public Rectangle Bounds {
			get {
				var rect = LocalBounds;
				if (Parent != null) {
					rect.X += Parent.Bounds.X;
					rect.Y += Parent.Bounds.Y;
				}
				return rect;
			}
			set {
				if (Parent != null) {
					value.X -= Parent.Bounds.X;
					value.Y -= Parent.Bounds.Y;
				}
				LocalBounds = value;
			}
		}

		public int LocalDepth;
		public int Depth {
			get {
				var rect = Depth;
				if (Parent != null)
					rect += Parent.Depth;
				
				return rect;
			}
			set {
				if (Parent != null)
					value -= Parent.Depth;
				
				Depth = value;
			}
		}

		public virtual void Render() { }
	}
}
