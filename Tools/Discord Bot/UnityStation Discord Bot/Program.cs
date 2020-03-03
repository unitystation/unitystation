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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UnityStation_Discord_Bot
{
	class Program
	{
		private IConfiguration configuration;
		private DiscordSocketClient client;
		private Config config;

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
			config = configuration.Get<Config>();

			client = new DiscordSocketClient();
			client.Log += Log;
			client.MessageReceived += MessageReceived;

			await client.LoginAsync(TokenType.Bot, config.SecretKey);
			await client.StartAsync();

			// Block this task until the program is closed.
			await Task.Delay(-1);
		}

		private async Task<bool> CheckAdmin(SocketMessage message)
		{
			if (config.Admins.Exists(p => p.Name.Equals($"{message.Author.Username}#{message.Author.Discriminator}", StringComparison.InvariantCultureIgnoreCase)))
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
					if (!await CheckAdmin(message))
						return;
					await Help(message);
					break;
				case "!ping":
					if (!await CheckAdmin(message))
						return;
					await message.Channel.SendMessageAsync("Pong!");
					break;
				case "!serverlist":
					if (!await CheckAdmin(message))
						return;
					await ServerList(message);
					break;
				case "!hardreset":
					if (!await CheckAdmin(message))
						return;
					await HardReset(message);
					break;
				case "!update":
					if (!await CheckAdmin(message))
						return;
					await Update(message);
					break;
				case "!reboot":
					if (!await CheckAdmin(message))
						return;
					await Reboot(message);
					break;
				case "!gameban":
					if (!await CheckAdmin(message))
						return;
					await GameBan(message);
					break;
				case "!gameadmin":
					if (!await CheckAdmin(message))
						return;
					await GameAdmin(message);
					break;
				case "!ufw":
					if (!await CheckAdmin(message))
						return;
					await Ufw(message);
					break;
				case "!botadmin":
					if (!await CheckAdmin(message))
						return;
					await BotAdmin(message);
					break;
			}
		}

		private static async Task Help(SocketMessage message)
		{
			string commandList = ">>> Implemented commands:\n";
			commandList += "!help\n!ping\n!serverlist\n!hardreset\n!update\n!reboot\n!gameban\n!gameadmin\n!ufw\n!botadmin";
			await message.Channel.SendMessageAsync(commandList);
		}

		private async Task HardReset(SocketMessage message)
		{
			List<string> commandParams = message.Content.Split(" ").ToList();
			if (commandParams.Count != 2)
			{
				await message.Channel.SendMessageAsync($"Usage: !hardreset servername (ex.: USA01 or GER01)");
			}
			else
			{
				ServerConnection serverConnection = config.ServersConnections.FirstOrDefault(s => s.ServerName == commandParams[1]);
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
			List<string> commandParams = message.Content.Split(" ").ToList();
			if (commandParams.Count != 2)
			{
				await message.Channel.SendMessageAsync($"Usage: !update servername (ex.: USA01 or GER01)");
			}
			else
			{
				ServerConnection serverConnection = config.ServersConnections.FirstOrDefault(s => s.ServerName == commandParams[1]);
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
			List<string> commandParams = message.Content.Split(" ").ToList();
			if (commandParams.Count != 2)
			{
				await message.Channel.SendMessageAsync($"Usage: !reboot servername (ex.: USA01 or GER01)");
			}
			else
			{
				ServerConnection serverConnection = config.ServersConnections.FirstOrDefault(s => s.ServerName == commandParams[1]);
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

			ServerConnection serverConnection = config.ServersConnections.FirstOrDefault(s => s.ServerName == commandParams[1]);
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
				await message.Channel.SendMessageAsync($"Getting ban details");
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
			await message.Channel.SendMessageAsync("This command is coming soon!");

			//FirebaseAdmin.Auth.
		}

		private async Task Ufw(SocketMessage message)
		{
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

			foreach (ServerConnection serverConnection in config.ServersConnections)
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

		private async Task BotAdmin(SocketMessage message)
		{
			List<string> allowedVerbs = new List<string>() { "list", "add", "revoke" };

			// Split the string preserving the double-quotes as a single argument
			List<string> commandParams = Regex.Matches(message.Content, @"[\""].+?[\""]|[^ ]+")
											  .Cast<Match>()
											  .Select(m => m.Value)
											  .ToList();

			if (commandParams.Count < 2 || commandParams.Count > 3 || !allowedVerbs.Contains(commandParams[1]))
			{
				await message.Channel.SendMessageAsync($"Usage: !botadmin list|add|revoke [username#1234]\nNote: since usernames may contain spaces, you may use \" to enclose it.");
				return;
			}

			if (commandParams[1] != "list" && commandParams.Count < 3)
			{
				await message.Channel.SendMessageAsync($"Usage: !botadmin add|revoke [username#1234]\nNote: since usernames may contain spaces, you may use \" to enclose it.");
				return;
			}

			switch (commandParams[1])
			{
				case "list":
					await BotAdminList(message);
					break;
				case "add":
					await BotAdminAdd(message, commandParams);
					break;
				case "revoke":
					await BotAdminRevoke(message, commandParams);
					break;
			}
		}

		private async Task BotAdminList(SocketMessage message)
		{
			string adminList = ">>> BOT Admins are: \n";
			foreach (Admin admin in config.Admins)
			{
				adminList += $"{admin.Name}\n";
			}
			await message.Channel.SendMessageAsync(adminList);
		}

		private async Task BotAdminAdd(SocketMessage message, List<string> commandParams)
		{
			string userName = commandParams[2].Trim('"');

			if (!userName.Contains("#"))
			{
				await message.Channel.SendMessageAsync("Discord user name should be in the Name#1234 format");
				return;
			}

			if (config.Admins.Exists(p => p.Name == userName))
			{
				await message.Channel.SendMessageAsync($"User {commandParams[2]} is already a bot admin");
				return;
			}

			config.Admins.Add(new Admin() { Name = userName });
			string configJson = JsonSerializer.Serialize(config);
			using (StreamWriter streamWriter = new StreamWriter(new FileStream("config.json", FileMode.Create)))
			{
				streamWriter.Write(configJson);
			}
			await message.Channel.SendMessageAsync($"User {commandParams[2]} was added to the bot admins");
		}

		private async Task BotAdminRevoke(SocketMessage message, List<string> commandParams)
		{
			string userName = commandParams[2].Trim('"');

			if (!userName.Contains("#"))
			{
				await message.Channel.SendMessageAsync("Discord user name should be in the Name#1234 format");
				return;
			}

			if (!config.Admins.Exists(p => p.Name == userName))
			{
				await message.Channel.SendMessageAsync($"User {commandParams[2]} is not a bot admin");
				return;
			}

			config.Admins.Remove(config.Admins.Find(p => p.Name == userName));

			string configJson = JsonSerializer.Serialize(config);
			using (StreamWriter streamWriter = new StreamWriter(new FileStream("config.json", FileMode.Create)))
			{
				streamWriter.Write(configJson);
			}
			await message.Channel.SendMessageAsync($"User {commandParams[2]} was removed from bot admins");
		}

	}
}