using System;
using System.Runtime.InteropServices;

namespace SshClient {
    public class SetConsoleCtrl {
        public static IDisposable SetHandler(Func<CtrlTypes, bool> Action)
        {
            var HandlerRoutine = new NativeMethods.HandlerRoutine(Action);
            var result = NativeMethods.SetConsoleCtrlHandler(HandlerRoutine, true);
            if (!result)
                return null;
            return Disposable.Create(() => NativeMethods.SetConsoleCtrlHandler(HandlerRoutine, false));
        }
        static class NativeMethods {
            [DllImport("Kernel32")]
            public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);
            public delegate bool HandlerRoutine(CtrlTypes CtrlType);
        }
        public enum CtrlTypes {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }
        public class Disposable : IDisposable {
            readonly Action Action;
            Disposable(Action Action) => this.Action = Action;
            public static IDisposable Create(Action Action) => new Disposable(Action);
            private bool disposedValue = false;
            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue) {
                    if (disposing)
                        Action.Invoke();
                    disposedValue = true;
                }
            }
            public void Dispose() => Dispose(true);
        }
    }
}