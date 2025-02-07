using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Logs;
using SecureStuff;
using Shared.Managers;
using Tomlyn;
using UnityEngine;

namespace Systems.Permissions
{
	public class PermissionsManager: SingletonManager<PermissionsManager>
	{
		public readonly string configPath = Path.Combine(AccessFile.AdminFolder, "permissions.toml");

		public PermissionsConfig Config { get; private set; }



		/// <summary>
		/// Tries to read the permissions config file and load it in memory. If for whatever reason it fails,
		/// there will be no permissions config and thus, no one will have any permissions.
		///
		/// It is possible to read validation errors in the console and log file.
		/// </summary>
		public void LoadPermissionsConfig()
		{

			if (AccessFile.Exists(configPath) == false)
			{
				Loggy.Error("Permissions config file not found!", Category.Admin);
				Config = new PermissionsConfig();
				return;
			}

			var fileContent = AccessFile.Load(configPath);

			LoadPermissionsConfig(fileContent);
		}

		public void LoadPermissionsConfig(string fileContent)
		{
			if (Toml.TryToModel<PermissionsConfig>(fileContent, out var model, out var diagnostics) == false)
			{
				Loggy.Error("Permissions config file is invalid! See next to find why.", Category.Admin);
				var errors = diagnostics.GetEnumerator();
				while (errors.MoveNext())
				{
					Loggy.Error($"reason: {errors.Current?.Message}", Category.Admin);
				}
				errors.Dispose();
				Config = new PermissionsConfig();
				return;
			}

			Config = model;

			var names = new StringBuilder();
			names.Append("Admins Loaded: ");
			foreach (var adminName in Config.Players)
			{
				names.AppendLine("Rank > " + adminName.Rank + " ID > " + adminName.Identifier);
			}
			Loggy.Info(names.ToString());
		}

		/// <summary>
		/// Returns true if the player has the permission, false otherwise.
		/// </summary>
		/// <param name="identifier">UUID from firebase or player identifier after we migrate to django.</param>
		/// <param name="permission">which permission are we looking for</param>
		/// <returns></returns>
		public bool HasPermission(string identifier, string permission)
		{
			var player = Config.Players.Find(p => p.Identifier == identifier);
			if (player == null)
			{
				//Player not found, so they don't have any permissions
				return false;
			}
			var rankName = player.Rank;
			if (Config.Ranks.ContainsKey(rankName) == false)
			{
				//Rank not found, so they don't have any permissions
				return false;
			}

			var rank = Config.Ranks[rankName];

			//wildcard permission means they have all permissions
			return rank.Permissions.Contains("*") ||
			       rank.Permissions.Contains(permission);
		}


		/// <summary>
		/// Returns a list of the permissions a player has, returns an empty list if they don't have any or Player is not found
		/// </summary>
		/// <param name="identifier">UUID from firebase or player identifier after we migrate to django.</param>
		/// <returns></returns>
		public List<string> GetPermissions(string identifier, out string rankType)
		{

			rankType = "";

			List<string> Returning = new List<string>();

			var rank = GetRank(identifier, out rankType);

			if (rank == null)
			{
				return Returning;
			}

			//wildcard permission means they have all permissions
			Returning.AddRange(rank.Permissions);
			return Returning;
		}

		public Rank GetRank(string identifier, out string rankType)
		{
			rankType = "";
			var player = Config.Players.Find(p => p.Identifier == identifier);
			if (player == null)
			{
				//Player not found, so they don't have any permissions
				return null;
			}
			var rankName = player.Rank;
			rankType = rankName;
			//Rank not found, so they don't have any permissions
			return Config.Ranks.GetValueOrDefault(rankName);
		}


		public void AddRoleTo(string userID, string rankType, bool saveFile = false)
		{
			var data = GetRank(userID, out var Rank);

			if (data != null) return;


			Config.Players.Add(new Player(){Identifier = userID, Rank = rankType});

			if (saveFile)
			{
				AccessFile.Save(configPath, Toml.FromModel(Config));
			}
		}

		public void RemoveRoleFrom(string userID, string rankType, bool saveFile = false)
		{
			var data = GetRank(userID, out var Rank);

			if (data == null) return;

			if (Rank != rankType) return;

			var player = Config.Players.FirstOrDefault(x => x.Identifier == userID);

			Config.Players.Remove(player);

			if (saveFile)
			{
				AccessFile.Save(configPath, Toml.FromModel(Config));
			}
		}
	}
}