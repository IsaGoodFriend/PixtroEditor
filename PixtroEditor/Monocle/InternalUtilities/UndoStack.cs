using System;
using System.Collections.Generic;
using System.Text;

namespace Monocle {
	public struct UndoState {
		public Action<object> Undo, Redo;
		public object UndoValue, RedoValue;
	}
	public class UndoStack {
		private Stack<UndoState> undoStates;
		private Stack<UndoState> undoneStates;

		public bool Dirty => undoStates.Count != notDirtyCount;

		private int notDirtyCount = 0;

		public UndoStack() {
			undoStates = new Stack<UndoState>();
			undoneStates = new Stack<UndoState>();
		}

		public void Clear() {
			undoStates.Clear();
			undoneStates.Clear();
			notDirtyCount = 0;
		}

		public void SetNotDirty() {
			notDirtyCount = undoStates.Count;
		}

		public void Push(UndoState state) {
			if (undoneStates.Count > 0)
				undoneStates.Clear();

			undoStates.Push(state);
		}
		public void Undo() {
			if (undoStates.Count == 0)
				return;

			var item = undoStates.Pop();

			item.Undo(item.UndoValue);

			undoneStates.Push(item);
		}
		public void Redo() {
			if (undoneStates.Count == 0)
				return;

			var item = undoneStates.Pop();

			item.Redo(item.RedoValue);

			undoStates.Push(item);
		}
	}
}
