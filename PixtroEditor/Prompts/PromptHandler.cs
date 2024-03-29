﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Pixtro {
	public static class PromptHandler {

		public static string AskForSaving(string warningText) {
			var response = MessageBox.ShowMessageBox(warningText, "Pixtro", MessageBox.BoxType.TYPE_YES_NO_CANCEL, MessageBox.Icon.ICON_WARN);

			if (response == MessageBox.Button.BUTTON_YES)
				return "saveContinue";
			else if (response == MessageBox.Button.BUTTON_NO)
				return "continue";
			else
				return "cancel";
		}
		public static bool AskConfirmation(string warningText) {
			var response = MessageBox.ShowMessageBox(warningText, "Pixtro", MessageBox.BoxType.TYPE_OK_CANCEL, MessageBox.Icon.ICON_WARN);

			if (response == MessageBox.Button.BUTTON_OK || response == MessageBox.Button.BUTTON_YES)
				return true;
			else
				return false;
		}
		public static void RunTest() {
		}
	}
}
