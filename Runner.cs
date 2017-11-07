using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWars2017.DevKit.CSharpCgdk {
    public sealed class Runner {
        private readonly RemoteProcessClient remoteProcessClient;
        private readonly string token;

        public static void Main(string[] args) {
            //var appSettings = ConfigurationManager.AppSettings;
            //var localRunnerPath = appSettings["LocalRunnerPath"];
            //Process.Start("CMD.exe", "/c cd D:\\Downloads\\Chrome\\local-runner-ru && D: && local-runner.bat");
            //return;

            new Runner(args.Length == 3 ? args : new[] {"127.0.0.1", "31001", "0000000000000000"}).Run();
        }

        private Runner(IReadOnlyList<string> args) {
            remoteProcessClient = new RemoteProcessClient(args[0], int.Parse(args[1]));
            token = args[2];
        }

        public void Run() {
            try {
                remoteProcessClient.WriteTokenMessage(token);
                remoteProcessClient.WriteProtocolVersionMessage();
                remoteProcessClient.ReadTeamSizeMessage();
                Game game = remoteProcessClient.ReadGameContextMessage();

                IStrategy strategy = new MyStrategy();

                PlayerContext playerContext;

                try
                {
                    while ((playerContext = remoteProcessClient.ReadPlayerContextMessage()) != null)
                    {
                        Player player = playerContext.Player;
                        if (player == null)
                        {
                            break;
                        }

                        Move move = new Move();
                        strategy.Move(player, playerContext.World, game, move);

                        remoteProcessClient.WriteMoveMessage(move);
                    }
                }
                catch
                {
                    System.Console.WriteLine("Потеря соединения... Выход через 3 сек");
                    System.Threading.Thread.Sleep(3000);
                }
            } finally {
                remoteProcessClient.Close();
            }
        }
    }
}