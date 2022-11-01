using System.Collections.Generic;
using Player.Language;
using UnityEngine;

namespace HealthV2
{
	public class Tongue : BodyPartFunctionality
	{
		private MobLanguages mobLanguages;

		[SerializeField]
		private List<LanguageSO> languages = new List<LanguageSO>();

		public override void AddedToBody(LivingHealthMasterBase livingHealth)
		{
			RelatedPart = GetComponent<BodyPart>();
			mobLanguages = livingHealth.GetComponent<MobLanguages>();

			if(CustomNetworkManager.IsServer == false) return;

			foreach (var language in languages)
			{
				mobLanguages.LearnLanguage(language, true);
			}
		}

		public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			if(CustomNetworkManager.IsServer == false) return;

			foreach (var language in languages)
			{
				//Don't remove the language if it is in the default list
				if(mobLanguages.DefaultLanguages != null && mobLanguages.DefaultLanguages.UnderstoodLanguages.Contains(language)) continue;

				if(language.Flags.HasFlag(LanguageFlags.TonguelessSpeech)) continue;

				//Can no longer speak, but can still understand
				mobLanguages.RemoveLanguage(language);
			}
		}
	}
}