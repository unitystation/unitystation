<<<<<<< HEAD
﻿using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using Objects;

namespace HealthV2
{
	public class BodyPartContainerBase : MonoBehaviour, IServerInventoryMove
	{
		[SerializeField]
		[Required("Need a health master to send updates too." +
		          "Will attempt to find a components in its parents if not already set in editor.")]
		private LivingHealthMasterBase healthMaster = null;

		private ItemStorage storage;
		private void Awake()
		{
			storage = GetComponent<ItemStorage>();

			if (!healthMaster)
			{
				healthMaster = GetComponentInParent<LivingHealthMasterBase>();
			}
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			//If the implant is being added, or transfered to the storage.
			if (info.InventoryMoveType == InventoryMoveType.Add || (info.InventoryMoveType == InventoryMoveType.Transfer && storage.HasSlot(info.ToSlot.SlotIdentifier)))
			{
				//Note: This assumes the moved item will have the implant base component.
				//I would not normally make this kind of assumption, especially one without any checks.
				//However, if this happens and returns a null component, that really is an error. In which case
				//we want to be logging it as such anyway.
				healthMaster.AddNewImplant(info.MovedObject.GetComponent<ImplantBase>());
			}
			else //I'm pretty sure the only other option is removal?
			{
				healthMaster.RemoveImplant(info.MovedObject.GetComponent<ImplantBase>());
			}
		}
	}

=======
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Objects;

public class BodyPartContainerBase : ItemStorage
{
>>>>>>> 0bdef99d26... Added Storage Containers for Organs/Implants
}
