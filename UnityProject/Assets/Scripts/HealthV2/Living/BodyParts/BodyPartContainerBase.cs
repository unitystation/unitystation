using System;
using NaughtyAttributes;
using UnityEngine;

namespace HealthV2
{
	public enum BodyPartType
	{
		Head,
		Chest,
		LeftArm,
		RightArm,
		LeftHand,
		RightHand,
		LeftLeg,
		RightLeg,
		Buttocks,
		Implant
	}

	public class BodyPartContainerBase : MonoBehaviour
	{
		[SerializeField]
		[Required("Need a health master to send updates too." +
		          "Will attempt to find a components in its parents if not already set in editor.")]
		private LivingHealthMasterBase healthMaster = null;

		private ItemStorage storage;

		[SerializeField]
		private BodyPartSprites bodyPartSprites;

		public event Action<ImplantBase, ImplantBase> ImplantUpdateEvent;
		void Awake()
		{
			storage = GetComponent<ItemStorage>();
			storage.ServerInventoryItemSlotSet += ImplantAdded;

			if (!healthMaster)
			{
				healthMaster = GetComponentInParent<LivingHealthMasterBase>();
			}

		}

		public virtual void ImplantAdded(Pickupable prevImplant, Pickupable newImplant)
		{
			if (newImplant)
			{
				ImplantBase implant = newImplant.GetComponent<ImplantBase>();
				healthMaster.AddNewImplant(implant);
				bodyPartSprites.UpdateSpritesForImplant(implant);
			}
			if (prevImplant)
			{
				ImplantBase implant = prevImplant.GetComponent<ImplantBase>();
				healthMaster.RemoveImplant(implant);
				bodyPartSprites.UpdateSpritesOnImplantRemoved(implant);
			}
		}
	}
}
