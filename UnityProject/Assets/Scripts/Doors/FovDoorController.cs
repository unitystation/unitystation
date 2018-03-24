using UnityEngine;

namespace Doors
{
	/// <summary>
	/// Controls FoV visibility, at the moment it is using messages from FieldOfViewStencil to switch it
	/// </summary>
	public class FovDoorController : MonoBehaviour
	{
		public GameObject fovTileSprite;
		private SpriteRenderer tileSpriteRenderer;

		void Awake()
		{
			GameObject fTile = Instantiate(fovTileSprite, transform.position, Quaternion.identity);
			fTile.transform.parent = transform;
			fTile.transform.localPosition = Vector3.zero;
			tileSpriteRenderer = fTile.GetComponent<SpriteRenderer>();
		}

		//Broadcast msg from FieldOfViewStencil:
		public void TurnOnDoorFov(){
			tileSpriteRenderer.enabled = true;
		}

		public void TurnOffDoorFov(){
			tileSpriteRenderer.enabled = false;
		}
	}
}
