using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.CommandLineUtils;

namespace SshClientConsole.NetCore {
    class Program {
        static void Main(string[] args)
        {
            using (var TokenSource = new CancellationTokenSource())
            using (SshClient.Console.SetConsoleCtrlHandler(ctrl => {
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
                var EndPatternOption = app.Option("-end|--endpattern <endpattern>", "End Pattern", CommandOptionType.SingleValue);
                app.Command("auto", Command => {
                    var AutoCommandsPathArgument = Command.Argument("AutoCommandsPath", "AutoCommandPath");
                    Command.Description = "自動実行する";
                    Command.HelpOption("-?|-h|--help");
                    Command.OnExecute(async () => {
                        using (var writer = new TextWriterTraceListener(Console.Out)) {
                            try {
                                Trace.Listeners.Add(writer);
                                var UserName = UserNameOption.HasValue() && !string.IsNullOrEmpty(UserNameOption.Value()) ? UserNameOption.Value() : throw new ArgumentNullException(UserNameOption.ValueName);
                                var HostName = HostNameOption.Value() ?? throw new ArgumentNullException(HostNameOption.ValueName);
                                var Password = PasswordOption.HasValue() ? PasswordOption.Value() : throw new ArgumentNullException(PasswordOption.ValueName);
                                var EndPattern = EndPatternOption.HasValue() ? EndPatternOption.Value() : SshClient.Console.DEFAULT_ENDPATTERN;
                                var AutoCommandsPath = AutoCommandsPathArgument.Value ?? throw new ArgumentNullException(AutoCommandsPathArgument.Name);
                                var Commands = File.ReadAllLines(AutoCommandsPath, new UTF8Encoding(false));
                                await new SshClient.AutoConsole(HostName, UserName, Password, Encoding.UTF8, new Regex(EndPattern, RegexOptions.Multiline), Commands).StartAsync(TokenSource.Token);
                                return 0;
                            } catch (OperationCanceledException) {
                                return 0;
                            } catch (Exception e) {
                                Trace.WriteLine(e.Message);
                                return e.HResult;
                            }
                        }
                    });
                });
                app.OnExecute(async () => {
                    using (var writer = new TextWriterTraceListener(Console.Out)) {
                        try {
                            Trace.Listeners.Add(writer);
                            var UserName = UserNameOption.HasValue() ? UserNameOption.Value() : throw new ArgumentNullException(UserNameOption.ValueName);
                            Trace.WriteLine($"{nameof(UserName)}: {UserName}");
                            var HostName = HostNameOption.Value() ?? throw new ArgumentNullException(HostNameOption.ValueName);
                            Trace.WriteLine($"{nameof(HostName)}: {HostName}");
                            var Password = PasswordOption.HasValue() ? PasswordOption.Value() : null;
                            while (string.IsNullOrEmpty(Password)) {
                                Trace.Write("Password: ");
                                Password = await SshClient.Console.ReadLineAsync(TokenSource.Token);
                            }
                            var EndPattern = EndPatternOption.HasValue() ? EndPatternOption.Value() : SshClient.Console.DEFAULT_ENDPATTERN;
                            Trace.WriteLine($"{nameof(EndPattern)}: {EndPattern}");
                            await new SshClient.Console(HostName, UserName, Password, Encoding.UTF8, new Regex(EndPattern, RegexOptions.Multiline)).StartAsync(TokenSource.Token);
                            return 0;
                        } catch (OperationCanceledException) {
                            return 0;
                        } catch (Exception e) {
                            Trace.WriteLine(e.Message);
                            return e.HResult;
                        }
                    }
                });
                app.Execute(args);
            }
        }
    }
}
