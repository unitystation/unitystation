using System.IO;
using Logs;
using SecureStuff;
using Shared.Managers;
using Tomlyn;
using UnityEngine;

namespace Systems.Permissions
{
	public class PermissionsManager: SingletonManager<PermissionsManager>
	{
		private readonly string configPath = Path.Combine(AccessFile.AdminFolder, "permissions.toml");

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
				Loggy.LogError("Permissions config file not found!", Category.Admin);
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
				Loggy.LogError("Permissions config file is invalid! See next to find why.", Category.Admin);
				var errors = diagnostics.GetEnumerator();
				while (errors.MoveNext())
				{
					Loggy.LogError($"reason: {errors.Current?.Message}", Category.Admin);
				}
				errors.Dispose();
				Config = new PermissionsConfig();
				return;
			}

			Config = model;
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
	}
}