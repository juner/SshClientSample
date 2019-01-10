using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Renci.SshNet.Async {
    public static class ShellStreamExtensions {
        public static Task<string> ExpectOnceAsync(this ShellStream Self, Regex Expect, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
            var Task = new TaskCompletionSource<string>();
            IDisposable Disposable = null;
            IAsyncResult result = null;
            Disposable = Token.Register(async () => {
                try {
                    if (result != null) {
                        var _result = result;
                        result = null;
                        var expect = await System.Threading.Tasks.Task.Run(() => Self.EndExpect(_result));
                    }
                } catch { }
                Task.TrySetCanceled(Token);
            });
            result = Self.BeginExpect(async ar => {
                try {
                    if (result != null) {
                        result = null;
                        var expect = await System.Threading.Tasks.Task.Run(() => Self.EndExpect(ar));
                        Task.TrySetResult(expect);
                    }

                } catch (Exception e) {
                    Task.TrySetException(e);
                } finally {
                    Disposable?.Dispose();
                    Disposable = null;
                }
            }, new ExpectAction(Expect, async v => {
                try {
                    if (result != null) {
                        var _result = result;
                        result = null;
                        var expect = await System.Threading.Tasks.Task.Run(() => Self.EndExpect(_result));
                        Task.TrySetResult(expect);
                    }

                } catch (Exception e) {
                    Task.TrySetException(e);
                } finally {
                    Disposable?.Dispose();
                    Disposable = null;
                }
            }));
            return Task.Task;
        }
    }
}
