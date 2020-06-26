using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(fileName = "CustomSpeechModifierCode", menuName = "ScriptableObjects/SpeechModifiers/SlurredSpeech")]
public class SlurredMod : CustomSpeechModifier
{
	// i also need to randomly insert burps and hiccups
	private static string Slur(Match m)
	{
		string x = m.ToString();
		string slur = "";

		//80% to match TG probability(?)
		if (DMMath.Prob(80))
		{
			//Randomly pick how long the slurred letter is
			int intensity = Random.Range(1, 6);
			for (int i = 0; i < intensity; i++)
			{
				slur = slur + x; //huuuuuh?
			}
		}
		else
		{
			slur = x;
		}
		return slur;
	}
	private static string HicBurp(Match m)
	{
		string x = m.ToString();
		string hicburp = "";

		if (DMMath.Prob(40))
		{
			int hicburptext = Random.Range(1,4);
			if(hicburptext == 1)
			{
				hicburp = " burp ";
			}
			else if(hicburptext == 2)
			{
				hicburp = " hic- ";
			}
			else if(hicburptext == 3)
			{
				hicburp = " hiccup- ";
			}
			else if(hicburptext == 4)
			{
				hicburp = " buuuurp ";
			}
		}
		else
		{
			hicburp = x;
		}
		return hicburp;
	}
	public override string ProcessMessage(string message)
	{
		//	//Drunk people commonly extend their vowels and doubled letters, as well as hiccupping and burping infrequently.
		//	//Regex - find vowels and other commonly slurred letters, like L. How? I dunno.
		message = Regex.Replace(message, @"", Slur);
		message = Regex.Replace(message, @" ", HicBurp);
		return message;
	}
}
