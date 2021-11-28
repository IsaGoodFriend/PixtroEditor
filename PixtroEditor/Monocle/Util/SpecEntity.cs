using Microsoft.Xna.Framework;

namespace Monocle {
	public class SpecEntity<T> : Entity where T : Scene {
		public T SpecScene { get; private set; }

		public SpecEntity(Vector2 position)
			: base(position) {

		}

		public SpecEntity()
			: base() {

		}

		public override bool Added(Scene scene) {
			if (Scene is T)
				SpecScene = Scene as T;
			return base.Added(scene);
		}

		public override void Removed(Scene scene) {
			SpecScene = null;
			base.Removed(scene);
		}
	}
}
