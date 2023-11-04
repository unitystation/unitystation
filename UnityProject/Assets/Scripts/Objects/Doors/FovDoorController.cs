using UnityEngine;
using Core.Editor.Attributes;


namespace Doors
{
	/// <summary>
	/// Controls FoV visibility, at the moment it is using messages from FieldOfViewStencil to switch it
	/// </summary>
	public class FovDoorController : MonoBehaviour
	{

		public Material normalMat;

		public Material greyScaleMat;

		public Material fovMaskMat;

		private SpriteRenderer tileSpriteRenderer;

		private SpriteRenderer[] cacheRends;

		void Awake()
		{
			cacheRends = GetComponentsInChildren<SpriteRenderer>(true);
			CheckIfWindowed();
		}

		void CheckIfWindowed()
		{
			//windowed = 18, Door Opened = 16
			if (gameObject.layer == 18
				|| gameObject.layer == 16)
			{
				SetForWindowDoor();
			}
		}

		void SetForWindowDoor()
		{
			//Apply mask mat and turn off this component
			for (int i = 0; i < cacheRends.Length; i++)
			{
				cacheRends[i].material = fovMaskMat;
			}
			this.enabled = false;
		}
	}
}
