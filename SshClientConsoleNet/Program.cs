using System.Threading;
using Microsoft.Extensions.CommandLineUtils;

namespace SshClientConsole.NetFramework {
    class Program {
        const string DEFAULT_ENDPATTERN = @"[$#>:][ ]$";
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
                var Console = new SshClient.Console {
                    UserNameOption = UserNameOption,
                    PasswordOption = PasswordOption,
                    HostNameOption = HostNameOption,
                    EndPatternOption = EndPatternOption,
                };
                app.OnExecute(() => Console.StartAsync(TokenSource.Token));
                app.Execute(args);
            }
        }
    }
}
