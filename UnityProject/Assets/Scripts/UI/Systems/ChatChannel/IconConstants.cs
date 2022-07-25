using System.Collections.Generic;

namespace UI
{
	public static class IconConstants
	{
		public static readonly Dictionary<ChatChannel, string> ChatPanelIcons
			= new Dictionary<ChatChannel, string>
			{
				//To add new glyphs go to https://fontawesome.com/icons then copy the glyph you want

				{ChatChannel.Local, ""}, //fa-comments
				{ChatChannel.OOC, ""}, //fa-comments-o
				{ChatChannel.Binary, ""}, //fa-microchip
				{ChatChannel.Supply, ""}, //fa-cube
				{ChatChannel.CentComm, ""}, //fa-institution
				{ChatChannel.Command, ""}, //fa-flag
				{ChatChannel.Common, ""}, //fa-headphones
				{ChatChannel.Engineering, ""}, //fa-wrench
				{ChatChannel.Medical, ""}, //fa-hotel
				{ChatChannel.Science, ""}, //fa-flask
				{ChatChannel.Service, ""}, //fa-bitcoin HODL
				{ChatChannel.Syndicate, ""}, //fa-bomb
				{ChatChannel.Security, ""}, //fa-crosshairs
				{ChatChannel.Ghost, ""}, //fa-snapchat-ghost
				{ChatChannel.Blob, ""}, //fa-bullseye
				{ChatChannel.Alien, ""} //fa-user-alien
			};
	}
}
