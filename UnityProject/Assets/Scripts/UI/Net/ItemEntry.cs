using UnityEngine;
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
			TADB_Debug.Log( "ItemEntry: no prefab found, not doing init",TADB_Debug.Category.ItemEntry.ToString() );
			return;
		}
		var itemAttributes = Prefab.GetComponent<ItemAttributes>();
		if ( !itemAttributes ) {
			TADB_Debug.LogWarning( $"No attributes found for prefab {Prefab}",TADB_Debug.Category.ItemEntry.ToString() );
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
		TADB_Debug.Log( $"ItemEntry: Init success! Prefab={Prefab}, ItemName={itemAttributes.name}, ItemIcon={itemAttributes.gameObject.name}",TADB_Debug.Category.ItemEntry.ToString() );
	}
}
