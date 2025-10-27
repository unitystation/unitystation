using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class TranslationSystem
{
	public static bool English = true;

	public static List<string> AvailableLanguages = new List<string>();

	public static Dictionary<string, Dictionary<string, string>> LoadedComplexLanguage = new Dictionary<string, Dictionary<string, string>>();

	public static Dictionary<string, string> LoadedLanguage = new Dictionary<string, string>();
	public static string Translate(string EN_String, string foreverID = "", string FieldName = "")
	{


		if (English)
		{
			return EN_String;
		}

		if (LoadedLanguage.TryGetValue(EN_String, out var Translation))
		{
			return Translation;
		}
		else if (string.IsNullOrEmpty(foreverID) ==false)
		{
			if (LoadedComplexLanguage.TryGetValue(foreverID, out var Anobject))
			{
				return Anobject[FieldName];
			}
		}

		/*/

		English = true;
		LoadedLanguage["Microwave"] = "Microdon";
		LoadedLanguage["Completely dead"] = "Wedi marw'n llwyr";
		LoadedLanguage["{0} Looks like they are {1}\n"] = "{0} Yn edrych fel eu bod {1}\n";

		English = true;
		LoadedComplexLanguage = new Dictionary<string, Dictionary<string, string>>();
		LoadedComplexLanguage["MiscFunctions_RRT"] = new Dictionary<string, string>();
		LoadedComplexLanguage["MiscFunctions_RRT"]["txt"] = "{0} Yn edrych fel eu bod {1}";
		LoadedComplexLanguage["MiscFunctions_RRT"]["targetName"] = "Microdon";
		LoadedComplexLanguage["MiscFunctions_RRT"]["ConsciousState"] = "Wedi marw'n llwyr";
		/*/

		return EN_String;
	}
}


public static class TS
{
	public static string T(string EN_String, string foreverID = "", string FieldName= "")
	{
		return TranslationSystem.Translate(EN_String, foreverID, FieldName);
	}

	public static string C(string EN_String , string FieldName = "",[CallerFilePath] string caller = "")
	{
		if (string.IsNullOrEmpty(FieldName) == false)
		{
			string className = System.IO.Path.GetFileNameWithoutExtension(caller);
			return TranslationSystem.Translate(EN_String, className, FieldName );
		}
		else
		{
			return TranslationSystem.Translate(EN_String, "", FieldName );
		}
	}
}
