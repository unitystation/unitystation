using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// A set of flags to show active chat channels. Be aware this can contain multiple active chat channels at a time!
/// </summary>
[Flags]
public enum ChatChannel
{
	[Description("")] 	None 		= 0,
	[Description("")] 	Examine 	= 1 << 0,
	[Description("")] 	Local 		= 1 << 1,
	[Description("")] 	OOC 		= 1 << 2,
	[Description(";")] 	Common 		= 1 << 3,
	[Description(":b")] Binary 		= 1 << 4,
	[Description(":u")] Supply 		= 1 << 5,
	[Description(":y")] CentComm 	= 1 << 6,
	[Description(":c")] Command 	= 1 << 7,
	[Description(":e")] Engineering = 1 << 8,
	[Description(":m")] Medical 	= 1 << 9,
	[Description(":n")] Science 	= 1 << 10,
	[Description(":s")] Security 	= 1 << 11,
	[Description(":v")] Service 	= 1 << 12,
	[Description(":t")] Syndicate 	= 1 << 13,
	[Description("")] 	System 		= 1 << 14,
	[Description(":g")] Ghost 		= 1 << 15,
	[Description("")] 	Combat 		= 1 << 16,
	[Description("")]	Warning		= 1 << 17,
	[Description("")]	Action		= 1 << 18,
	[Description("")]	Admin		= 1 << 19
}

/// <summary>
/// A set of flags to show active chat modifiers. Be aware this can contain multiple active chat modifiers at once!
/// </summary>
[Flags]
public enum ChatModifier
{
	// The following comments are for easy reference. They may be out of date.
	// See Chat.cs to see how a message's ChatModifier is determined.
	None 	= 0,      // Default value
	Drunk 	= 1 << 0, // Slurred Speech
	Stutter = 1 << 1,
	Mute 	= 1 << 2, // Dead, unconcious or naturally mute
	Hiss 	= 1 << 3,
	Clown 	= 1 << 4, // Having the clown occupation
	Whisper = 1 << 5, // Message starts with "#" or "/w"
	Yell    = 1 << 6, // Message is in capital letters
	Emote   = 1 << 7, // Message starts with "/me" or "*"
	Exclaim = 1 << 8, // Message ends with a "!"
	Question= 1 << 9, // Message ends with a "?"
	Sing = 1 << 10, // Message starts with "/s" or "%"
	State = 1 << 22, // Silicon speaking
	Query = 1 << 23, // Silicon querying
	ColdlyState = 1 << 24, // Automated Announcer speaking
	
	//Speech mutations, these should happen before drunk, stutter and that kind of thing!
	Canadian = 1 << 11,
	French = 1 << 12,
	Italian = 1 << 13,
	Swedish = 1 << 14,
	Chav = 1 << 15,
	Smile = 1 << 16,
	Elvis = 1 << 17,
	Spurdo = 1 << 18,
	UwU = 1 << 19,
	Unintelligible = 1 << 20,
	Scotsman = 1 << 21
}

public class ChatEvent
{
	public ChatChannel channels;
	public string message;
	public string messageOthers;
	public ChatModifier modifiers = ChatModifier.None;
	public string speaker;
	public double timestamp;
	public Vector3 position = TransformState.HiddenPos;
	public GameObject originator;

	/// <summary>
	/// Send chat message only to those on this matrix
	/// </summary>
	public MatrixInfo matrix
	{
		get => _matrix;
		set => _matrix = value;
	}

	private MatrixInfo _matrix = MatrixInfo.Invalid;

	public ChatEvent() {
		timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
	}
}
