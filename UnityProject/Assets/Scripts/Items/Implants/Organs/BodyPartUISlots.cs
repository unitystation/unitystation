using System.Collections.Generic;
using JetBrains.Annotations;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace HealthV2
{
	public class BodyPartUISlots : MonoBehaviour, IDynamicItemSlotS
	{
		public NamedSlotFlagged NamedSlotFlagged;

		private DynamicItemStorage ItemStorage;

		[CanBeNull] public GameObject GameObject => gameObject;

		public ItemStorage RelatedStorage => relatedStorage;

		[SerializeField] [FormerlySerializedAs("RelatedStorage")]
		private ItemStorage relatedStorage;

		public List<BodyPartUISlots.StorageCharacteristics> Storage => storage;


		[SerializeField] [FormerlySerializedAs("Storage")]
		private List<BodyPartUISlots.StorageCharacteristics> storage;

		[System.Serializable]
		public struct StorageCharacteristics
		{
			public bool NotPresentOnUI;
			public UI_SlotManager.SlotArea SlotArea;
			public NamedSlot namedSlot;
			public string hoverName;
			public bool DropContents;
			public Sprite placeholderSprite;
			public bool Conditional;
			[ShowIf(nameof(Conditional))] public Conditional Condition;
		}


		[System.Serializable]
		public struct Conditional
		{
			public string CategoryID;
			public ConditionalParameter ConditionalParameter;
			public int XAmountConditional;
		}

		public enum ConditionalParameter
		{
			OnlyAllowOne,
			RequireX
		}
	}
}