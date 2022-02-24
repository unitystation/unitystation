﻿using System.Linq;
using Items;
using UnityEngine;

namespace UI.Core.NetUI
{
	/// <summary>
	/// For storing Prefabs and not actual instances
	/// To be renamed into PrefabEntry
	/// All methods are serverside.
	/// </summary>
	public class ItemEntry : DynamicEntry
	{
		private GameObject prefab;

		public GameObject Prefab {
			get => prefab;
			set {
				prefab = value;
				ReInit();
			}
		}

		public void ReInit()
		{
			if (!Prefab)
			{
				Logger.Log("ItemEntry: no prefab found, not doing init", Category.NetUI);
				return;
			}
			var itemAttributes = Prefab.GetComponent<ItemAttributesV2>();
			if (itemAttributes != null)
			{
				Logger.LogWarning($"No attributes found for prefab {Prefab}", Category.NetUI);
				return;
			}
			foreach (var element in Elements.Cast<NetUIElement<string>>())
			{
				string nameBeforeIndex = element.name.Split(DELIMITER)[0];
				element.Value = nameBeforeIndex switch
				{
					"ItemName" => itemAttributes.name,
					"ItemIcon" => itemAttributes.gameObject.name,
					_ => string.Empty,
				};
			}
			Logger.Log(
					$"ItemEntry: Init success! Prefab={Prefab}, ItemName={itemAttributes.name}, ItemIcon={itemAttributes.gameObject.name}",
					Category.NetUI);
		}
	}
}
