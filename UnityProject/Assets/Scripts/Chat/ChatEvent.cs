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
	[Description("")]	Action		= 1 << 18
}

/// <summary>
/// A set of flags to show active chat modifiers. Be aware this can contain multiple active chat modifiers at once!
/// </summary>
[Flags]
public enum ChatModifier
{
	None 	= 0,
	Drunk 	= 1 << 0,
	Stutter = 1 << 1,
	Mute 	= 1 << 2,
	Hiss 	= 1 << 3,
	Clown 	= 1 << 4,
	Whisper = 1 << 5,
}

public class ChatEvent
{
	public ChatChannel channels;
	public string message;
	public string messageOthers;
	public ChatModifier modifiers = ChatModifier.None;
	public string speaker;
	public double timestamp;
	public Vector2 position;
	public GameObject originator;

	/// <summary>
	/// Send chat message only to those on this matrix
	/// </summary>
	public MatrixInfo matrix = MatrixInfo.Invalid;

	public ChatEvent() {
		timestamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
	}
}