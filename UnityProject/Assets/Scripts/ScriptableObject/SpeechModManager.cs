using UnityEngine;
using System;

[CreateAssetMenu(fileName = "SpeechModManager", menuName = "Singleton/SpeechModManager")]
public class SpeechModManager: SingletonScriptableObject<SpeechModManager>
{
    // Add your new speech mutations here
    public SpeechModifierSO Canadian;
    public SpeechModifierSO Chav;
    public SpeechModifierSO Elvis;
    public SpeechModifierSO French;
    public SpeechModifierSO Italian;
    public SpeechModifierSO Spurdo;
    public SpeechModifierSO Swedish;
    public SpeechModifierSO UwU;
    public SpeechModifierSO Stutter;


    public string ApplyMod(ChatModifier modifiers, string message)
    {
		if ((modifiers & ChatModifier.Canadian) == ChatModifier.Canadian)
        {
            message = Canadian.ProcessMessage(message);
        }

		if ((modifiers & ChatModifier.Chav) == ChatModifier.Chav)
        {
            message = Chav.ProcessMessage(message);
        }

		if ((modifiers & ChatModifier.Elvis) == ChatModifier.Elvis)
        {
            message = Elvis.ProcessMessage(message);
        }

		if ((modifiers & ChatModifier.French) == ChatModifier.French)
        {
            message = French.ProcessMessage(message);
        }

		if ((modifiers & ChatModifier.Italian) == ChatModifier.Italian)
        {
            message = Italian.ProcessMessage(message);
        }

		if ((modifiers & ChatModifier.Swedish) == ChatModifier.Swedish)
        {
            message = Swedish.ProcessMessage(message);
        }

        if ((modifiers & ChatModifier.Spurdo) == ChatModifier.Spurdo)
        {
            message = Spurdo.ProcessMessage(message);
        }

        if ((modifiers & ChatModifier.UwU) == ChatModifier.UwU)
        {
            message = UwU.ProcessMessage(message);
        }

        if ((modifiers & ChatModifier.Stutter) == ChatModifier.Stutter)
        {
            message = Stutter.ProcessMessage(message);
        }

        return message;
    }
}

