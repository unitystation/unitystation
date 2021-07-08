using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace HealthV2
{
	public class BodyPartUISlots : BodyPartModification, IDynamicItemSlotS
	{
		public NamedSlotFlagged NamedSlotFlagged;

		private DynamicItemStorage ItemStorage;
		public GameObject GameObject => gameObject;
		public ItemStorage RelatedStorage => relatedStorage;

		[SerializeField] [FormerlySerializedAs("RelatedStorage")]
		private ItemStorage relatedStorage;

		public List<BodyPartUISlots.StorageCharacteristics> Storage => storage;


		[SerializeField] [FormerlySerializedAs("Storage")]
		private List<BodyPartUISlots.StorageCharacteristics> storage;

		public override void RemovedFromBody(LivingHealthMasterBase livingHealthMasterBase)
		{
			var dynamicItemStorage = livingHealthMasterBase.GetComponent<DynamicItemStorage>();
			if (dynamicItemStorage != null)
			{
				dynamicItemStorage.Remove(this);
			}
		}

		public override void HealthMasterSet()
		{
			if (RelatedPart.HealthMaster == null) return;
			var dynamicItemStorage = RelatedPart.HealthMaster.GetComponent<DynamicItemStorage>();
			if (dynamicItemStorage != null)
			{
				dynamicItemStorage.Add(this);
			}
		}

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