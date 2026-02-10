using System.Collections.Generic;

namespace US13.UI.Systems.ChatChannel
{
	public static class IconConstants
	{
		public static readonly Dictionary<US13.Core.Chat.ChatChannel, string> ChatPanelIcons
			= new Dictionary<US13.Core.Chat.ChatChannel, string>
			{
				//To add new glyphs go to https://fontawesome.com/icons then copy the glyph you want

				{US13.Core.Chat.ChatChannel.Local, ""}, //fa-comments
				{US13.Core.Chat.ChatChannel.OOC, ""}, //fa-comments-o
				{US13.Core.Chat.ChatChannel.Binary, ""}, //fa-microchip
				{US13.Core.Chat.ChatChannel.Supply, ""}, //fa-cube
				{US13.Core.Chat.ChatChannel.CentComm, ""}, //fa-institution
				{US13.Core.Chat.ChatChannel.Command, ""}, //fa-flag
				{US13.Core.Chat.ChatChannel.Common, ""}, //fa-headphones
				{US13.Core.Chat.ChatChannel.Engineering, ""}, //fa-wrench
				{US13.Core.Chat.ChatChannel.Medical, ""}, //fa-hotel
				{US13.Core.Chat.ChatChannel.Science, ""}, //fa-flask
				{US13.Core.Chat.ChatChannel.Service, ""}, //fa-bitcoin HODL
				{US13.Core.Chat.ChatChannel.Syndicate, ""}, //fa-bomb
				{US13.Core.Chat.ChatChannel.Security, ""}, //fa-crosshairs
				{US13.Core.Chat.ChatChannel.Ghost, ""}, //fa-snapchat-ghost
				{US13.Core.Chat.ChatChannel.Blob, ""}, //fa-bullseye
				{US13.Core.Chat.ChatChannel.Alien, ""} //fa-sitemap
			};

		public static readonly Dictionary<string, string> ChangelogIcons = new()
		{
			{"FIX", ""}, //fa-wrench
			{"IMPROVEMENT", ""}, //fa-hand-point-up
			{"NEW", ""}, //fa-circle-plus
			{"BALANCE","" } //fa-scale-balanced
		};
	}
}
