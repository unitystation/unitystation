using System.Text.RegularExpressions;
using UnityEngine;

namespace ScriptableObjects.Speech.CustomMods
{
	[CreateAssetMenu(fileName = "CustomSpeechModifierCode", menuName = "ScriptableObjects/SpeechModifiers/HissSpeech")]
	public class HissMod : CustomSpeechModifier
	{
		public override string ProcessMessage(string message)
		{
			message = Regex.Replace(message, @"\B([iusl]\b)", Hiss);
			return message;
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
	}
}