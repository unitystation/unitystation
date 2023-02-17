using Shared.Managers;
using System.IO;
using Tomlyn;
using UnityEngine;

namespace Systems.Permissions
{
	public class PermissionsManager: SingletonManager<PermissionsManager>
	{
		private readonly string configPath = Path.Combine(Application.streamingAssetsPath, "admin", "permissions.toml");

		public PermissionsConfig Config { get; private set; }

		/// <summary>
		/// Tries to read the permissions config file and load it in memory. If for whatever reason it fails,
		/// there will be no permissions config and thus, no one will have any permissions.
		///
		/// It is possible to read validation errors in the console and log file.
		/// </summary>
		public void LoadPermissionsConfig()
		{
			if (File.Exists(configPath) == false)
			{
				Logger.LogError("Permissions config file not found!", Category.Admin);
				Config = new PermissionsConfig();
				return;
			}

			var fileContent = File.ReadAllText(configPath);

			LoadPermissionsConfig(fileContent);
		}

		public void LoadPermissionsConfig(string fileContent)
		{
			if (Toml.TryToModel<PermissionsConfig>(fileContent, out var model, out var diagnostics) == false)
			{
				Logger.LogError("Permissions config file is invalid! See next to find why.", Category.Admin);
				var errors = diagnostics.GetEnumerator();
				while (errors.MoveNext())
				{
					Logger.LogError($"reason: {errors.Current?.Message}", Category.Admin);
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
			return rank.Permissions.Contains(permission);
		}
	}
}