using UnityEngine;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;

public partial class Chat
{
	public Color oocColor;
	public Color ghostColor;
	public Color binaryColor;
	public Color supplyColor;
	public Color centComColor;
	public Color commandColor;
	public Color commonColor;
	public Color engineeringColor;
	public Color medicalColor;
	public Color scienceColor;
	public Color securityColor;
	public Color serviceColor;
	public Color localColor;
	public Color combatColor;
	public Color defaultColor;

	public static string ProcessMessageFurther(string message, string speaker, ChatChannel channels,
		ChatModifier modifiers)
	{ // TODO this should use modifiers to determine if the player is shouting or not
		//Skip everything if system message
		if (channels.HasFlag(ChatChannel.System))
		{
			return message;
		}

		//Skip everything in case of combat channel
		if (channels.HasFlag(ChatChannel.Combat))
		{
			return AddMsgColor(channels, $"<i>{message}</i>"); //POC
		}

		//Skip everything if it is an action or examine message or if it is a local message
		//without a speaker (which is used by machines)
		if (channels.HasFlag(ChatChannel.Examine) || channels.HasFlag(ChatChannel.Action)
		    || channels.HasFlag(ChatChannel.Local) && string.IsNullOrEmpty(speaker))
		{
			return AddMsgColor(channels, $"<i>{message}</i>");
		}

		// Skip everything if the message is a local warning
		if (channels.HasFlag(ChatChannel.Warning))
		{
			return AddMsgColor(channels, $"<i>{message}</i>");
		}

		message = StripTags(message);

		//Check for emote. If found skip chat modifiers, make sure emote is only in Local channel
		if ((modifiers & ChatModifier.Emote) == ChatModifier.Emote)
		{
			// /me message
			channels = ChatChannel.Local;
			message = AddMsgColor(channels, $"<i><b>{speaker}</b> {message}</i>");
			return message;
		}

		//Check for OOC. If selected, remove all other channels and modifiers (could happen if UI fucks up or someone tampers with it)
		if (channels.HasFlag(ChatChannel.OOC))
		{
			message = AddMsgColor(channels, $"[ooc] <b>{speaker}: {message}</b>");
			return message;
		}

		//Ghosts don't get modifiers
		if (channels.HasFlag(ChatChannel.Ghost))
		{
			return AddMsgColor(channels, $"[dead] <b>{speaker}</b>: {message}");
		}

		var verb = "says:";

		if ((modifiers & ChatModifier.Whisper) == ChatModifier.Whisper)
		{
			verb = "whispers,";
			message = $"<b>{message}</b>";
		}
		else if ((modifiers & ChatModifier.Yell) == ChatModifier.Yell)
		{
			verb = "yells,";
			message = $"<b>{message}</b>";
		}
		else if (message.Contains("!")){ // Not an "official" ChatModifier
			verb = "exclaims,";
		}

		var chan = $"[{channels.ToString().ToLower().Substring(0, 3)}] ";

		if (channels.HasFlag(ChatChannel.Command))
		{
			chan = "[cmd] ";
		}

		if (channels.HasFlag(ChatChannel.Local))
		{
			chan = "";
		}

		return AddMsgColor(channels, $"{chan}<b>{speaker}</b> {verb} " + "\"" + message + "\"");
	}

	private static string StripTags(string input)
	{
		//Regex - find "<" followed by any number of not ">" and ending in ">". Matches any HTML tags.
		Regex rx = new Regex("[<][^>]+[>]");
		string output = rx.Replace(input, "");

		return output;
	}

	private static string Slur(Match m)
	{
		string x = m.ToString();
		if (char.IsLower(x[0]))
		{
			x = x + "h";
		}
		else
		{
			x = x + "H";
		}

		return x;
	}

	private static string Hic(Match m)
	{
		string x = m.ToString();
		//10% chance to hic at any given space
		if (Random.Range(1, 11) == 1)
		{
			x = " ...hic!... ";
		}

		return x;
	}

	private static string Hiss(Match m)
	{
		string x = m.ToString();
		if (char.IsLower(x[0]))
		{
			x = x + "ss";
		}
		else
		{
			x = x + "SS";
		}

		return x;
	}

	private static string Stutter(Match m)
	{
		string x = m.ToString();
		string stutter = "";
		//20% chance to stutter at any given consonant
		if (Random.Range(1, 6) == 1)
		{
			//Randomly pick how bad is the stutter
			int intensity = Random.Range(1, 4);
			for (int i = 0; i < intensity; i++)
			{
				stutter = stutter + x + "... "; //h... h... h...
			}

			stutter = stutter + x; //h... h... h... h[ello]
		}
		else
		{
			stutter = x;
		}
		return stutter;
	}

	private static string AddMsgColor(ChatChannel channel, string message)
	{
		return $"<color=#{GetChannelColor(channel)}>{message}</color>";
	}



	private static string GetChannelColor(ChatChannel channel)
	{
		if (channel.HasFlag(ChatChannel.OOC)) return ColorUtility.ToHtmlStringRGBA(Instance.oocColor);
		if (channel.HasFlag(ChatChannel.Ghost)) return ColorUtility.ToHtmlStringRGBA(Instance.ghostColor);
		if (channel.HasFlag(ChatChannel.Binary)) return ColorUtility.ToHtmlStringRGBA(Instance.binaryColor);
		if (channel.HasFlag(ChatChannel.Supply)) return ColorUtility.ToHtmlStringRGBA(Instance.supplyColor);
		if (channel.HasFlag(ChatChannel.CentComm)) return ColorUtility.ToHtmlStringRGBA(Instance.centComColor);
		if (channel.HasFlag(ChatChannel.Command)) return ColorUtility.ToHtmlStringRGBA(Instance.commandColor);
		if (channel.HasFlag(ChatChannel.Common)) return ColorUtility.ToHtmlStringRGBA(Instance.commonColor);
		if (channel.HasFlag(ChatChannel.Engineering)) return ColorUtility.ToHtmlStringRGBA(Instance.engineeringColor);
		if (channel.HasFlag(ChatChannel.Medical)) return ColorUtility.ToHtmlStringRGBA(Instance.medicalColor);
		if (channel.HasFlag(ChatChannel.Science)) return ColorUtility.ToHtmlStringRGBA(Instance.scienceColor);
		if (channel.HasFlag(ChatChannel.Security)) return ColorUtility.ToHtmlStringRGBA(Instance.securityColor);
		if (channel.HasFlag(ChatChannel.Service)) return ColorUtility.ToHtmlStringRGBA(Instance.serviceColor);
		if (channel.HasFlag(ChatChannel.Local)) return ColorUtility.ToHtmlStringRGBA(Instance.localColor);
		if (channel.HasFlag(ChatChannel.Combat)) return ColorUtility.ToHtmlStringRGBA(Instance.combatColor);
		return ColorUtility.ToHtmlStringRGBA(Instance.defaultColor);;
	}

	private static bool IsNamelessChan(ChatChannel channel)
	{
		if (channel.HasFlag(ChatChannel.System) ||
		    channel.HasFlag(ChatChannel.Combat) ||
		    channel.HasFlag(ChatChannel.Action) ||
		    channel.HasFlag(ChatChannel.Examine))
		{
			return true;
		}
		return false;
	}
}
