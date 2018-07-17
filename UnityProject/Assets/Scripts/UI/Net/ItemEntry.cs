﻿using UnityEngine;
/// all server only
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
			Logger.Log( "ItemEntry: no prefab found, not doing init",Category.ItemEntry );
			return;
		}
		var itemAttributes = Prefab.GetComponent<ItemAttributes>();
		if ( !itemAttributes ) {
			Logger.LogWarning( $"No attributes found for prefab {Prefab}",Category.ItemEntry );
			return;
		}
		foreach ( var element in Elements ) {
			string nameBeforeIndex = element.name.Split( '_' )[0];
			switch ( nameBeforeIndex ) {
					case "ItemName":
						element.Value = itemAttributes.name;
						break;
					case "ItemIcon":
						element.Value = itemAttributes.gameObject.name; 
//						element.Value = itemAttributes.GetComponentInChildren<SpriteRenderer>()?.sprite.name; 
						break;
				}
		}
		Logger.Log( $"ItemEntry: Init success! Prefab={Prefab}, ItemName={itemAttributes.name}, ItemIcon={itemAttributes.gameObject.name}",Category.ItemEntry );
	}
}
