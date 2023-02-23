using System.Collections.Generic;
using Tomlyn.Model;

namespace Systems.Permissions
{
	/// <summary>
	/// Model for permissions.toml file. Has reference to all ranks and permissions created by the server owner and all players with their rank.
	/// </summary>
	public class PermissionsConfig: ITomlMetadataProvider
	{
		// keep comments on config file
		public TomlPropertiesMetadata PropertiesMetadata { get; set; }

		public Dictionary<string, Rank> Ranks { get; set; }
		public List<Player> Players { get; set; }
	}

	public class Rank
	{
		public bool ShowInChat { get; set; }
		public string Abbreviation { get; set; }
		public string Color { get; set; }
		public List<string> Permissions { get; set; }
	}

	public class Player
	{
		public string Identifier { get; set; }
		public string Rank { get; set; }
	}
}