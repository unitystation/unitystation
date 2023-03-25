using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Items.Implants.Organs
{
	public class BodyPartUISlots : MonoBehaviour, IDynamicItemSlotS
	{
		private DynamicItemStorage ItemStorage;

		[CanBeNull] public GameObject GameObject
		{
			get
			{
				if (this == null) return null;
				return gameObject;
			}
		}

		public ItemStorage RelatedStorage => relatedStorage;

		[SerializeField] [FormerlySerializedAs("RelatedStorage")]
		private ItemStorage relatedStorage;

		public List<BodyPartUISlots.StorageCharacteristics> Storage => storage;


		[SerializeField] [FormerlySerializedAs("Storage")]
		private List<BodyPartUISlots.StorageCharacteristics> storage;

		public int InterfaceGetInstanceID => GetInstanceID();

		[System.Serializable]
		public class StorageCharacteristics
		{
			public bool NotPresentOnUI;
			public UI_SlotManager.SlotArea SlotArea;
			public NamedSlot namedSlot;
			public string hoverName;
			public bool DropContents;
			public Sprite placeholderSprite;
			public bool Conditional;
			[ShowIf(nameof(Conditional))] public Conditional Condition;
			[NonSerialized] public IDynamicItemSlotS RelatedIDynamicItemSlotS;
			[NonSerialized] public int IndexInList = 0;
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