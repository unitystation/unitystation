using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Logs;
using SecureStuff;
using Shared.Managers;
using Tomlyn;
using Tomlyn.Syntax;
using US13.Managers.NetworkManagement;

namespace US13.Systems.Permissions
{
	public class PermissionsManager : SingletonManager<PermissionsManager>
	{
		public readonly string ConfigPath = Path.Combine(AccessFile.AdminFolder, "permissions.toml");
		public PermissionsConfig Config { get; private set; } = new();
		[CanBeNull] private Rank DefinedAutoRank => Config.Ranks.GetValueOrDefault(Config.AutoRank);

		/// <summary>
		/// Tries to read the permissions config file and load it in memory. If for whatever reason it fails,
		/// there will be no permissions config and thus, no one will have any permissions.
		///
		/// It is possible to read validation errors in the console and log file.
		/// </summary>
		public void LoadPermissionsConfig()
		{
			// File lives on server, if client has a file it is irrelevant
			if (CustomNetworkManager.IsServer == false) return;

			if (AccessFile.Exists(ConfigPath) == false)
			{
				Loggy.Error("Permissions config file not found!", Category.Admin);
				return;
			}

			var fileContent = AccessFile.Load(ConfigPath);
			LoadPermissionsConfig(fileContent);
		}

		public void LoadPermissionsConfig(string fileContent)
		{
			if (Toml.TryToModel(fileContent, out PermissionsConfig model, out DiagnosticsBag diagnostics) == false)
			{
				Loggy.Error("Permissions config file is invalid! See next to find why.", Category.Admin);
				var errors = diagnostics.GetEnumerator();
				while (errors.MoveNext())
				{
					Loggy.Error($"reason: {errors.Current?.Message}", Category.Admin);
				}
				errors.Dispose();
				return;
			}

			Config = model;
			FillRankNames();

			StringBuilder logMessage = new("Finished loading permissions config file.");
			logMessage.Append("Players with rank defined: ");
			if (Config.Players.Count > 0)
			{
				logMessage.AppendLine("The following players have ranks assigned: ");
				foreach (Player player in Config.Players)
				{
					logMessage.AppendLine($" >AccountId: {player.Identifier} >Rank: {player.Rank}");
				}
			}
			Loggy.Info(logMessage.ToString());
		}

		/// <summary>
		/// Returns true if the player has the permission, false otherwise.
		/// </summary>
		/// <param name="accountId">Unique identifier of the account</param>
		/// <param name="permission">which permission tag are we looking for</param>
		/// <returns></returns>
		public bool PlayerHasPermission(string accountId, string permission)
		{
			Player player = Config.Players.Find(p => p.Identifier == accountId);
			if (player == null)
			{
				// Player is not in the permissions configuration. Try to find if auto rank is defined and return that,
				// otherwise the player has no permission
				return DefinedAutoRank != null && DefinedAutoRank.Permissions.Contains(permission);
			}

			Rank rank = Config.Ranks.GetValueOrDefault(player.Rank);
			if (rank == null)
			{
				Loggy.Error($"Rank {player.Identifier} not found! We will provide auto rank if defined, " +
				            "otherwise the player has no permissions", Category.Admin);
				return DefinedAutoRank != null && DefinedAutoRank.Permissions.Contains(permission);
			}

			//wildcard permission means they have all permissions
			return rank.Permissions.Contains("*") ||
			       rank.Permissions.Contains(permission);
		}

		[CanBeNull]
		public Rank GetRankForAccount(string identifier)
		{
			Player player = Config.Players.Find(p => p.Identifier == identifier);
			return player == null
				? DefinedAutoRank
				: // Player is defined in config file, we will return their rank or null if improperly defined
				Config.Ranks.GetValueOrDefault(player.Rank);
		}

		public void AddRoleTo(string userID, string rankType, bool saveFile = false)
		{
			if (CustomNetworkManager.IsServer == false)
			{
				Loggy.Error("Client attempt to modify permissions config!", Category.Admin);
				return;
			}

			if (Config.Ranks.GetValueOrDefault(rankType) == null)
			{
				Loggy.Error($"Tried to add a non existent rank: {rankType} to Player with id:  {userID}", Category.Admin);
				return;
			}

			Player player = Config.Players.FirstOrDefault(x => x.Identifier == userID);
			if (player == null)
			{
				Config.Players.Add(new Player {Identifier = userID, Rank = rankType});
			}
			else
			{
				player.Rank = rankType;
			}

			if (saveFile)
			{
				AccessFile.Save(ConfigPath, Toml.FromModel(Config));
			}
		}

		public void AddTempRoleTo(string userID, string rankType)
		{
			if (CustomNetworkManager.IsServer == false)
			{
				Loggy.Error("Client attempt to modify permissions config!", Category.Admin);
				return;
			}

			Config.Players.Add(new Player() { Identifier = userID, Rank = rankType });
		}

		public void RemoveRoleFrom(string userID, string rankType, bool saveFile = false)
		{
			if (CustomNetworkManager.IsServer == false)
			{
				Loggy.Error("Client attempt to modify permissions config!", Category.Admin);
				return;
			}

			Rank rank = GetRankForAccount(userID);
			if (rank == null) return;
			if (rank.Name != rankType) return;
			Player player = Config.Players.FirstOrDefault(x => x.Identifier == userID);

			Config.Players.Remove(player);

			if (saveFile)
			{
				AccessFile.Save(ConfigPath, Toml.FromModel(Config));
			}
		}

		private void FillRankNames()
		{
			foreach (var kvp  in Config.Ranks )
			{
				if (kvp.Value == null) continue;
				kvp.Value.Name = kvp.Key;
			}
		}
	}
}
