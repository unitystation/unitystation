using UnityEngine;

public class ItemEntry : DynamicEntry {
	public GameObject Prefab;
	public override void Init() {
		var itemAttributes = Prefab?.GetComponent<ItemAttributes>();
		if ( !itemAttributes ) {
			Debug.LogWarning( $"No attributes found for prefab {Prefab}" );
			return;
		}
		foreach ( var element in Elements ) {
			string nameBeforeIndex = element.name.Split( '_' )[0];
			switch ( nameBeforeIndex ) {
					case "ItemName":
						element.Value = itemAttributes.name;
						break;
					case "ItemIcon":
						//todo: figure out how to pass sprite via string && NetUIImage component
						element.Value = itemAttributes.gameObject.name; 
//						element.Value = itemAttributes.GetComponentInChildren<SpriteRenderer>()?.sprite.name; 
						break;
				}
		}
	}
	//todo: setPrefab?
}
