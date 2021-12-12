using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;

namespace Pixtro.Editor
{
	public sealed class EditorLayout : IEnumerable<EditorLayout.LayoutWindow>
	{
		public static bool Resizing { get; private set; }
		public enum SplitDirection
		{
			None,
			Vertical,
			Horizontal
		}

		public interface ILayoutInfo
		{
			ILayoutInfo Parent { get; set; }
			Rectangle BoundingRect { get; set; }
			Point MinimumSize();
			void ResizeWindow(Rectangle bound);
			void FinalizeSize();
		}
		public class LayoutSplit : ILayoutInfo
		{
			public const int SPLIT_PIXEL_SIZE = 2;
			public const int SPLIT_LEEWAY = 3;

			public SplitDirection Direction = SplitDirection.Horizontal;
			public float SplitPercent { get => split;
				set
				{
					split = value;
					
					ResizeWindow(BoundingRect);
				}
			}
			private float split = 0.5f;
			private ILayoutInfo item1, item2;
			public ILayoutInfo Item1 { get => item1; set {
					item1 = value;
					item1.Parent = this;
				}
			}
			public ILayoutInfo Item2 { get => item2; set {
					item2 = value;
					item2.Parent = this;
				}
			}

			public ILayoutInfo Parent { get; set; }
			public Rectangle BoundingRect { get; set; }

			public Point MinimumSize()
			{
				int width = 0, height;

				var p1 = Item1.MinimumSize();
				var p2 = Item2.MinimumSize();

				// Swap coordinates temporarily if splitting vertical just to reuse code to check sizes
				if (Direction == SplitDirection.Vertical)
				{
					int temp = p1.X;
					p1.X = p1.Y;
					p1.Y = temp;

					temp = p2.X;
					p2.X = p2.Y;
					p2.Y = temp;
				}

				if (p1.X > 0 && p2.X > 0)
					width = p1.X + p2.X;
				else if (p1.X > 0)
					width = p1.X;
				else if (p2.X > 0)
					width = p2.X;

				width += SPLIT_PIXEL_SIZE;

				height = Math.Max(p1.Y, p2.Y);

				if (Direction == SplitDirection.Vertical)
					return new Point(height, width);
				else
					return new Point(width, height);
			}
			public void ResizeWindow(Rectangle bound)
			{
				BoundingRect = bound;

				if (Direction == SplitDirection.Vertical)
					bound.Height -= SPLIT_PIXEL_SIZE;
				else
					bound.Width -= SPLIT_PIXEL_SIZE;

				Rectangle rect1 = new Rectangle(bound.X, bound.Y,
					(Direction == SplitDirection.Horizontal) ? (int)(bound.Width * SplitPercent) : bound.Width,
					(Direction == SplitDirection.Horizontal) ? bound.Height : (int)(bound.Height * SplitPercent));

				Rectangle rect2 = (Direction == SplitDirection.Horizontal) ?
					new Rectangle(0, bound.Y, bound.Width - rect1.Width, bound.Height) :
					new Rectangle(bound.X, 0, bound.Width, bound.Height - rect1.Height);

				var size1 = Item1.MinimumSize();
				var size2 = Item2.MinimumSize();

				if (Direction == SplitDirection.Horizontal)
				{
					if (rect1.Width < size1.X)
					{
						rect2.Width -= size1.X - rect1.Width;
						rect1.Width = size1.X;
					}
					else if (rect2.Width < size2.X)
					{
						rect1.Width -= size2.X - rect2.Width;
						rect2.Width = size2.X;
					}
					rect2.X = rect1.Right + SPLIT_PIXEL_SIZE;
				}
				else
				{
					if (rect1.Height < size1.Y)
					{
						rect2.Height -= size1.Y - rect1.Height;
						rect1.Height = size1.Y;
					}
					else if (rect2.Height < size2.Y)
					{
						rect1.Height -= size2.Y - rect2.Height;
						rect2.Height = size2.Y;
					}
					rect2.Y = rect1.Bottom + SPLIT_PIXEL_SIZE;
				}

				Item1.ResizeWindow(rect1);
				Item2.ResizeWindow(rect2);
			}
			public void FinalizeSize() {
				ResizeWindow(BoundingRect);
			}
		}
		public class LayoutWindow : ILayoutInfo
		{
			public int MinimumWidth { get; internal set; } = 100;
			public int MinimumHeight { get; internal set; } = 100;

			public ILayoutInfo Parent { get; set; }
			public Rectangle BoundingRect { get; set; }
			public Scene RootScene { get; private set; }

			public LayoutWindow(Type sceneType) : this(Activator.CreateInstance(sceneType) as Scene)
			{
			}
			public LayoutWindow(Scene scene) {
				ChangeRootScene(scene);
			}

			public Point MinimumSize()
			{
				return new Point(MinimumWidth, MinimumHeight);
			}
			public void ResizeWindow(Rectangle bound)
			{
				BoundingRect = bound;
			}
			public void FinalizeSize()
			{
			}

			public void ChangeRootScene(Scene newScene) {
				if (RootScene != null)
					RootScene.End();

				RootScene = newScene;

				newScene.EditorLayout = this;
				newScene.PreviousBounds = newScene.VisualBounds;

				newScene.OnSetWindow(this);
				newScene.OnResize();

				newScene.Begin();
			}
		}


		public ILayoutInfo layout { get; private set; }

		private LayoutSplit adjustingLayout = null;

