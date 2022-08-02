using System.Collections.Generic;
using UnityEngine;

namespace Player.Language
{
	[CreateAssetMenu(fileName = "DefaultLanguageGroupSO", menuName = "ScriptableObjects/Player/DefaultLanguageGroupSO")]
	public class DefaultLanguageGroupSO : ScriptableObject
	{
		[SerializeField]
		[Tooltip("The languages that can be understood")]
		private List<LanguageSO> understoodLanguages = new List<LanguageSO>();
		public List<LanguageSO> UnderstoodLanguages => understoodLanguages;

		[SerializeField]
		[Tooltip("The languages that can be spoken")]
		private List<LanguageSO> spokenLanguages = new List<LanguageSO>();
		public List<LanguageSO> SpokenLanguages => spokenLanguages;

		[SerializeField]
		[Tooltip("The languages that can never be understood/spoken")]
		private List<LanguageSO> blockedLanguages  = new List<LanguageSO>();
		public List<LanguageSO> BlockedLanguages => blockedLanguages;
	}
}