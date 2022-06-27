using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Monocle;

namespace Pixtro.UI {
	public class UITransform {

		public Point Size;
		public Point Offset;
		public Vector2 Center;

		internal UITransform parent;

		Vector2 anchor;
		public Vector2 Anchor {
			get => anchor;
			set {
				anchor = Calc.Clamp01(value);
			}
		}
		public float AnchorX {
			get => anchor.X;
			set {
				anchor.X = Calc.Clamp01(value);
			}
		}
		public float AnchorY {
			get => anchor.Y;
			set {
				anchor.Y = Calc.Clamp01(value);
			}
		}

		public Point Position {
			get => new Point(X, Y);
			set {
				X = value.X;
				Y = value.Y;
			}
		}
		public int X {
			get {
				float width = parent == null ? Engine.ViewWidth : parent.Size.X;
				int value = (int)(width * anchor.X) +  Offset.X - (int)(Size.X * Center.X);
				if (parent != null)
					value += parent.X;
				return value;
			}
			set {
				value += (int)(Size.X * Center.X);
				value -= (int)(Engine.ViewWidth * anchor.X);

				Offset.X = value;
			}
		}
		public int Y {
			get {
				float height = parent == null ? Engine.ViewHeight : parent.Size.Y;
				int value =  (int)(height * anchor.Y) +  Offset.Y - (int)(Size.Y * Center.Y);
				if (parent != null)
					value += parent.Y;

				return value;
			}
			set {
				value += (int)(Size.Y * Center.Y);
				value -= (int)(Engine.ViewHeight * anchor.Y);

				Offset.Y = value;
			}
		}

		public Rectangle Bounds {
			get => new Rectangle(X, Y, Size.X, Size.Y);
			set {
				Size = new Point(value.Width, value.Height);
				Position = new Point(value.X, value.Y);
			}
		}

		public void SetParent(UITransform t, bool keepLocation) {
			Point pos = Position;
			parent = t;

			if (keepLocation) {
				Position = pos;
			}
		}
	}
	public abstract class Control {
		internal List<Control> children = new List<Control>();

		public Control() {
			Interactable = true;
			_trans = new UITransform();
			LocalDepth = 5;
		}

		internal protected Control Parent;

		
		UITransform _trans;
		public UITransform Transform {
			get => _trans;
			set {
				if (value == null)
					return;
				_trans.SetParent(null, false);
				foreach (var child in children) {
					child.Transform.SetParent(value, false);
				}
				if (Parent != null) {
					value.SetParent(Parent._trans, false);
				}
			}
		}

		public Point Position { 
			get => _trans.Position;
			set {
				_trans.Position = value;
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

		public Control AddChild(Control child) {
			if (child.Parent != null) {
				child.Parent.RemoveChild(child);
			}

			child.Transform.SetParent(Transform, false);
			children.Add(child);
			child.Parent = this;
			UIFramework.AddControl(child);

			return child;
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