		public EditorLayout()
		{
			layout = new LayoutSplit();
			(layout as LayoutSplit).Item1 = new LayoutWindow(typeof(Scenes.EmulatorScene));
			(layout as LayoutSplit).Item2 = new LayoutWindow(typeof(Scenes.LevelEditorScene));

			OnResize(1280, 720);

			SplitAt(new Point(0, 500), SplitDirection.Vertical);

			var window = GetWindow(0b10);
			window.ChangeRootScene(new Scenes.ConsoleScene());

			Engine.OnMouseDown += MouseDown;
			Engine.ResizeEnd += OnResize;
		}

		private void MouseDown(int x, int y) {
			var item = GetElementAt(new Point(x, y));

			if (item != null && item is LayoutSplit) {
				adjustingLayout = item as LayoutSplit;

				Resizing = true;
				Engine.OnMouseDrag += MouseDrag;
				Engine.OnMouseUp += MouseUp;
			}
		}

		private void MouseUp(int x, int y) {
			Engine.OnMouseDrag -= MouseDrag;
			Resizing = false;
		}

		private void MouseDrag(int x, int y) {
			float percent = adjustingLayout.Direction == SplitDirection.Horizontal ?
				(x - adjustingLayout.BoundingRect.X + 2) / (float) adjustingLayout.BoundingRect.Width :
				(y - adjustingLayout.BoundingRect.Y + 2) / (float) adjustingLayout.BoundingRect.Height;

			adjustingLayout.SplitPercent = percent;

			foreach (var sc in GetFromSplit(adjustingLayout)) {
				sc.RootScene.OnResize();
				sc.RootScene.PreviousBounds = sc.RootScene.VisualBounds;
			}
		}

		private void OnResize(int width, int height) {

			layout.ResizeWindow(new Rectangle(0, EditorWindow.TOP_MENU_BAR, width, height - EditorWindow.HEIGHT_SUB));
			foreach (var layout in this) {
				layout.RootScene.PreviousBounds = layout.RootScene.VisualBounds;
			}
		}

		public SplitDirection GetSplitDirection(Point point)
		{
			return GetSplitDirection(layout, point);
		}
		private SplitDirection GetSplitDirection(ILayoutInfo info, Point point)
		{
			if (info is LayoutWindow)
			{
				return SplitDirection.None;
			}
			else
			{
				LayoutSplit split = info as LayoutSplit;

				if (split.Item1.BoundingRect.Contains(point))
				{
					return GetSplitDirection(split.Item1, point);
				}
				else if (split.Item2.BoundingRect.Contains(point))
				{
					return GetSplitDirection(split.Item2, point);
				}

				return split.Direction;
			}
		}

		public LayoutWindow GetWindow(int binary) {
			if (layout is LayoutWindow)
				return layout as LayoutWindow;

			return GetWindow(binary, layout as LayoutSplit);
		}
		private LayoutWindow GetWindow(int binary, LayoutSplit split) {
			ILayoutInfo info = (binary & 0x1) == 0 ? split.Item1 : split.Item2;

			if (info is LayoutWindow)
				return info as LayoutWindow;
			else
				return GetWindow(binary >> 1, info as LayoutSplit);
		}

		public ILayoutInfo GetElementAt(Point point)
		{
			if (layout.BoundingRect.Contains(point))
				return GetElementAt(layout, point);
			else
				return null;
		}
		private ILayoutInfo GetElementAt(ILayoutInfo info, Point point)
		{
			if (info is LayoutWindow)
			{
				return info;
			}
			else
			{
				LayoutSplit split = info as LayoutSplit;

				Rectangle rect1 = split.Item1.BoundingRect;
				Rectangle rect2 = split.Item2.BoundingRect;

				if (split.Direction == SplitDirection.Horizontal) {
					rect1.Width -= LayoutSplit.SPLIT_LEEWAY;
					rect2.Width -= LayoutSplit.SPLIT_LEEWAY;
					rect2.X += LayoutSplit.SPLIT_LEEWAY;
				} 
				else {
					rect1.Height -= LayoutSplit.SPLIT_LEEWAY;
					rect2.Height -= LayoutSplit.SPLIT_LEEWAY;
					rect2.Y += LayoutSplit.SPLIT_LEEWAY;
				}

				if (rect1.Contains(point))
				{
					return GetElementAt(split.Item1, point);
				}
				else if (rect2.Contains(point))
				{
					return GetElementAt(split.Item2, point);
				}

				return split;
			}
		}

		public void SplitAt(Point point, SplitDirection direction)
		{
			dynamic element = GetElementAt(point);
			if (element is LayoutSplit)
				return;

			element = element as LayoutWindow;

			var parent = element.Parent;
			element.Parent = null;

			var split = new LayoutSplit();
			split.Direction = direction;

			split.Item1 = element;
			split.Item2 = new LayoutWindow(element.RootScene.GetType());

			if (parent != null) {
				var sp = parent as LayoutSplit;
				if (sp.Item1 == element)
					sp.Item1 = split;
				if (sp.Item2 == element)
					sp.Item2 = split;
			}

			split.BoundingRect = element.BoundingRect;
			if (element == layout) {
				layout = split;
			}
			layout.FinalizeSize();
		}

		private IEnumerable<LayoutWindow> GetFromSplit(LayoutSplit split)
		{
			if (split.Item1 is LayoutSplit)
				foreach (var item in GetFromSplit(split.Item1 as LayoutSplit))
					yield return item;
			else
				yield return split.Item1 as LayoutWindow;

			if (split.Item2 is LayoutSplit)
				foreach (var item in GetFromSplit(split.Item2 as LayoutSplit))
					yield return item;
			else
				yield return split.Item2 as LayoutWindow;
		}

		public IEnumerator<LayoutWindow> GetEnumerator()
		{
			if (layout is LayoutSplit)
				foreach (var item in GetFromSplit(layout as LayoutSplit))
					yield return item;
			else
				yield return layout as LayoutWindow;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
