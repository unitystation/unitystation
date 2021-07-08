using System.Linq;
using Items;
using UnityEngine;

/// <summary>
/// For storing Prefabs and not actual instances
/// To be renamed into PrefabEntry
/// All methods are serverside.
/// </summary>
public class ItemEntry : DynamicEntry {
	private GameObject prefab;

	public GameObject Prefab {
		get { return prefab; }
		set {
			prefab = value;
			ReInit();
		}
	}

	public void ReInit() {
		if ( !Prefab ) {
			Logger.Log( "ItemEntry: no prefab found, not doing init",Category.NetUI );
			return;
		}
		var itemAttributes = Prefab.GetComponent<ItemAttributesV2>();
		if ( itemAttributes != null) {
			Logger.LogWarning( $"No attributes found for prefab {Prefab}",Category.NetUI );
			return;
		}
		foreach ( var element in Elements.Cast<NetUIElement<string>>() ) {
			string nameBeforeIndex = element.name.Split( DELIMITER )[0];
			switch ( nameBeforeIndex ) {
					case "ItemName":
						element.Value = (itemAttributes as Component).name;
						break;
					case "ItemIcon":
						element.Value = (itemAttributes as Component).gameObject.name;
//						element.Value = itemAttributes.GetComponentInChildren<SpriteRenderer>()?.sprite.name;
						break;
				}
		}
		Logger.Log( $"ItemEntry: Init success! Prefab={Prefab}, ItemName={(itemAttributes as Component).name}, ItemIcon={(itemAttributes as Component).gameObject.name}",Category.NetUI );
	}
}
