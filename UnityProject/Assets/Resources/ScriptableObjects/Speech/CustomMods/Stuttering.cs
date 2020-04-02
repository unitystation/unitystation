using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(fileName = "CustomSpeechModifierCode", menuName = "ScriptableObjects/SpeechModifiers/CustomCode")]
public class Stuttering : CustomSpeechModifier
{
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

			stutter += x; //h... h... h... h[ello]
		}
		else
		{
			stutter = x;
		}
		return stutter;
	}
    public override string ProcessMessage(string message)
    {
        //	//Stuttering people randomly repeat beginnings of words
		//	//Regex - find word boundary followed by non digit, non special symbol, non end of word letter. Basically find the start of words.
			Regex rx = new Regex(@"(\b)+([^\d\W])\B");
			message = rx.Replace(message, Stutter);

        return message;
    }
}
