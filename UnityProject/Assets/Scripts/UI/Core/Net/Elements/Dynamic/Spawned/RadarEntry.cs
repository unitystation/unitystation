using UnityEngine;
using UI.Core.NetUI;

namespace UI.Objects.Shuttles
{
	/// all server only
	public class RadarEntry : DynamicEntry
	{
		[Tooltip("Assign the component responible for the icon.")]
		[SerializeField]
		private NetSpriteImage icon = default;

		[Tooltip("Assign the NetRadiusCircle component.")]
		[SerializeField]
		private NetRadiusCircle circle = default;

		private MapIconType type = MapIconType.None;
		public MapIconType Type {
			get => type;
			set {
				type = value;
				ReInit();
			}
		}

		public GameObject TrackedObject;
		public int Radius = -1;

		public void RefreshTrackedPos(Vector2 origin)
		{
			if (TrackedObject && TrackedObject.transform.position != TransformState.HiddenPos)
			{
				Value = (Vector2)TrackedObject.transform.position - origin;
			}
			else
			{
				Position = TransformState.HiddenPos;
			}

			UpdatePeepers();
		}

		/// <summary>
		/// Update entry's internal elements to inform peepers about tracked object updates,
		/// As this entry's value is simply its layout coordinates
		/// </summary>
		public void ReInit()
		{
			icon.SetSprite((int)Type);
			circle.MasterSetValue(Radius.ToString());
		}
	}

	// Aligns with the sprites set in NetSpriteImage component of ShuttleControlEntry.
	public enum MapIconType
	{
		None = -1,
		Waypoint = 0,
		Ship = 1,
		Station = 2,
		Asteroids = 4,
		Unknown = 5,
		Airlock = 9,
		Singularity = 10,
		Ian = 16,
		Human = 17,
		Syndicate = 18,
		Carp = 19,
		Clown = 20,
		Disky = 21,
		Nuke = 22,
	}
}
