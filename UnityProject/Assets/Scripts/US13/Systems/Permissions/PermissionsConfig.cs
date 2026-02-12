using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Tomlyn.Model;

namespace US13.Systems.Permissions
{
	/// <summary>
	/// Model for permissions.toml file. Has reference to all ranks and permissions created by the server owner and all players with their rank.
	/// </summary>
	public class PermissionsConfig: ITomlMetadataProvider
	{
		// keep comments on config file
		public TomlPropertiesMetadata PropertiesMetadata { get; set; } = new();

		[UsedImplicitly] public Dictionary<string, Rank> Ranks { get; set; } = new();

		[UsedImplicitly] public List<Player> Players { get; set; } = new();

		[CanBeNull] [UsedImplicitly] public string AutoRank { get; set; } = "";
	}

	[System.Serializable]
	public class Rank
	{
		[IgnoreDataMember] public string Name { get; set; } = "";
		public bool ShowInChat { get; set; } = false;
		public string Abbreviation { get; set; } = "";
		public string Color { get; set; } = "";
		public List<string> Permissions { get; set; } = new();
	}

	public class Player
	{
		public string Identifier { get; set; } = "";
		public string Rank { get; set; } = "";
	}
}
