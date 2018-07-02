using System.ComponentModel;
using UnityEngine;
using Util;

/// all server only
public class RadarEntry : DynamicEntry {
	public MapIconType type = MapIconType.None;
	public MapIconType Type {
		get { return type; }
		set {
			type = value;
			ReInit();
		}
	}

	public GameObject TrackedObject;
	public Vector3 StaticPosition = TransformState.HiddenPos;
	public int Radius = -1;

	public void RefreshTrackedPos(Vector2 origin) {
		if ( TrackedObject && TrackedObject.transform.position != TransformState.HiddenPos ) {
//			Vector2 objectPos = (Vector2)TrackedObject.WorldPos() - origin; // WorldPos generates garbage :(
			Vector2 objectPos = (Vector2)TrackedObject.transform.position - origin;
			Value = (int)objectPos.x+"x"+(int)objectPos.y;
		} 
		else if ( StaticPosition != TransformState.HiddenPos ) 
		{
			Vector2 objectPos = (Vector2)StaticPosition - origin;
			Value = (int)objectPos.x+"x"+(int)objectPos.y;
		} else {
			Position = TransformState.HiddenPos;
		}
		
	}

	public void ReInit() {
		for ( var i = 0; i < Elements.Length; i++ ) {
			var element = Elements[i];
			string nameBeforeIndex = element.name.Split( '_' )[0];
			switch ( nameBeforeIndex ) {
				//can be expanded in the future
				case "MapIcon":
					string spriteValue = Type.GetDescription();
					element.Value = spriteValue;
					break;
				case "MapRadius":
					element.Value = Radius.ToString();
					break;
			}
		}

//		Debug.Log( $"ItemEntry: Init success! Prefab={Prefab}, ItemName={itemAttributes.name}, ItemIcon={itemAttributes.gameObject.name}" );
	}
}

public enum MapIconType {
[Description("")] None=-1,
[Description("MapIcons16x16_0")] Waypoint=0,
[Description("MapIcons16x16_1")] Ship=1,
[Description("MapIcons16x16_2")] Station=2,
[Description("MapIcons16x16_4")] Asteroids=4,
[Description("MapIcons16x16_5")] Unknown=5,
[Description("MapIcons16x16_9")] Airlock=9,
[Description("MapIcons16x16_10")] Singularity=10,
[Description("emoji_27")] Clown=16,
}