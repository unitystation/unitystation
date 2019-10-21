using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

public partial class Chat
{
	public static string ProcessMessageFurther(string message, string speaker, ChatChannel channels,
		ChatModifier modifiers)
	{
		message = StripTags(message);

		//Skip everything if system message
		if (channels.HasFlag(ChatChannel.System))
		{
			return $"<b><i>{message}</i></b>";
		}

		//Skip everything in case of combat channel
		if (channels.HasFlag(ChatChannel.Combat))
		{
			return AddMsgColor(channels, $"<b>{message}</b>"); //POC
		}

		//Skip everything if examining something
		if (channels.HasFlag(ChatChannel.Examine))
		{
			return AddMsgColor(channels, $"<b><i>{message}</i></b>");
		}

		// Skip everything if the message is a local warning
		if (channels.HasFlag(ChatChannel.Warning))
		{
			return AddMsgColor(channels, $"<i>{message}</i>");
		}

		//Check for emote. If found skip chat modifiers, make sure emote is only in Local channel
		Regex rx = new Regex("^(/me )");
		if (rx.IsMatch(message))
		{
			// /me message
			channels = ChatChannel.Local;

			message = rx.Replace(message, " ");
			message = AddMsgColor(channels, $"<i><b>{speaker}</b> {message}</i>");
			return message;
		}

		//Check for OOC. If selected, remove all other channels and modifiers (could happen if UI fucks up or someone tampers with it)
		if (channels.HasFlag(ChatChannel.OOC))
		{
			message = AddMsgColor(channels, $"<b>{speaker}: {message}</b>");
			return message;
		}

		//Ghosts don't get modifiers
		if (channels.HasFlag(ChatChannel.Ghost))
		{
			return AddMsgColor(channels, $"<b>{speaker}: ") + $"<color=white>{message}</b></color>";
		}

		message = ApplyModifiers(message, modifiers);
		if (message.Length < 1)
		{
			return "";
		}

		return AddMsgColor(channels, $"<b>{speaker}</b> says:") + "<color=white> \"" + message + "\"</color>";
	}

	private static string StripTags(string input)
	{
		//Regex - find "<" followed by any number of not ">" and ending in ">". Matches any HTML tags.
		Regex rx = new Regex("[<][^>]+[>]");
		string output = rx.Replace(input, "");

		return output;
	}

	private static string ApplyModifiers(string input, ChatModifier modifiers)
	{
		string output = input;

		//Clowns say a random number (1-3) HONK!'s after every message
		if ((modifiers & ChatModifier.Clown) == ChatModifier.Clown)
		{
			int intensity = Random.Range(1, 4);
			for (int i = 0; i < intensity; i++)
			{
				if (i == 0)
				{
					output = output + " HONK!";
				}
				else
				{
					output = output + "HONK!";
				}
			}
		}

		//Sneks say extra S's
		if ((modifiers & ChatModifier.Hiss) == ChatModifier.Hiss)
		{
			//Regex - find 1 or more "s"
			Regex rx = new Regex("s+|S+");
			output = rx.Replace(output, Hiss);
		}

		//Stuttering people randomly repeat beginnings of words
		if ((modifiers & ChatModifier.Stutter) == ChatModifier.Stutter)
		{
			//Regex - find word boundary followed by non digit, non special symbol, non end of word letter. Basically find the start of words.
			Regex rx = new Regex(@"(\b)+([^\d\W])\B");
			output = rx.Replace(output, Stutter);
		}

		//Drunk people slur all "s" into "sh", randomly ...hic!... between words and have high % to ...hic!... after a sentance
		if ((modifiers & ChatModifier.Drunk) == ChatModifier.Drunk)
		{
			//Regex - find 1 or more "s"
			Regex rx = new Regex("s+|S+");
			output = rx.Replace(output, Slur);
			//Regex - find 1 or more whitespace
			rx = new Regex(@"\s+");
			output = rx.Replace(output, Hic);
			//50% chance to ...hic!... at end of sentance
			if (Random.Range(1, 3) == 1)
			{
				output = output + " ...hic!...";
			}
		}
		if ((modifiers & ChatModifier.Whisper) == ChatModifier.Whisper)
		{
			//If user is in barely conscious state, make text italic
			//todo: decrease range and modify text somehow
			//This can be changed later to other status effects
			output = "<i>"+output+"</i>";
		}
		if ((modifiers & ChatModifier.Mute) == ChatModifier.Mute)
		{
			//If user is in unconscious state remove text
			//This can be changed later to other status effects
			output = "";
		}

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
		if (channel.HasFlag(ChatChannel.OOC)) return "386aff";
		if (channel.HasFlag(ChatChannel.Ghost)) return "386aff";
		if (channel.HasFlag(ChatChannel.Binary)) return "ff00ff";
		if (channel.HasFlag(ChatChannel.Supply)) return "a8732b";
		if (channel.HasFlag(ChatChannel.CentComm)) return "686868";
		if (channel.HasFlag(ChatChannel.Command)) return "204090";
		if (channel.HasFlag(ChatChannel.Common)) return "008000";
		if (channel.HasFlag(ChatChannel.Engineering)) return "fb5613";
		if (channel.HasFlag(ChatChannel.Medical)) return "337296";
		if (channel.HasFlag(ChatChannel.Science)) return "993399";
		if (channel.HasFlag(ChatChannel.Security)) return "a30000";
		if (channel.HasFlag(ChatChannel.Service)) return "6eaa2c";
		if (channel.HasFlag(ChatChannel.Local)) return "white";
		if (channel.HasFlag(ChatChannel.Combat)) return "dd0000";
		return "white";
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
