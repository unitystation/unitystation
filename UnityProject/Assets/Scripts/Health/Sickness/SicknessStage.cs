using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Health.Sickness
{
	/// <summary>
	/// Sickness stage
	/// </summary>
	[Serializable]
	public class SicknessStage: ISerializationCallbackReceiver
	{
		/// <summary>
		/// Symptom the player develop at this stage of the sickness
		/// </summary>
		[SerializeField]
		private SymptomType symptom = SymptomType.Wellbeing;

		/// <summary>
		/// Indicates if a symptom should be repeated once in a while.
		/// For symptoms such as hiccup, cough, sneeze, vomit, etc.
		/// </summary>
		[SerializeField]
		private bool repeatSymptom = true;

		/// <summary>
		/// For repeatable symptoms.  This is the minimum time (in seconds) the game waits before applying the effect once more
		/// </summary>
		[SerializeField]
		private int repeatMinDelay = 5;

		/// <summary>
		/// For repeatable symptoms.  This is the maximum time (in seconds) the game waits before applying the effect once more
		/// </summary>
		[SerializeField]
		private int repeatMaxDelay = 60;

		/// <summary>
		/// The number of seconds before the sickness progress to the next stage
		/// </summary>
		[SerializeField]
		private int secondsBeforeNextStage = 300;

		[SerializeField]
		private string extendedSymptomParametersSerialized = string.Empty;

		/// <summary>
		/// This is for extended symptom parameters that should derive from BaseSymptomParameter Abstract Class.
		/// </summary>
		private BaseSymptomParameter extendedSymptomParameters;

		/// <summary>
		/// Symptom the player develop at this stage of the sickness
		/// </summary>
		public SymptomType Symptom
		{
			get
			{
				return symptom;
			}
			set
			{
				symptom = value;
			}
		}

		/// <summary>
		/// Indicates if a symptom should be repeated once in a while.
		/// For symptoms such as hiccup, cough, sneeze, vomit, etc.
		/// </summary>
		public bool RepeatSymptom
		{
			get
			{
				return repeatSymptom;
			}
			set
			{
				repeatSymptom = value;
			}
		}

		/// <summary>
		/// For repeatable symptoms.  This is the minimum time (in seconds) the game waits before applying the effect once more
		/// </summary>
		public int RepeatMinDelay
		{
			get
			{
				return repeatMinDelay;
			}
			set
			{
				repeatMinDelay = value;
			}
		}

		/// <summary>
		/// For repeatable symptoms.  This is the maximum time (in seconds) the game waits before applying the effect once more
		/// </summary>
		public int RepeatMaxDelay
		{
			get
			{
				return repeatMaxDelay;
			}
			set
			{
				repeatMaxDelay = value;
			}
		}

		/// <summary>
		/// The number of seconds before the sickness progress to the next stage
		/// </summary>
		public int SecondsBeforeNextStage
		{
			get
			{
				return secondsBeforeNextStage;
			}
			set
			{
				secondsBeforeNextStage = value;
			}
		}

		/// <summary>
		/// This is for extended symptom parameters that should derive from BaseSymptomParameter Abstract Class.
		/// </summary>
		public BaseSymptomParameter ExtendedSymptomParameters
		{
			get
			{
				if (extendedSymptomParameters == null)
				{
					switch (symptom)
					{
						case SymptomType.CustomMessage:
							extendedSymptomParameters = new CustomMessageParameter();							
							((CustomMessageParameter)extendedSymptomParameters).CustomMessages.Add(new CustomMessage());
							break;
					}
				}

				return extendedSymptomParameters;
			}
		}

		public void OnAfterDeserialize()
		{
			if (!string.IsNullOrWhiteSpace(extendedSymptomParametersSerialized))
			{
				switch (symptom)
				{
					case SymptomType.CustomMessage:
						extendedSymptomParameters = JsonConvert.DeserializeObject<CustomMessageParameter>(extendedSymptomParametersSerialized);
						break;
				}
			}
			else
				extendedSymptomParameters = null;
		}

		public void OnBeforeSerialize()
		{
			extendedSymptomParametersSerialized = JsonConvert.SerializeObject(ExtendedSymptomParameters);
		}
	}
}
