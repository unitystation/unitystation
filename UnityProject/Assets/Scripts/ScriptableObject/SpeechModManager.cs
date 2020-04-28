using UnityEngine;
using System;

[CreateAssetMenu(fileName = "SpeechModManager", menuName = "Singleton/SpeechModManager")]
public class SpeechModManager: SingletonScriptableObject<SpeechModManager>
{
    public SpeechModDict speechModifier;

    public string ApplyMod(ChatModifier modifiers, string message)
    {
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

[Serializable]
public class SpeechModDict : SerializableDictionary<ChatModifier, SpeechModifier>{}