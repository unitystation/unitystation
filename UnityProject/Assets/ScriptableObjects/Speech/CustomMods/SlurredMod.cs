using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using ScriptableObjects;

[CreateAssetMenu(fileName = "CustomSpeechModifierCode", menuName = "ScriptableObjects/SpeechModifiers/SlurredSpeech")]
public class SlurredMod : CustomSpeechModifier
{
	public int drunkSpeechTime;
	public Mind playerMind;
	private void DoEffectTimeCheck()
	{
		if (drunkSpeechTime > 0)
		{
			//This is it used anywhere???
			//playerMind.inventorySpeechModifiers |= ChatModifier.Drunk;
			drunkSpeechTime--;
		}
		else
		{
			//This is it used anywhere???
			//playerMind.inventorySpeechModifiers &= ~ChatModifier.Drunk;
		}
	}
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
				slur = slur + x; //uuuuu
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

		if (DMMath.Prob(10))
		{
			int hicburptext = Random.Range(1, 4);
			switch (hicburptext)
			{
				case 1:
					hicburp = "- burp... ";
					break;
				case 2:
					hicburp = "- hic- ";
					break;
				case 3:
					hicburp = "- hic! ";
					break;
				case 4:
					hicburp = "- buuuurp... ";
					break;
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
		message = Regex.Replace(message, @"\B([aeiouslnmr]\B)", Slur);
		//	//Drunk people commonly extend their vowels and doubled letters, as well as hiccupping and burping infrequently.
		//	//Regex - find vowels and other commonly slurred letters, like L. How? I dunno.
		message = Regex.Replace(message, @" ", HicBurp);
		return message;
	}
}