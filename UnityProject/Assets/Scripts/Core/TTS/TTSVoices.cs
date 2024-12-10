using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TTSVoices
{
	public static List<string> Voices = new List<string>()
	{
		"Male 01",
		"Male 02",
		"Male 03",
		"Male 04",
		"Male 05",
		"Male 06",
		"Male 07",
		"Male 08",
		"Male 09",
		"Male 10",
		"Male 11",
		"Male 12",
		"Male 13",
		"Male 14",
		"Male 15",
		"Male 16",
		"Male 17",
		"Male 18",
		"Male 19",
		"Male 20",
		"Male 21",
		"Male 22",
		"Male 23",
		"Male 24",
		"Male 25",
		"Male 26",
		"Male 27",
		"Male 28",
		"Male 29",
		"Male 30",
		"Male 31",
		"Male 32",
		"Male 33",
		"Female 01",
		"Female 02",
		"Female 03",
		"Female 04",
		"Female 05",
		"Female 06",
		"Female 07",
		"Female 08",
		"Female 09",
		"Female 10",
		"Female 11",
		"Female 12",
		"Female 13",
		"Female 14",
		"Female 15",
		"Female 16",
		"Female 17",
		"Female 18",
		"Female 19",
		"Female 20",
		"Female 21",
		"Female 22",
		"Female 23",
		"Female 24",
		"Female 25",
		"Female 26",
		"Female 27",
		"Female 28",
		"Female 29",
		"Female 30",
		"Female 31",
		"Female 32"
	};

	public static string CurrentDefaultVoice
	{

		get
		{
			if (currentDefaultVoice == "")
			{
				currentDefaultVoice = GetDefaultPreference(true);
			}

			return currentDefaultVoice;
		}

	}

	private static string currentDefaultVoice = "";

	public static string GetDefaultPreference(bool save = false)
	{
		var Prefere=  PlayerPrefs.GetString(PlayerPrefKeys.DefaultVoice, Voices.First());
		if (Voices.Contains(Prefere) == false)
		{
			Prefere = Voices.First();
			SetSystemTTS(Prefere);
		}

		if (save)
		{
			SetSystemTTS(Prefere);
		}

		return Prefere;
	}

	public static void SetSystemTTS(string Preference)
	{
		PlayerPrefs.SetString(PlayerPrefKeys.DefaultVoice, Preference);
		currentDefaultVoice = Preference;
	}



}
