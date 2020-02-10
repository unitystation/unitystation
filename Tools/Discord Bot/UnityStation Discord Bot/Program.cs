using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UnityStation_Discord_Bot
{
    class Program
    {
        private IConfiguration configuration;
        private DiscordSocketClient client;
        private List<Admin> admins;
        private List<ServerConnection> serverConnections;

        public static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task MainAsync()
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("Config.json");

            configuration = configurationBuilder.Build();

            admins = new List<Admin>();
            foreach (IConfigurationSection adminSection in configuration.GetSection("Admins").GetChildren())
            {
                admins.Add(adminSection.Get<Admin>());
            }

            serverConnections = new List<ServerConnection>();
            foreach (IConfigurationSection serverConnection in configuration.GetSection("ServersConnections").GetChildren())
            {
                serverConnections.Add(serverConnection.Get<ServerConnection>());
            }

            client = new DiscordSocketClient();
            client.Log += Log;
            client.MessageReceived += MessageReceived;

            await client.LoginAsync(TokenType.Bot, configuration.GetSection("SecretKey").Value);
            await client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private async Task<bool> CheckAdmin(SocketMessage message)
        {
            if (admins.Exists(p => p.Name.Equals($"{message.Author.Username}#{message.Author.Discriminator}", StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }
            else
            {
                await message.Channel.SendMessageAsync($"Insufficient privileges");
                return false;
            }
        }

        private async Task MessageReceived(SocketMessage message)
        {
            List<string> commandParams = message.Content.Split(" ").ToList();

            switch (commandParams[0])
            {
                case "!help":
                    await Help(message);
                    break;
                case "!ping":
                    await message.Channel.SendMessageAsync("Pong!");
                    break;
                case "!admins":
                    await AdminsList(message);
                    break;
                case "!serverlist":
                    await ServerList(message);
                    break;
                case "!hardreset":
                    await HardReset(message);
                    break;
                case "!update":
                    await Update(message);
                    break;
                case "!reboot":
                    await Reboot(message);
                    break;
                case "!gameban":
                    await GameBan(message);
                    break;
                case "!gameadmin":
                    await GameAdmin(message);
                    break;
                case "!ufw":
                    await Ufw(message);
                    break;
            }
        }

        private static async Task Help(SocketMessage message)
        {
            string commandList = ">>> Implemented commands:\n";
            commandList += "!help\n!ping\n!admins\n!serverlist\n!hardreset\n!update\n!reboot\n!gameban\n!gameadmin\n!ufw";
            await message.Channel.SendMessageAsync(commandList);
        }

        private async Task HardReset(SocketMessage message)
        {
            if (!await CheckAdmin(message))
                return;

            List<string> commandParams = message.Content.Split(" ").ToList();
            if (commandParams.Count != 2)
            {
                await message.Channel.SendMessageAsync($"Usage: !hardreset servername (ex.: USA01 or GER01)");
            }
            else
            {
                ServerConnection serverConnection = serverConnections.FirstOrDefault(s => s.ServerName == commandParams[1]);
                if (serverConnection == null)
                {
                    await message.Channel.SendMessageAsync($"Unknown server: {commandParams[1]}");
                }
                else
                {
                    await message.Channel.SendMessageAsync($"{message.Author.Username} asked for an hardreset for server {commandParams[1]}");

                    using (SshClient sshClient = new SshClient(serverConnection.Ip, serverConnection.Login, serverConnection.Password))
                    {
                        sshClient.Connect();
                        await message.Channel.SendMessageAsync($"Connection successful");
                        sshClient.CreateCommand("bash restart.sh").Execute();
                        await message.Channel.SendMessageAsync($"Restart command sent");
                        sshClient.Disconnect();
                    }
                }
            }
        }

        private async Task Update(SocketMessage message)
        {
            if (!await CheckAdmin(message))
                return;

            List<string> commandParams = message.Content.Split(" ").ToList();
            if (commandParams.Count != 2)
            {
                await message.Channel.SendMessageAsync($"Usage: !update servername (ex.: USA01 or GER01)");
            }
            else
            {
                ServerConnection serverConnection = serverConnections.FirstOrDefault(s => s.ServerName == commandParams[1]);
                if (serverConnection == null)
                {
                    await message.Channel.SendMessageAsync($"Unknown server: {commandParams[1]}");
                }
                else
                {
                    await message.Channel.SendMessageAsync($"{message.Author.Username} asked for an update for server {commandParams[1]}");

                    using (SshClient sshClient = new SshClient(serverConnection.Ip, serverConnection.Login, serverConnection.Password))
                    {
                        sshClient.Connect();
                        await message.Channel.SendMessageAsync($"Connection successful");
                        sshClient.CreateCommand("bash update.sh").Execute();
                        await message.Channel.SendMessageAsync($"Update command sent");
                        sshClient.Disconnect();
                    }
                }
            }
        }

        private async Task Reboot(SocketMessage message)
        {
            if (!await CheckAdmin(message))
                return;

            List<string> commandParams = message.Content.Split(" ").ToList();
            if (commandParams.Count != 2)
            {
                await message.Channel.SendMessageAsync($"Usage: !reboot servername (ex.: USA01 or GER01)");
            }
            else
            {
                ServerConnection serverConnection = serverConnections.FirstOrDefault(s => s.ServerName == commandParams[1]);
                if (serverConnection == null)
                {
                    await message.Channel.SendMessageAsync($"Unknown server: {commandParams[1]}");
                }
                else
                {
                    await message.Channel.SendMessageAsync($"{message.Author.Username} asked for a delete logs + reboot for server {commandParams[1]}");

                    using (SshClient sshClient = new SshClient(serverConnection.Ip, serverConnection.Login, serverConnection.Password))
                    {
                        sshClient.Connect();
                        await message.Channel.SendMessageAsync($"Connection successful");
                        sshClient.CreateCommand("rm server/serverlog.txt").Execute();
                        await message.Channel.SendMessageAsync($"Logs deleted");
                        await message.Channel.SendMessageAsync($"Rebooting");
                        try
                        {
                            sshClient.CreateCommand("reboot").Execute();
                        }
                        catch (SshConnectionException)
                        {
                            await Log(new LogMessage(LogSeverity.Info, "", "SSH connection lost after a reboot command"));
                            return;
                        }
                        sshClient.Disconnect();
                    }
                }
            }
        }

        private async Task AdminsList(SocketMessage message)
        {
            string adminList = ">>> Admins are: \n";
            foreach (Admin admin in admins)
            {
                adminList += $"{admin.Name}\n";
            }
            await message.Channel.SendMessageAsync(adminList);
        }

        private static async Task ServerList(SocketMessage message)
        {
            string contentString;

            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync("https://api.unitystation.org/serverlist");
                contentString = await response.Content.ReadAsStringAsync();
            }

            Servers servers = JsonSerializer.Deserialize<Servers>(contentString);

            string serverList = ">>> ";
            foreach (ServerInfo serverInfo in servers.servers)
            {
                serverList += $"{serverInfo.ServerName} {serverInfo.ForkName} Build:{serverInfo.BuildVersion} - Player Count: {serverInfo.PlayerCount}\n";
            }
            await message.Channel.SendMessageAsync(serverList);
        }

        private async Task GameBan(SocketMessage message)
        {
            if (!await CheckAdmin(message))
                return;

            List<string> allowedVerbs = new List<string>() { "list", "get", "add", "remove" };

            List<string> commandParams = message.Content.Split(" ").ToList();

            if (commandParams.Count < 3 || commandParams.Count > 4 || !allowedVerbs.Contains(commandParams[2]))
            {
                await message.Channel.SendMessageAsync($"Usage: !gameban servername (ex.: USA01 or GER01) list|get|add|remove");
                return;
            }

            if (commandParams[2] != "list" && commandParams.Count < 4)
            {
                await message.Channel.SendMessageAsync($"Usage: !gameban servername (ex.: USA01 or GER01) get|add|remove username");
                return;
            }

            ServerConnection serverConnection = serverConnections.FirstOrDefault(s => s.ServerName == commandParams[1]);
            if (serverConnection == null)
            {
                await message.Channel.SendMessageAsync($"Unknown server: {commandParams[1]}");
                return;
            }
            else
            {
                switch (commandParams[2])
                {
                    case "list":
                        await GameAdminList(message, serverConnection);
                        break;
                    case "get":
                        await GameAdminGet(message, serverConnection, commandParams[3]);
                        break;
                    case "add":
                        await message.Channel.SendMessageAsync($"This command is coming soon");
                        break;
                    case "remove":
                        await message.Channel.SendMessageAsync($"This command is coming soon");
                        break;
                }
            }
        }

        private static async Task GameAdminGet(SocketMessage message, ServerConnection serverConnection, string userName)
        {
            using (SshClient sshClient = new SshClient(serverConnection.Ip, serverConnection.Login, serverConnection.Password))
            {
                sshClient.Connect();
                await message.Channel.SendMessageAsync($"Connection successful");
                await message.Channel.SendMessageAsync($"Getting ban list");
                SshCommand cmd = sshClient.CreateCommand("cat ./server/Unitystation-Server_Data/StreamingAssets/admin/banlist.json");
                string cmdResult = cmd.Execute();
                sshClient.Disconnect();

                BanList banList = JsonSerializer.Deserialize<BanList>(cmdResult);
                BanEntry bannedUser = banList.banEntries.FirstOrDefault(p => p.userName == userName);

                if (bannedUser == null)
                {
                    await message.Channel.SendMessageAsync($"That user is not in the ban list");
                    return;
                }

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append($"**User name:** {bannedUser.userName}\n");
                stringBuilder.Append($"**User id:** {bannedUser.userId}\n");
                stringBuilder.Append($"**Date of ban:** {bannedUser.dateTimeOfBan}\n");
                stringBuilder.Append($"**Minutes:** {bannedUser.minutes}\n");
                stringBuilder.Append($"**Reason:** {bannedUser.reason}");

                await message.Channel.SendMessageAsync($">>> **Banned user:**\n{stringBuilder.ToString()}");
            }
        }

        private static async Task GameAdminList(SocketMessage message, ServerConnection serverConnection)
        {
            using (SshClient sshClient = new SshClient(serverConnection.Ip, serverConnection.Login, serverConnection.Password))
            {
                sshClient.Connect();
                await message.Channel.SendMessageAsync($"Connection successful");
                await message.Channel.SendMessageAsync($"Getting ban list");
                SshCommand cmd = sshClient.CreateCommand("cat ./server/Unitystation-Server_Data/StreamingAssets/admin/banlist.json");
                string cmdResult = cmd.Execute();
                sshClient.Disconnect();

                BanList banList = JsonSerializer.Deserialize<BanList>(cmdResult);

                StringBuilder stringBuilder = new StringBuilder();
                foreach (BanEntry banEntry in banList.banEntries)
                {
                    stringBuilder.Append($"{banEntry.userName}\n");
                }

                await message.Channel.SendMessageAsync($">>> **Banned users:**\n{stringBuilder.ToString()}");
                await message.Channel.SendMessageAsync($"Use **!gameban servername get username** to see details");
            }
        }

        private async Task GameAdmin(SocketMessage message)
        {
            if (!await CheckAdmin(message))
                return;

            await message.Channel.SendMessageAsync("This command is coming soon!");

            //FirebaseAdmin.Auth.
        }

        private async Task Ufw(SocketMessage message)
        {
            if (!await CheckAdmin(message))
                return;

            List<string> commandParams = message.Content.Split(" ").ToList();
            if (commandParams.Count != 3)
            {
                await message.Channel.SendMessageAsync($"Usage: !ufw deny ip");
                return;
            }

            if (commandParams[1] != "deny")
            {
                await message.Channel.SendMessageAsync($"Unknown verb. Usage: !ufw deny ip");
                return;
            }

            foreach (ServerConnection serverConnection in serverConnections)
            {
                using (SshClient sshClient = new SshClient(serverConnection.Ip, serverConnection.Login, serverConnection.Password))
                {
                    sshClient.Connect();
                    await message.Channel.SendMessageAsync($"Connection to {serverConnection.ServerName} successful");
                    await message.Channel.SendMessageAsync($"Adding {commandParams[2]} to deny rule list");
                    SshCommand cmd = sshClient.CreateCommand($"ufw insert 1 deny from {commandParams[2]} to any");
                    string cmdResult = cmd.Execute();
                    sshClient.Disconnect();
                }
            }
        }
    }
}

