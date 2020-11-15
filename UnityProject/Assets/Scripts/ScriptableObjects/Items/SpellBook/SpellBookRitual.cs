using System;
using System.Collections;
using UnityEngine;
using NaughtyAttributes;
using InGameEvents;

namespace ScriptableObjects.Items.SpellBook
{
	/// <summary>
	/// An ritual-type entry for a wizard's Book of Spells.
	/// </summary>
	[CreateAssetMenu(fileName = "SpellBookRitual", menuName = "ScriptableObjects/Items/SpellBook/Ritual")]
	[Serializable]
	public class SpellBookRitual : SpellBookEntry
	{
		[SerializeField]
		private new string name = default;
		[SerializeField]
		private InGameEventType eventType = default;
		[Tooltip("The index of the event to trigger (in the event type), as found in InGameEventsManager.")]
		[SerializeField]
		private int eventIndex = default;
		[SerializeField]
		private string invocationMessage = default;
		[SerializeField]
		private string castSound = default;

		public string Name => name;
		public InGameEventType EventType => eventType;
		public int EventIndex => eventIndex;
		public string InvocationMessage => invocationMessage;
		public string CastSound => castSound;

		public enum RitualType
		{
			Event = 0,
			CustomImplementation,
		}
	}
}
