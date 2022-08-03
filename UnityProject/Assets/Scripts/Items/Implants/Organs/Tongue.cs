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
			bodyPart = GetComponent<BodyPart>();
			mobLanguages = GetComponent<MobLanguages>();

			foreach (var language in languages)
			{
				mobLanguages.LearnLanguage(language, true);
			}
		}

		public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			foreach (var language in languages)
			{
				mobLanguages.RemoveLanguage(language, true);
			}
		}
	}
}