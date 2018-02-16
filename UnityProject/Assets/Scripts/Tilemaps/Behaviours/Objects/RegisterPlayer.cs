using UnityEngine;

namespace Tilemaps.Behaviours.Objects
{
	[ExecuteInEditMode]
	public class RegisterPlayer : RegisterTile
	{
		public bool IsBlocking { get; set; } = true;

		public override bool IsPassable()
		{
			return !IsBlocking;
		}

		void OnTriggerEnter2D(Collider2D coll)
		{
			if (coll.gameObject.layer == 24) {
				Debug.Log("PlayerEntered Matrix: " + coll.gameObject.transform.parent.name);
			}
		}
	}
}