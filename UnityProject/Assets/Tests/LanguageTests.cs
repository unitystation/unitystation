using System.Collections.Generic;
using System.Linq;
using Managers;
using NUnit.Framework;
using Player.Language;
using Systems.CraftingV2;

namespace Tests
{
	public class LanguageTests
	{
		[Test]
		public void CheckLanguageSOs()
		{
			var report = new TestReport();

			var languageManager = Utils.GetManager<LanguageManager>("LanguageManager");

			if (languageManager == null)
			{
				report.Fail().Append("Failed to find LanguageManager!").Log().AssertPassed();
				return;
			}

			var languages = Utils.FindAssetsByType<LanguageSO>().ToList();

			foreach (var language in languages)
			{
				if(languageManager.AllLanguages.Contains(language)) continue;

				report.Fail().AppendLine($"{language.LanguageName} is not in the Language Manager AllLanguages list!");
			}

			var usedId = new Dictionary<ushort, LanguageSO>();

			foreach (var language in languages)
			{
				var id = language.LanguageUniqueId;

				if (id == 0)
				{
					report.Fail().AppendLine($"{language.LanguageName} unique Id is: ({id}), 0 is not to be used!");
					continue;
				}

				if (usedId.TryGetValue(id, out var value) == false)
				{
					usedId.Add(id, language);
					continue;
				}

				report.Fail().AppendLine($"{language.LanguageName} unique Id: ({id}) is already in use by {value.LanguageName}!");
			}

			report.Log().AssertPassed();
		}

		[Test]
		public void CheckLanguageScramble()
		{
			var report = new TestReport();

			var languageManager = Utils.GetManager<LanguageManager>("LanguageManager");

			if (languageManager == null)
			{
				report.Fail().Append("Failed to find LanguageManager!").Log().AssertPassed();
				return;
			}

			var languages = Utils.FindAssetsByType<LanguageSO>().ToList();

			var testing = "Hello, hello, this is a testing string";

			foreach (var language in languages)
			{
				var scrambled = languageManager.TestScramble(language, string.Copy(testing));

				if(scrambled != testing) continue;

				report.Fail().Append($"Failed to scramble with {language.LanguageName}");
			}

			report.Log().AssertPassed();
		}
	}
}