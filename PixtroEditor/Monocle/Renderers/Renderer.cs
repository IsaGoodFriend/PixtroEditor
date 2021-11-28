using System.Collections;

namespace Monocle {
	public abstract class Renderer {
		public bool Visible = true;
		public static float FadeAmount;

		public static IEnumerator FadeOut() {
			for (float f = 0; f < 1; f = Calc.Approach(f, 1, Engine.DeltaTime)) {
				FadeAmount = f;
				yield return null;
			}
		}
		public static IEnumerator FadeIn() {
			for (float f = 1; f > 0; f = Calc.Approach(f, 0, Engine.DeltaTime)) {
				FadeAmount = f;
				yield return null;
			}
			FadeAmount = 0;
		}

		public virtual void Update(Scene scene) { }
		public virtual void BeforeRender(Scene scene) { }
		public virtual void Render(Scene scene) { }
		public virtual void AfterRender(Scene scene) { }
	}
}
