using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Monocle;

namespace Pixtro.UI {
	public abstract class Control {
		internal List<Control> children = new List<Control>();

		public Control() {
			Interactable = true;
			
		}

		internal protected Control Parent;

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

		public Point Position { get => new Point(Bounds.X, Bounds.Y);
			set {
				Bounds = new Rectangle(value.X, value.Y, LocalBounds.Width, LocalBounds.Height);
			}
		}
		public Point LocalPosition {
			get => new Point(LocalBounds.X, LocalBounds.Y);
			set {
				LocalBounds.X = value.X;
				LocalBounds.Y = value.Y;
			}
		}

		public int LocalDepth;
		public int Depth {
			get {
				var rect = LocalDepth;
				if (Parent != null)
					rect += Parent.Depth;
				
				return rect;
			}
			set {
				if (Parent != null)
					value -= Parent.Depth;

				LocalDepth = value;
			}
		}

		public bool Interactable { get; set; }

		#region Children

		public void AddChild(Control child) {
			if (child.Parent != null)
				return;

			children.Add(child);
			child.Parent = this;
			if (UIFramework.HasControl(this))
				UIFramework.AddControl(child);
		}
		public void RemoveChild(Control child) {
			if (child.Parent != this)
				return;

			children.Remove(child);
			child.Parent = null;
		}

		#endregion

		#region Control Events

		internal void Click() {
			if (OnClicked != null)
				OnClicked(this, new EventArgs() { });
		}

		internal void Hover() {
			if (OnHover != null)
				OnHover(this, new EventArgs() { });
		}

		public event EventHandler OnHover, OnClicked;

		#endregion

		internal protected virtual void ClickHeldUpdate() { }
		internal protected virtual void Update() { }
		internal protected virtual void Render() { }
	}
}
