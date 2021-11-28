using System.Collections;
using System.Collections.Generic;

namespace Monocle {
	public class Coroutine : Component {
		public bool Finished { get; private set; }
		public bool RemoveOnComplete = true;
		public bool UseRawDeltaTime = false;
		public bool RunNextItem = false;

		private Stack<IEnumerator> enumerators;
		private float waitTimer;
		private bool ended;

		public Coroutine(IEnumerator functionCall, bool removeOnComplete = true)
			: base(true, false) {
			enumerators = new Stack<IEnumerator>();
			enumerators.Push(functionCall);
			RemoveOnComplete = removeOnComplete;
		}

		public Coroutine(bool removeOnComplete = true)
			: base(false, false) {
			RemoveOnComplete = removeOnComplete;
			enumerators = new Stack<IEnumerator>();
		}

		public override void Update() {
			ended = false;

			do {
				RunNextItem = false;

				if (waitTimer > 0) {
					waitTimer -= Engine.DeltaTime;
					break;
				}
				else if (enumerators.Count > 0) {
					IEnumerator now = enumerators.Peek();
					if (now.MoveNext() && !ended) {
						if (now.Current is int)
							waitTimer = (int)now.Current;
						if (now.Current is float)
							waitTimer = (float)now.Current;
						else if (now.Current is IEnumerator) {
							RunNextItem = true;
							enumerators.Push(now.Current as IEnumerator);
						}
					}
					else if (!ended) {
						enumerators.Pop();
						if (enumerators.Count == 0) {
							Finished = true;
							Active = false;
							if (RemoveOnComplete)
								RemoveSelf();
						}
						RunNextItem = true;
					}
				}
			}
			while (RunNextItem);
		}

		public void Cancel() {
			Active = false;
			Finished = true;
			waitTimer = 0;
			enumerators.Clear();

			ended = true;
		}

		public void Replace(IEnumerator functionCall) {
			Active = true;
			Finished = false;
			waitTimer = 0;
			enumerators.Clear();
			enumerators.Push(functionCall);

			ended = true;
		}
	}
}
