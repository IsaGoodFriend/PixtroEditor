using System;
using System.Collections;
using System.Collections.Generic;


namespace Monocle {
	public class StateMachine : Component {
		private int state;
		private List<Action> begins;
		private List<Func<int>> updates;
		private List<Action> ends;
		private List<Func<IEnumerator>> coroutines;
		private List<Dictionary<string, Action>> customActions;
		private Coroutine currentCoroutine;

		public bool ChangedStates;
		public bool Log;
		public int PreviousState { get; private set; }
		public bool Locked;

		public StateMachine(int maxStates = 10)
			: base(true, false) {
			PreviousState = state = -1;

			begins = new List<Action>(maxStates);
			begins.AddRange(new Action[maxStates]);
			updates = new List<Func<int>>(maxStates);
			updates.AddRange(new Func<int>[maxStates]);
			ends = new List<Action>(maxStates);
			ends.AddRange(new Action[maxStates]);
			coroutines = new List<Func<IEnumerator>>(maxStates);
			coroutines.AddRange(new Func<IEnumerator>[maxStates]);
			customActions = new List<Dictionary<string, Action>>();

			for (int i = 0; i < maxStates; ++i) {
				customActions.Add(new Dictionary<string, Action>());
			}

			currentCoroutine = new Coroutine();
			currentCoroutine.RemoveOnComplete = false;
		}

		public override void Added(Entity entity) {
			base.Added(entity);

			if (state == -1)
				State = 0;
		}

		public int State {
			get { return state; }
			set {
#if DEBUG
				if (value >= updates.Count || value < 0)
					throw new Exception("StateMachine state out of range");
#endif

				if (!Locked && state != value) {
					if (Log)
						Calc.Log("Enter State " + value + " (leaving " + state + ")");

					ChangedStates = true;
					PreviousState = state;
					state = value;

					if (PreviousState != -1 && ends[PreviousState] != null) {
						if (Log)
							Calc.Log("Calling End " + PreviousState);
						ends[PreviousState]();
					}

					if (begins[state] != null) {
						if (Log)
							Calc.Log("Calling Begin " + state);
						begins[state]();
					}

					if (coroutines[state] != null) {
						if (Log)
							Calc.Log("Starting Coroutine " + state);
						currentCoroutine.Replace(coroutines[state]());
					}
					else
						currentCoroutine.Cancel();
				}
			}
		}

		public void ForceState(int toState) {
			if (state != toState)
				State = toState;
			else {
				if (Log)
					Calc.Log("Enter State " + toState + " (leaving " + state + ")");

				ChangedStates = true;
				PreviousState = state;
				state = toState;

				if (PreviousState != -1 && ends[PreviousState] != null) {
					if (Log)
						Calc.Log("Calling End " + state);
					ends[PreviousState]();
				}

				if (begins[state] != null) {
					if (Log)
						Calc.Log("Calling Begin " + state);
					begins[state]();
				}

				if (coroutines[state] != null) {
					if (Log)
						Calc.Log("Starting Coroutine " + state);
					currentCoroutine.Replace(coroutines[state]());
				}
				else
					currentCoroutine.Cancel();
			}
		}

		public void SetCallbacks(int state, Func<int> onUpdate, Func<IEnumerator> coroutine = null, Action begin = null, Action end = null) {
			if (updates.Count <= state) {
				throw new IndexOutOfRangeException("State index is out of range.  Use \"CreateNewCallback\" to add a new callback state");
			}

			updates[state] = onUpdate;
			begins[state] = begin;
			ends[state] = end;
			coroutines[state] = coroutine;
		}
		public int CreateNewCallback(Func<int> onUpdate, Func<IEnumerator> coroutine = null, Action begin = null, Action end = null) {
			int retVal = updates.Count;

			updates.Add(onUpdate);
			begins.Add(begin);
			ends.Add(end);
			coroutines.Add(coroutine);
			customActions.Add(new Dictionary<string, Action>());

			return retVal;
		}

		public void ReflectState(Entity from, int index, string name) {
			updates[index] = (Func<int>)Calc.GetMethod<Func<int>>(from, name + "Update");
			begins[index] = (Action)Calc.GetMethod<Action>(from, name + "Begin");
			ends[index] = (Action)Calc.GetMethod<Action>(from, name + "End");
			coroutines[index] = (Func<IEnumerator>)Calc.GetMethod<Func<IEnumerator>>(from, name + "Coroutine");
		}

		public void AddCustomAction(string actionName, int state, Action action) {
			if (state == -1) {
				for (int i = 0; i < customActions.Count; ++i) {
					AddCustomAction(actionName, i, action);
				}
			}
			else {
				if (!customActions[state].ContainsKey(actionName))
					customActions[state].Add(actionName, action);
			}
		}
		public void ClearCustomAction(string actionName, int state) {
			if (state == -1) {
				for (int i = 0; i < customActions.Count; ++i) {
					ClearCustomAction(actionName, i);
				}
			}
			else {
				if (customActions[state].ContainsKey(actionName))
					customActions[state].Remove(actionName);
			}
		}
		public bool RunCustomAction(string actionName) {
			if (customActions[state].ContainsKey(actionName)) {
				customActions[state][actionName]();
				return true;
			}
			else return false;
		}

		public override void Update() {
			ChangedStates = false;

			if (updates[state] != null) {
				int st = updates[state]();
				if (st >= 0)
					State = st;
			}
			if (currentCoroutine.Active) {
				currentCoroutine.Update();
				if (!ChangedStates && Log && currentCoroutine.Finished)
					Calc.Log("Finished Coroutine " + state);
			}
		}

		public static implicit operator int(StateMachine s) {
			return s.state;
		}

		public void LogAllStates() {
			for (int i = 0; i < updates.Count; i++)
				LogState(i);
		}

		public void LogState(int index) {
			Calc.Log("State " + index + ": "
				+ (updates[index] != null ? "U" : "")
				+ (begins[index] != null ? "B" : "")
				+ (ends[index] != null ? "E" : "")
				+ (coroutines[index] != null ? "C" : ""));
		}
	}
}
