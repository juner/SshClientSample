using System;
using System.Diagnostics;
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
            var TokenSource = new CancellationTokenSource();
            var app = new CommandLineApplication(throwOnUnexpectedArg: false) {
                Name = nameof(SshClientConsole),
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
                            var stream = client.CreateShellStream(string.Empty, 0, 0, 0, 0, 1024);

                            var ExpectTask = stream.ExpectOnceAsync(end);
                            do {
                                if (stream.DataAvailable) {
                                    var streamLength = stream.Length;
                                    var length = stream.Length > int.MaxValue || (int)streamLength > buffer.Length ? buffer.Length : (int)streamLength;
                                    var mem = buffer.Slice(0, length);
                                    length = await stream.ReadAsync(mem, Token);
                                    Trace.Write(Encoding.GetString(mem.Slice(0, length).Span));
                                } else {
                                    await Task.Delay(ConsoleWait);
                                }
                                if (ExpectTask.IsCompleted && !stream.DataAvailable)
                                    break;
                            } while (true);
                            Trace.Write(await ExpectTask);
                            var line = string.Empty;
                            do {
                                try {

                                    line = Console.ReadLine();
                                    stream.WriteLine(line);
                                    ExpectTask = stream.ExpectOnceAsync(end);
                                    do {
                                        if (stream.DataAvailable) {
                                            var streamLength = stream.Length;
                                            var length = stream.Length > int.MaxValue || (int)streamLength > buffer.Length ? buffer.Length : (int)streamLength;
                                            var mem = buffer.Slice(0, length);
                                            length = await stream.ReadAsync(mem, Token);
                                            Trace.Write(Encoding.GetString(mem.Slice(0, length).Span));
                                        } else {
                                            await Task.Delay(ConsoleWait);
                                        }
                                        if (ExpectTask.IsCompleted && !stream.DataAvailable)
                                            break;
                                    } while (true);
                                    Trace.Write(await ExpectTask);
                                } catch (Exception e) {
                                    Trace.WriteLine(e);
                                }
                            } while (line != string.Empty);
                            if (client.IsConnected)
                                client.Disconnect();
                        }
                    }
                    return 0;
                } catch (Exception e) {
                    Trace.WriteLine(e);
                    return e.HResult;
                }
            });
            app.Execute(args);
        }
    }
}
