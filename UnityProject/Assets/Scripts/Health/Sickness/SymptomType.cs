using System;
using UnityEngine;

namespace Health.Sickness
{
	/// <summary>
	/// The type of symptom applied at a particuliar stage of a sickness
	/// </summary>
	[Serializable]
	public enum SymptomType
	{
		[Tooltip("Completly heals the player.  But can get the sickness back")]
		Wellbeing,
		[Tooltip("Completly heals the player.  Sickness can't affect this player anymore.")]
		Immune,
		[Tooltip("Anyone in the vacinity will receive a message that the player just coughed.  Will spawn a contagion zone if the sickness is contagious.")]
		Cough,
		[Tooltip("Anyone in the vacinity will receive a message that the player just sneezed.  Will spawn a contagion zone if the sickness is contagious.")]
		Sneeze,
		[Tooltip("Player receive a random message from a list of specified messages.  All other player in vacinity will receive a public counterpart of the message.")]
		CustomMessage,
	}
}
