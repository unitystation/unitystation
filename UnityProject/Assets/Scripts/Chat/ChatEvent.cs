using System;
using UnityEngine;
using System.Text.RegularExpressions;

[Flags]
public enum ChatChannel
{
	None = 0,
	Examine = 1,
	Local = 2,
	OOC = 4,
	Common = 8,
	Binary = 16,
	Supply = 32,
	CentComm = 64,
	Command = 128,
	Engineering = 256,
	Medical = 512,
	Science = 1024,
	Security = 2048,
	Service = 4096,
	Syndicate = 8192,
	System = 16384,
	Ghost = 32768
}

[Flags]
public enum ChatModifier
{
	None = 0,
	Drunk = 1,
	Stutter = 2,
	Hiss = 4,
	Clown = 8,
}

public class ChatEvent
{
    public string message;
	public string speaker;
	public double timestamp;
	public ChatChannel channels;
	public ChatModifier modifiers;

	public ChatEvent(string message, string speaker, ChatChannel channels, ChatModifier modifiers)
	{
		this.timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
		this.channels = channels;
		this.modifiers = modifiers;
		this.speaker = speaker;
		this.message = ProcessMessage(message, speaker, this.channels, this.modifiers);
	}

	public ChatEvent(string message, ChatChannel channels, bool skipProcessing = false)
	{
		this.timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
		this.channels = channels;
		this.modifiers = ChatModifier.None;
		this.speaker = "";
		if(skipProcessing) {
			this.message = message;
		} else {
			this.message = ProcessMessage(message, speaker, this.channels, this.modifiers);
		}
		
	}

	public static ChatChannel GetNonNetworkedChannels()
	{
		return (ChatChannel.Examine | ChatChannel.System);
	}

	private string ProcessMessage(string message, string speaker, ChatChannel channels, ChatModifier modifiers)
	{
		message = StripTags(message);

		//Skip everything if system message
		if ((channels & ChatChannel.System) == ChatChannel.System)
		{
			this.channels = ChatChannel.System;
			this.modifiers = ChatModifier.None;
			return message = "<b><i>" + message + "</i></b>";
		}

		//Skip everything if examining something
		if ((channels & ChatChannel.Examine) == ChatChannel.Examine) {
			this.channels = ChatChannel.Examine;
			this.modifiers = ChatModifier.None;
			return message = "<b><i>" + message + "</i></b>";
		}

		//Check for emote. If found skip chat modifiers, make sure emote is only in Local channel
		var rx = new Regex("^(/me )");
		if (rx.IsMatch(message)) { // /me message
			this.channels = ChatChannel.Local;
			message = rx.Replace(message, " ");
			message = "<i><b>" + speaker + "</b> " + message + "</i>";
			return message;
		}

		//Check for OOC. If selected, remove all other channels and modifiers (could happen if UI fucks up or someone tampers with it)
		if((channels & ChatChannel.OOC) == ChatChannel.OOC) {
			this.channels = ChatChannel.OOC;
			this.modifiers = ChatModifier.None;

			message = "<b>" + speaker + ": " + message + "</b>";
			return message;
		}

		//Ghosts don't get modifiers
		if ((channels & ChatChannel.Ghost) == ChatChannel.Ghost)
		{
			this.channels = ChatChannel.Ghost;
			this.modifiers = ChatModifier.None;
			return message = "<b>" + speaker + ": " + message + "</b>";
		}

		message = ApplyModifiers(message, modifiers);
		message = "<b>" + speaker + "</b> says: \"" + message + "\"";

		return message;
	}

	private string StripTags(string input)
	{
		//Regex - find "<" followed by any number of not ">" and ending in ">". Matches any HTML tags.
		var rx = new Regex("[<][^>]+[>]");
		var output = rx.Replace(input, "");

		return output;
	}

	private string ApplyModifiers(string input, ChatModifier modifiers) {
		var output = input;

		//Clowns say a random number (1-3) HONK!'s after every message
		if ((modifiers & ChatModifier.Clown) == ChatModifier.Clown) {
			int intensity = UnityEngine.Random.Range(1, 4);
			for (int i = 0; i < intensity; i++)
			{
				if(i == 0) {
					output = output + " HONK!";
				} else {
					output = output + "HONK!";
				}
			}
		}

		//Sneks say extra S's
		if ((modifiers & ChatModifier.Hiss) == ChatModifier.Hiss)
		{
			//Regex - find 1 or more "s"
			var rx = new Regex("s+|S+");
			output = rx.Replace(output, new MatchEvaluator(ChatEvent.Hiss));
		}

		//Stuttering people randomly repeat beginnings of words
		if ((modifiers & ChatModifier.Stutter) == ChatModifier.Stutter)
		{
			//Regex - find word boundary followed by non digit, non special symbol, non end of word letter. Basically find the start of words.
			var rx = new Regex(@"(\b)+([^\d\W])\B");
			output = rx.Replace(output, new MatchEvaluator(ChatEvent.Stutter));
		}

		//Drunk people slur all "s" into "sh", randomly ...hic!... between words and have high % to ...hic!... after a sentance
		if ((modifiers & ChatModifier.Drunk) == ChatModifier.Drunk) {
			//Regex - find 1 or more "s"
			var rx = new Regex("s+|S+");
			output = rx.Replace(output, new MatchEvaluator(ChatEvent.Slur));
			//Regex - find 1 or more whitespace
			rx = new Regex(@"\s+");
			output = rx.Replace(output, new MatchEvaluator(ChatEvent.Hic));
			//50% chance to ...hic!... at end of sentance
			if(UnityEngine.Random.Range(1,3) == 1) {
				output = output + " ...hic!...";
			}
		}

		return output;
	}

#region Match Evaluators - contains the methods for string replacement magic  
	private static string Slur(Match m) {
		string x = m.ToString();
		if(Char.IsLower(x[0])) {
			x = x + "h";
		} else {
			x = x + "H";
		}
		
		return x;
	}

	private static string Hic(Match m)
	{
		string x = m.ToString();
		//10% chance to hic at any given space
		if(UnityEngine.Random.Range(1, 11) == 1) { 
			x = " ...hic!... ";
		}

		return x;
	}

	private static string Hiss(Match m)
	{
		string x = m.ToString();
		if(Char.IsLower(x[0])) {
			x = x + "ss";
		} else {
			x = x + "SS";
		}
		
		return x;
	}

	private static string Stutter(Match m)
	{
		string x = m.ToString();
		string stutter = "";
		//20% chance to stutter at any given consonant
		if (UnityEngine.Random.Range(1, 6) == 1) {
			//Randomly pick how bad is the stutter
			int intensity = UnityEngine.Random.Range(1, 4);
			for (int i = 0; i < intensity; i++) {
				stutter = stutter + x + "... "; //h... h... h...
			}

			stutter = stutter + x; //h... h... h... h[ello]
		} else {
			stutter = x;
		}
		return stutter;
	}
	#endregion
}