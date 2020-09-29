﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Parsed data from player chat input
/// Contains meta information about channels, modifiers, etc
/// </summary>
public class ParsedChatInput 
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="rawInput">The raw message from input field</param>
	/// <param name="clearMessage">The message cleared from all tags</param>
	/// <param name="extractedChannel">The channels parsed from tags</param>
	public ParsedChatInput(string rawInput, string clearMessage, ChatChannel extractedChannel)
	{
		RawInput = rawInput;
		ClearMessage = clearMessage;
		ParsedChannel = extractedChannel;
	}

	/// <summary>
	/// The raw message from client input field
	/// </summary>
	public string RawInput { get; set; }

	/// <summary>
	/// The message cleared from all tags
	/// </summary>
	public string ClearMessage { get; set; }

	/// <summary>
	/// The channel flag parsed from tag. ChatChannel.None if no tag found
	/// </summary>
	public ChatChannel ParsedChannel { get; set; }
}
