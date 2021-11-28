using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Monocle {
	public class Entity : IEnumerable<Component>, IEnumerable {
		public const int MAX_GAME_UIDEPTH = 2048;

		public bool Active = true;
		public bool Visible = true;
		public Vector2 Position;

		public Scene Scene { get; private set; }
		public ComponentList Components { get; private set; }

		public bool UpdateWhileFrozen { get; set; }
		public bool UpdateAlways { get; set; }

		public int RenderDepth { get; set; }
		public int UIRenderDepth { get; set; }

		public string Name { get; private set; }

		private int tag;
		internal int updateDepth = 0;
		public uint UUID { get; private set; }

		public Entity(Vector2 _offset) {
			Position = _offset;
			Components = new ComponentList(this);
		}

		public Entity()
			: this(Vector2.Zero) {

		}

		/// <summary>
		/// Called when the containing Scene Begins
		/// </summary>
		public virtual void SceneBegin(Scene scene) {

		}

		/// <summary>
		/// Called when the containing Scene Ends
		/// </summary>
		public virtual void SceneEnd(Scene scene) {
			if (Components != null)
				foreach (var c in Components)
					c.SceneEnd(scene);
		}

		/// <summary>
		/// Called before the frame starts, after Entities are added and removed, on the frame that the Entity was added
		/// Useful if you added two Entities in the same frame, and need them to detect each other before they start Updating
		/// </summary>
		/// <param name="scene"></param>
		public virtual void Awake(Scene scene) {
			if (Components != null)
				foreach (var c in Components)
					c.EntityAwake();
		}

		/// <summary>
		/// Called when this Entity is added to a Scene, which only occurs immediately before each Update. 
		/// Keep in mind, other Entities to be added this frame may be added after this Entity. 
		/// See Awake() for after all Entities are added, but still before the frame Updates.
		/// </summary>
		/// <param name="scene"></param>
		public virtual bool Added(Scene scene) {
			Scene = scene;
			if (Components != null)
				foreach (var c in Components)
					c.EntityAdded(scene);
			//Scene.SetActualDepth(this);
			return true;
		}

		/// <summary>
		/// Called when the Entity is removed from a Scene
		/// </summary>
		/// <param name="scene"></param>
		public virtual void Removed(Scene scene) {
			if (Components != null)
				foreach (var c in Components)
					c.EntityRemoved(scene);
			if (scene == Scene)
				Scene = null;
		}

		public virtual void OnPlayerLeftScene() {

		}

		public virtual void OnPlayerEnterScene() {

		}

		/// <summary>
		/// Do game logic here, but do not render here. Not called if the Entity is not Active
		/// </summary>
		public virtual void Update() {
			Components.Update();
		}

		public virtual void BeforeUpdate() {

		}

		/// <summary>
		/// Draw the Entity here. Not called if the Entity is not Visible
		/// </summary>
		public virtual void Render() {
			Draw.Depth = RenderDepth;
			Components.Render();
		}

		/// <summary>
		/// Draw the Entity here. Not called if the Entity is not Visible
		/// </summary>
		public virtual void RenderLightmap() {
			Draw.Depth = RenderDepth + 1;
		}

		/// <summary>
		/// Draw the Entity's UI here. Not called if the Entity is not Visible
		/// </summary>
		public virtual void RenderUI() {
			Draw.Depth = UIRenderDepth;
			Components.RenderUI();
		}

		/// <summary>
		/// Draw any debug visuals here. Only called if the console is open, but still called even if the Entity is not Visible
		/// </summary>
		public virtual void DebugRender(Camera camera) {

			Components.DebugRender(camera);
		}

		/// <summary>
		/// Called when the graphics device resets. When this happens, any RenderTargets or other contents of VRAM will be wiped and need to be regenerated
		/// </summary>
		public virtual void HandleGraphicsReset() {
			Components.HandleGraphicsReset();
		}

		public virtual void SaveState(List<byte> _tempList) {
			_tempList.AddRange(BitConverter.GetBytes(Position.X));
			_tempList.AddRange(BitConverter.GetBytes(Position.Y));
		}

		public virtual void OnSave(bool _local) { }

		public virtual void OnReset(bool _local) { }

		public virtual void HandleGraphicsCreate() {
			Components.HandleGraphicsCreate();
		}

		public void RemoveSelf() {
			if (Scene != null)
				Scene.Entities.Remove(this);
		}

		public float X {
			get { return Position.X; }
			set { Position.X = value; }
		}

		public float Y {
			get { return Position.Y; }
			set { Position.Y = value; }
		}

		#region Tag

		public int Tag {
			get {
				return tag;
			}

			set {
				if (tag != value) {
					if (Scene != null) {
						for (int i = 0; i < Monocle.BitTag.TotalTags; i++) {
							int check = 1 << i;
							bool add = (value & check) != 0;
							bool has = (Tag & check) != 0;

							if (has != add) {
								if (add)
									Scene.TagLists[i].Add(this);
								else
									Scene.TagLists[i].Remove(this);
							}
						}
					}

					tag = value;
				}
			}
		}

		public bool TagFullCheck(int tag) {
			return (this.tag & tag) == tag;
		}

		public bool TagCheck(int tag) {
			return (this.tag & tag) != 0;
		}

		public void AddTag(int tag) {
			Tag |= tag;
		}

		public void RemoveTag(int tag) {
			Tag &= ~tag;
		}

		#endregion

		#region Components Shortcuts

		/// <summary>
		/// Shortcut function for adding a Component to the Entity's Components list
		/// </summary>
		/// <param name="component">The Component to add</param>
		public void Add(Component component) {
			Components.Add(component);
		}

		/// <summary>
		/// Shortcut function for removing an Component from the Entity's Components list
		/// </summary>
		/// <param name="component">The Component to remove</param>
		public void Remove(Component component) {
			Components.Remove(component);
		}

		/// <summary>
		/// Shortcut function for adding a set of Components from the Entity's Components list
		/// </summary>
		/// <param name="components">The Components to add</param>
		public void Add(params Component[] components) {
			Components.Add(components);
		}

		/// <summary>
		/// Shortcut function for removing a set of Components from the Entity's Components list
		/// </summary>
		/// <param name="components">The Components to remove</param>
		public void Remove(params Component[] components) {
			Components.Remove(components);
		}

		public T Get<T>() where T : Component {
			return Components.Get<T>();
		}

		/// <summary>
		/// Allows you to iterate through all Components in the Entity
		/// </summary>
		/// <returns></returns>
		public IEnumerator<Component> GetEnumerator() {
			return Components.GetEnumerator();
		}

		/// <summary>
		/// Allows you to iterate through all Components in the Entity
		/// </summary>
		/// <returns></returns>
		IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion

		#region Misc Utils

		public Entity Closest(params Entity[] entities) {
			Entity closest = entities[0];
			float dist = Vector2.DistanceSquared(Position, closest.Position);

			for (int i = 1; i < entities.Length; i++) {
				float current = Vector2.DistanceSquared(Position, entities[i].Position);
				if (current < dist) {
					closest = entities[i];
					dist = current;
				}
			}

			return closest;
		}

		public Entity Closest(BitTag tag) {
			var list = Scene[tag];
			Entity closest = null;
			float dist;

			if (list.Count >= 1) {
				closest = list[0];
				dist = Vector2.DistanceSquared(Position, closest.Position);

				for (int i = 1; i < list.Count; i++) {
					float current = Vector2.DistanceSquared(Position, list[i].Position);
					if (current < dist) {
						closest = list[i];
						dist = current;
					}
				}
			}

			return closest;
		}

		public T SceneAs<T>() where T : Scene {
			return Scene as T;
		}

		#endregion
	}
}
