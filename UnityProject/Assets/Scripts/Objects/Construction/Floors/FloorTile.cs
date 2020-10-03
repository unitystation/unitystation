using UnityEngine;

namespace Objects.Construction
{
	[ExecuteInEditMode]
	public class FloorTile : MonoBehaviour
	{
		public GameObject ambientTile;
		public GameObject fireScorch;

		private void Start()
		{
			CheckAmbientTile();
		}

		public void CheckAmbientTile()
		{
			if (ambientTile == null)
			{
				ambientTile = Instantiate(Resources.Load("AmbientTile") as GameObject, transform.position,
					Quaternion.identity, transform);
			}
		}

		public void CleanTile()
		{
			if (fireScorch != null)
			{
				fireScorch.transform.parent = null;
				Despawn.ClientSingle(fireScorch);
			}
		}
	}
}
