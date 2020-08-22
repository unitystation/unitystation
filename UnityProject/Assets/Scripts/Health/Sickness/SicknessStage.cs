using System;
using UnityEngine;

namespace Assets.Scripts.Health.Sickness
{
	/// <summary>
	/// Sickness stage
	/// </summary>
	[Serializable]
	public class SicknessStage
	{
		/// <summary>
		/// Symptom the player develop at this stage of the sickness
		/// </summary>
		public SymptomType Symptom;

		/// <summary>
		/// Indicates if a symptom should be repeated once in a while.
		/// For symptoms such as hiccup, cough, sneeze, vomit, etc.
		/// </summary>
		public bool RepeatSymptom;

		/// <summary>
		/// For repeatable symptoms.  This is the minimum time (in seconds) the game waits before applying the effect once more
		/// </summary>
		public int RepeatMinDelay = 5;

		/// <summary>
		/// For repeatable symptoms.  This is the maximum time (in seconds) the game waits before applying the effect once more
		/// </summary>
		public int RepeatMaxDelay = 60;

		/// <summary>
		/// The number of seconds before the sickness progress to the next stage
		/// </summary>
		public int SecondsBeforeNextStage = 300;

		[SerializeField]
		private BaseSymptomParameter symptomParameter;

		public BaseSymptomParameter SymptomParameter
		{
			get
			{
				if (symptomParameter == null)
				{
					switch (Symptom)
					{
						case SymptomType.CustomMessage:
							symptomParameter = ScriptableObject.CreateInstance<CustomMessageParameter>();
							break;
					}
				}

				return symptomParameter;
			}
		}
	}
}
