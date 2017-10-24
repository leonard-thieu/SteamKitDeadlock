using System;
using System.Threading.Tasks;
using SteamKit2;
using static SteamKit2.SteamUser;

namespace SteamKitDeadlock
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            if (args.Length != 2)
                throw new ArgumentException("Your Steam user name and password must be passed in as arguments.");

            var userName = args[0];
            var password = args[1];

            var steamClient = new SteamClient();
            var manager = new CallbackManager(steamClient);
            using (var client = new SteamClientAdapter(steamClient, manager))
            {
                await client.ConnectAsync();
                await client.LogOnAsync(new LogOnDetails
                {
                    Username = userName,
                    Password = password,
                });

                var appId = 247080U; // Crypt of the NecroDancer
                var leaderboardName = "HARDCORE";
                Console.WriteLine($"Start download {leaderboardName}");
                await client.GetSteamUserStats().FindLeaderboard(appId, leaderboardName);
                Console.WriteLine($"End download {leaderboardName}");
            }
        }
    }
}
