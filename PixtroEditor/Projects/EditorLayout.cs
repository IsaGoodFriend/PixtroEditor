using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;

namespace Pixtro.Editor
{
	public sealed class EditorLayout : IEnumerable<EditorLayout.LayoutWindow>
	{
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
			Size MinimumSize();
			void ResizeWindow(Rectangle bound);
			void FinalizeSize();
		}
		public class LayoutSplit : ILayoutInfo
		{
			public const int SPLIT_PIXEL_SIZE = 2;

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

			public Size MinimumSize()
			{
				int width = 0, height;

				var p1 = Item1.MinimumSize();
				var p2 = Item2.MinimumSize();

				// Swap coordinates temporarily if splitting vertical just to reuse code to check sizes
				if (Direction == SplitDirection.Vertical)
				{
					int temp = p1.Width;
					p1.Width = p1.Height;
					p1.Height = temp;

					temp = p2.Width;
					p2.Width = p2.Height;
					p2.Height = temp;
				}

				if (p1.Width > 0 && p2.Width > 0)
					width = p1.Width + p2.Width;
				else if (p1.Width > 0)
					width = p1.Width;
				else if (p2.Width > 0)
					width = p2.Width;

				width += SPLIT_PIXEL_SIZE;

				height = Math.Max(p1.Height, p2.Height);

				if (Direction == SplitDirection.Vertical)
					return new Size(height, width);
				else
					return new Size(width, height);
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
					if (rect1.Width < size1.Width)
					{
						rect2.Width -= size1.Width - rect1.Width;
						rect1.Width = size1.Width;
					}
					else if (rect2.Width < size2.Width)
					{
						rect1.Width -= size2.Width - rect2.Width;
						rect2.Width = size2.Width;
					}
					rect2.X = rect1.Right + SPLIT_PIXEL_SIZE;
				}
				else
				{
					if (rect1.Height < size1.Height)
					{
						rect2.Height -= size1.Height - rect1.Height;
						rect1.Height = size1.Height;
					}
					else if (rect2.Height < size2.Height)
					{
						rect1.Height -= size2.Height - rect2.Height;
						rect2.Height = size2.Height;
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
				RootScene = scene;
				scene.EditorLayout = this;
				scene.OnSetWindow(this);
			}

			public Size MinimumSize()
			{
				return new Size(MinimumWidth, MinimumHeight);
			}
			public void ResizeWindow(Rectangle bound)
			{
				BoundingRect = bound;
			}
			public void FinalizeSize()
			{
			}

			public void ChangeRootScene(Scene newScene) {
				RootScene = newScene;
				newScene.EditorLayout = this;
				newScene.OnSetWindow(this);
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

			Engine.OnMouseDown += Engine_OnMouseDown;
			Engine.ResizeEnd += OnResize;
		}

		private void Engine_OnMouseDown(int x, int y, bool newScene) {
			var item = GetElementAt(new Point(x, y));

			if (item != null && item is LayoutSplit) {
				adjustingLayout = item as LayoutSplit;

				Engine.OnMouseDrag += MouseDrag;
				Engine.OnMouseUp += (x, y) => {
					Engine.OnMouseDrag -= MouseDrag;
				};
			}
		}

		private void MouseDrag(int x, int y) {
			float percent = adjustingLayout.Direction == SplitDirection.Horizontal ?
				(x - adjustingLayout.BoundingRect.X + 2) / (float) adjustingLayout.BoundingRect.Width :
				(y - adjustingLayout.BoundingRect.Y + 2) / (float) adjustingLayout.BoundingRect.Height;

			adjustingLayout.SplitPercent = percent;
		}

		private void OnResize(int width, int height) {

			layout.ResizeWindow(new Rectangle(0, EditorWindow.TOP_MENU_BAR, width, height - EditorWindow.HEIGHT_SUB));
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

				info = null;

				if (split.Item1.BoundingRect.Contains(point))
				{
					return GetElementAt(split.Item1, point);
				}
				else if (split.Item2.BoundingRect.Contains(point))
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
