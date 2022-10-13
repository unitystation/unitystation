using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using UnityEngine;

namespace Learning.ProtipObjectTypes
{
	public class ProtipObjectOnPickupTrait : ProtipObject
	{
		public List<TraitProtips> protipsForTraits;

		/// <summary>
		/// Dictionaries are not supported in the unity editor for some reason.
		/// </summary>
		[Serializable]
		public class TraitProtips
		{
			public ItemTrait Trait;
			public ProtipSO Tip;
		}

		private void OnEnable()
		{
			PlayerManager.LocalPlayerScript.DynamicItemStorage.OnContentsChangeClient.AddListener(InventoryChange);
		}

		private void OnDisable()
		{
			PlayerManager.LocalPlayerScript.DynamicItemStorage.OnContentsChangeClient.RemoveListener(InventoryChange);
		}


		private void InventoryChange()
		{
			var handslot = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot();
			if(handslot == null || handslot.IsEmpty || handslot.ItemAttributes.GetTraits().Count() == 0) return;
			foreach (var trait in protipsForTraits)
			{
				if(handslot.ItemAttributes.GetTraits().Any(x => x == trait.Trait) == false) continue;
				TriggerTip(trait.Tip);
			}
		}
	}
}