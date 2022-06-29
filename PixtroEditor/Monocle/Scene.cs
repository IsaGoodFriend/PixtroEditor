using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Pixtro.Editor;
using Pixtro.UI;
using Pixtro.Scenes;

namespace Monocle {

	public class Scene : IEnumerable<Entity>, IEnumerable {
		public float TimeActive;
		public bool Focused { get; private set; }
		public EntityList Entities { get; private set; }
		public TagLists TagLists { get; private set; }
		public RendererList RendererList { get; private set; }
		public Entity HelperEntity { get; private set; }
		public Tracker Tracker { get; private set; }
		public Camera Camera { get; private set; }
		public SceneRenderer Renderer { get; private set; }
		public EditorLayout.LayoutWindow EditorLayout { get; internal set; }
		public Rectangle PreviousBounds { get; internal set; }
		public Rectangle VisualBounds => EditorLayout == null ? default : new Rectangle(EditorLayout.BoundingRect.X, EditorLayout.BoundingRect.Y + EditorWindow.SUB_MENU_BAR, EditorLayout.BoundingRect.Width, EditorLayout.BoundingRect.Height - EditorWindow.SUB_MENU_BAR);

		public event Action OnEndOfFrame;

		public event Action<int, int, bool> OnMouseDown, OnMouseDrag, OnMouseUp;

		public SceneBounds UIBounds { get; private set; }
		
		Dropdown OpenDropdown() {
			return new Dropdown(
				("Emulator", Change),
				("Level Editor", Change),
				("Console", Change),
				("Memory Viewer", Change),
				("File Viewer", Change)
			);
		}

		void Change(int index) {
			Point p = EditorLayout.BoundingRect.Location;

			Scene newScene;

			switch (index) {
				default:
				case 0:
					newScene = new EmulatorScene();
					break;
				case 1:
					newScene = new LevelEditorScene();
					break;
				case 2:
					newScene = new ConsoleScene();
					break;
				case 3:
					newScene = new MemoryPeekScene();
					break;
				case 4:
					newScene = new ProjectBrowserScene();
					break;
			}
			if (newScene == null)
				return;

			var item = Engine.Layout.GetElementAt(p + new Point(3)) as EditorLayout.LayoutWindow;

			item.ChangeRootScene(newScene);
		}

		public Scene(Image buttonImage) {
			Tracker = new Tracker();
			Entities = new EntityList(this);
			TagLists = new TagLists();
			RendererList = new RendererList(this);
			Camera = new Camera();

			HelperEntity = new Entity();
			Entities.Add(HelperEntity);

			Add(Renderer = new SceneRenderer(this));

			UIFramework.AddControl(UIBounds = new SceneBounds(this));
			var element = UIBounds.AddChild(new IconBarButton(buttonImage){
				OnClick = OpenDropdown
			});
			element.Transform.Offset.Y = -EditorWindow.SUB_MENU_BAR;
		}

		public void UpdateMouse(bool sceneNew) {

			if (OnMouseDown != null && MInput.Mouse.PressedLeftButton)
				OnMouseDown((int)MInput.Mouse.X, (int)MInput.Mouse.Y, sceneNew);
			if (OnMouseDrag != null && MInput.Mouse.CheckLeftButton && !MInput.Mouse.PressedLeftButton && MInput.Mouse.WasMoved)
				OnMouseDrag((int)MInput.Mouse.X, (int)MInput.Mouse.Y, sceneNew);
			if (OnMouseUp != null && MInput.Mouse.ReleasedLeftButton)
				OnMouseUp((int)MInput.Mouse.X, (int)MInput.Mouse.Y, sceneNew);
		}

		public virtual void OnSetWindow(EditorLayout.LayoutWindow window) { }

		public virtual void OnResize() { }

		public virtual void Begin() {
			foreach (var entity in Entities)
				entity.SceneBegin(this);
		}

		public virtual void End() {
			Focused = false;
			foreach (var entity in Entities)
				entity.SceneEnd(this);

			UIFramework.RemoveControl(UIBounds);
		}

		public virtual void BeforeUpdate() {
			TimeActive += Engine.DeltaTime;

			Entities.BeforeUpdate();

			UIBounds.Transform.Bounds = VisualBounds;
		}

		public virtual void Update() {
			RendererList.UpdateLists();
			Entities.UpdateLists();
			TagLists.UpdateLists();

			RendererList.Update();
		}

		public virtual void FocusedUpdate() {
			Entities.Update();
		}

		public virtual void AfterUpdate() {
			if (OnEndOfFrame != null) {
				OnEndOfFrame();
				OnEndOfFrame = null;
			}
		}

		public void PrepRendering() {
			var rect = EditorLayout.BoundingRect;
			Engine.Instance.GraphicsDevice.Viewport = new Viewport(rect.X, rect.Y, rect.Width, rect.Height);
		}

		public virtual void BeforeRender() {
			RendererList.BeforeRender();
		}

		public virtual void Render() {
			RendererList.Render();
		}

		public virtual void AfterRender() {
			RendererList.AfterRender();
		}

		public virtual void DrawGraphics() {

			Entities.Render();
		}

		public virtual void HandleGraphicsReset() {
			Entities.HandleGraphicsReset();
		}

		public virtual void HandleGraphicsCreate() {
			Entities.HandleGraphicsCreate();
		}

