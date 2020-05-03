using System.ComponentModel;
using UnityEngine;

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
			Value = (Vector2)TrackedObject.transform.position - origin;
		}
		else if ( StaticPosition != TransformState.HiddenPos )
		{
			Value = (Vector2)StaticPosition - origin;
		} else {
			Position = TransformState.HiddenPos;
		}

	}

	/// <summary>
	/// Update entry's internal elements to inform peepers about tracked object updates,
	/// As this entry's value is simply its layout coordinates
	/// </summary>
	public void ReInit()
	{
		foreach (var element in Elements)
		{
			switch ( element ) {
				//can be expanded in the future
				case NetSpriteImage image:
					image.Value = Type.GetDescription();
					break;
				case NetRadiusCircle circle:
					circle.Value = Radius.ToString();
					break;
			}
		}
	}
}

public enum MapIconType {
[Description("")] None=-1,
[Description("MapIcons16x16@0")] Waypoint=0,
[Description("MapIcons16x16@1")] Ship=1,
[Description("MapIcons16x16@2")] Station=2,
[Description("MapIcons16x16@4")] Asteroids=4,
[Description("MapIcons16x16@5")] Unknown=5,
[Description("MapIcons16x16@9")] Airlock=9,
[Description("MapIcons16x16@10")] Singularity=10,
[Description("MapIcons16x16@16")] Ian=16,
[Description("MapIcons16x16@17")] Human=17,
[Description("MapIcons16x16@18")] Syndicate=18,
[Description("MapIcons16x16@19")] Carp=19,
[Description("MapIcons16x16@20")] Clown=20,
[Description("MapIcons16x16@21")] Disky=21,
[Description("MapIcons16x16@22")] Nuke=22,
}