#if WINDOWS
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Pixtro {
    public static class MessageBox {
        public enum BoxType {
            TYPE_ABORT_TRY_IGNORE = 2, TYPE_CANCEL_TRY_CONTINUE = 6, TYPE_HELP = 0x4000, TYPE_OK = 0, TYPE_OK_CANCEL = 1, TYPE_RETRY_CANCEL = 4, TYPE_YES_NO = 5, TYPE_YES_NO_CANCEL = 3
        }

        public enum Icon {
            ICON_WARN = 0x00000030, ICON_QUESTION = 0x00000020, ICON_ERROR = 0x00000010
        }

        public enum Button {
            BUTTON_ABORT = 3, BUTTON_CANCEL = 2, BUTTON_CONTINUE = 11, BUTTON_IGNORE = 5, BUTTON_NO = 7, BUTTON_OK = 1, BUTTON_RETRY = 4, BUTTON_TRYAGAIN = 10, BUTTON_YES = 6
        }

        [DllImport("user32.dll")]
        private extern static IntPtr GetActiveWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private extern static int MessageBoxA(IntPtr hWnd, [In, MarshalAs(UnmanagedType.LPStr)] string lpText, [In, MarshalAs(UnmanagedType.LPStr)] string lpCaption, uint uType);

        public static Button ShowMessageBox(string caption, string text, BoxType type, Icon icon) {
            int res = MessageBoxA(GetActiveWindow(), caption, text, (uint) type | (uint) icon);
            if (res == 0)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            return (Button)res;
        }
    }
}
#endif