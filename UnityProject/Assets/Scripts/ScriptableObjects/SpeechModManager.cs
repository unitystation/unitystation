using System;
using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "SpeechModManager", menuName = "Singleton/SpeechModManager")]
	public class SpeechModManager: SingletonScriptableObject<SpeechModManager>
	{
		public SerializableDictionary<ChatModifier, SpeechModifier> speechModifier;

		public string ApplyMod(ChatModifier modifiers, string message)
		{
			// Prevents accents from modifying emotes
			if (modifiers.HasFlag(ChatModifier.Emote))
			{
				return message;
			}

			foreach (var kvp in speechModifier)
			{
				if (modifiers.HasFlag(kvp.Key))
				{
					message = kvp.Value.ProcessMessage(message);
				}
			}

			return message;
		}
	}

}