		public virtual void GainFocus() {

		}

		public virtual void LoseFocus() {

		}

		#region Interval

		/// <summary>
		/// Returns whether the Scene timer has passed the given time interval since the last frame. Ex: given 2.0f, this will return true once every 2 seconds
		/// </summary>
		/// <param name="interval">The time interval to check for</param>
		/// <returns></returns>
		public bool OnInterval(float interval) {
			return (int)((TimeActive - Engine.DeltaTime) / interval) < (int)(TimeActive / interval);
		}

		/// <summary>
		/// Returns whether the Scene timer has passed the given time interval since the last frame. Ex: given 2.0f, this will return true once every 2 seconds
		/// </summary>
		/// <param name="interval">The time interval to check for</param>
		/// <returns></returns>
		public bool OnInterval(float interval, float offset) {
			return Math.Floor((TimeActive - offset - Engine.DeltaTime) / interval) < Math.Floor((TimeActive - offset) / interval);
		}

		public bool BetweenInterval(float interval) {
			return Calc.BetweenInterval(TimeActive, interval);
		}

		#endregion

		#region Utils

		internal void SetActualDepth(Entity entity) {
			//Mark lists unsorted
			Entities.MarkUnsorted();
			for (int i = 0; i < BitTag.TotalTags; i++)
				if (entity.TagCheck(1 << i))
					TagLists.MarkUnsorted(i);
		}

		#endregion

		#region Entity Shortcuts

		/// <summary>
		/// Shortcut to call Engine.Pooler.Create, add the Entity to this Scene, and return it. Entity type must be marked as Pooled
		/// </summary>
		/// <typeparam name="T">Pooled Entity type to create</typeparam>
		/// <returns></returns>
		public T CreateAndAdd<T>() where T : Entity, new() {
			var entity = Engine.Pooler.Create<T>();
			Add(entity);
			return entity;
		}

		/// <summary>
		/// Quick access to entire tag lists of Entities. Result will never be null
		/// </summary>
		/// <param name="tag">The tag list to fetch</param>
		/// <returns></returns>
		public List<Entity> this[BitTag tag] {
			get {
				return TagLists[tag.ID];
			}
		}

		/// <summary>
		/// Shortcut function for adding an Entity to the Scene's Entities list
		/// </summary>
		/// <param name="entity">The Entity to add</param>
		public void Add(Entity entity) {
			Entities.Add(entity);
		}

		/// <summary>
		/// Shortcut function for adding an Entity to the Scene's Entities list
		/// </summary>
		/// <param name="entity">The Entity to add</param>
		public void AddOld(Entity entity) {
			if (entity.Scene != this)
				entity.RemoveSelf();
			Entities.Add(entity);
		}

		/// <summary>
		/// Shortcut function for removing an Entity from the Scene's Entities list
		/// </summary>
		/// <param name="entity">The Entity to remove</param>
		public void Remove(Entity entity) {
			Entities.Remove(entity);
		}

		/// <summary>
		/// Shortcut function for adding a set of Entities from the Scene's Entities list
		/// </summary>
		/// <param name="entities">The Entities to add</param>
		public void Add(IEnumerable<Entity> entities) {
			Entities.Add(entities);
		}

		/// <summary>
		/// Shortcut function for removing a set of Entities from the Scene's Entities list
		/// </summary>
		/// <param name="entities">The Entities to remove</param>
		public void Remove(IEnumerable<Entity> entities) {
			Entities.Remove(entities);
		}

		/// <summary>
		/// Shortcut function for adding a set of Entities from the Scene's Entities list
		/// </summary>
		/// <param name="entities">The Entities to add</param>
		public void Add(params Entity[ ] entities) {
			Entities.Add(entities);
		}

		/// <summary>
		/// Shortcut function for removing a set of Entities from the Scene's Entities list
		/// </summary>
		/// <param name="entities">The Entities to remove</param>
		public void Remove(params Entity[ ] entities) {
			Entities.Remove(entities);
		}

		/// <summary>
		/// Allows you to iterate through all Entities in the Scene
		/// </summary>
		/// <returns></returns>
		public IEnumerator<Entity> GetEnumerator() {
			return Entities.GetEnumerator();
		}

		/// <summary>
		/// Allows you to iterate through all Entities in the Scene
		/// </summary>
		/// <returns></returns>
		IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		public List<Entity> GetEntitiesByTagMask(int mask) {
			List<Entity> list = new List<Entity>();
			foreach (var entity in Entities)
				if ((entity.Tag & mask) != 0)
					list.Add(entity);
			return list;
		}

		public List<Entity> GetEntitiesExcludingTagMask(int mask) {
			List<Entity> list = new List<Entity>();
			foreach (var entity in Entities)
				if ((entity.Tag & mask) == 0)
					list.Add(entity);
			return list;
		}

		#endregion

		#region Renderer Shortcuts

		/// <summary>
		/// Shortcut function to add a Renderer to the Renderer list
		/// </summary>
		/// <param name="renderer">The Renderer to add</param>
		public void Add(Renderer renderer) {
			RendererList.Add(renderer);
		}

		/// <summary>
		/// Shortcut function to remove a Renderer from the Renderer list
		/// </summary>
		/// <param name="renderer">The Renderer to remove</param>
		public void Remove(Renderer renderer) {
			RendererList.Remove(renderer);
		}

		#endregion
	}
}
