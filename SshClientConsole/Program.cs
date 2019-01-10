using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Renci.SshNet;
using Renci.SshNet.Async;

namespace SshClientConsole {
    class Program {
        static void Main(string[] args)
        {
            using (var TokenSource = new CancellationTokenSource())
            using (SetConsoleCtrlHandler(ctrl => {
                TokenSource.Cancel();
                return false;
            })) {
                var app = new CommandLineApplication(throwOnUnexpectedArg: false) {
                    Name = nameof(SshClientConsole),
                    FullName = "SSH.NET SshClient -> ShellStream Example",
                };
                app.HelpOption("-?|-h|--help");
                var UserNameOption = app.Option("-user|--username <username>", "User Name", CommandOptionType.SingleValue);
                var PasswordOption = app.Option("-p|--password <password>", "Password", CommandOptionType.SingleValue);
                var HostNameOption = app.Option("-host|--hostname <hostname>", "Host Name", CommandOptionType.SingleValue);
                app.OnExecute(async () => {
                    var Token = TokenSource.Token;
                    var UserName = UserNameOption.Value() ?? throw new ArgumentNullException(UserNameOption.ValueName);
                    var Password = PasswordOption.Value() ?? throw new ArgumentNullException(PasswordOption.ValueName);
                    var HostName = HostNameOption.Value() ?? throw new ArgumentNullException(HostNameOption.ValueName);
                    var end = new Regex(@"[$#>:][ ]$", RegexOptions.Multiline);
                    var ConsoleWait = TimeSpan.FromMilliseconds(100);
                    try {
                        using (var writer = new TextWriterTraceListener(Console.Out)) {
                            Trace.Listeners.Add(writer);
                            var AuthMethod = new PasswordAuthenticationMethod(UserName, Password);
                            var info = new ConnectionInfo(HostName, UserName, AuthMethod) {
                                Encoding = System.Text.Encoding.UTF8
                            };
                            var Encoding = info.Encoding;
                            using (var client = new SshClient(info)) {
                                client.Connect();
                                var buffer = new byte[1024].AsMemory();
                                var IsEnable = true;
                                using (var stream = client.CreateShellStream(string.Empty, 0, 0, 0, 0, 1024)) {
                                    IsEnable = await OutEndAsync(stream, end, ConsoleWait, buffer, Encoding, Token);
                                    var line = string.Empty;
                                    do {
                                        if (!IsEnable)
                                            break;
                                        try {
                                            line = Console.ReadLine();
                                            stream.WriteLine(line);
                                            IsEnable = await OutEndAsync(stream, end, ConsoleWait, buffer, Encoding, Token);
                                        } catch (Exception e) {
                                            Trace.WriteLine(e);
                                        }
                                    } while (IsEnable);
                                }
                            }
                        }
                        return 0;
                    } catch (TaskCanceledException e) {
                        Trace.WriteLine("Canceled.");
                        return e.HResult;
                    } catch (Exception e) {
                        Trace.WriteLine(e);
                        return e.HResult;
                    }
                });
                app.Execute(args);
            }
        }
        /// <summary>
        /// <see cref="Trace.Write"/>で出力しつつ <paramref name="end"/>に合致した文字列が出現して出力しきる迄待つ
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="end"></param>
        /// <param name="ConsoleWait"></param>
        /// <param name="buffer"></param>
        /// <param name="Encoding"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        static async Task<bool> OutEndAsync(ShellStream stream, Regex end, TimeSpan ConsoleWait, Memory<byte> buffer, Encoding Encoding, CancellationToken Token = default)
        {
            var ExpectTask = stream.ExpectOnceAsync(end, Token);
            do {
                if (stream.DataAvailable)
                    Trace.Write(await ReadAsync(stream, buffer, Encoding, Token));
                else
                    await Task.Delay(ConsoleWait);
                if (ExpectTask.IsCompleted && !stream.DataAvailable)
                    break;
                if (!ExpectTask.IsCompleted && !stream.DataAvailable) {
                    try {
                        await stream.FlushAsync(Token);
                    } catch (ObjectDisposedException) {
                        return false;
                    }
                }
            } while (!Token.IsCancellationRequested);
            await stream.FlushAsync(Token);
            Trace.Write(await ExpectTask);
            return true;
        }
        /// <summary>
        /// <paramref name="stream"/>を末尾まで文字列として読み込む
        /// </summary>
        /// <param name="stream">読み込み元</param>
        /// <param name="buffer">バッファ</param>
        /// <param name="Encoding">エンコード</param>
        /// <param name="Token">キャンセル用</param>
        /// <returns></returns>
        static async Task<string> ReadAsync(ShellStream stream, Memory<byte> buffer, Encoding Encoding, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
            var streamLength = stream.Length;
            var length = stream.Length > int.MaxValue || (int)streamLength > buffer.Length ? buffer.Length : (int)streamLength;
            var mem = buffer.Slice(0, length);
            length = await stream.ReadAsync(mem, Token);
            if (length == 0)
                return string.Empty;
            return Encoding.GetString(mem.Slice(0, length).Span);
        }
        static IDisposable SetConsoleCtrlHandler(Func<CtrlTypes, bool> Action)
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
        enum CtrlTypes {
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
