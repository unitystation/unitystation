using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;

namespace UI.Core.GUI.Components
{
	/// <summary>
	/// UI click raycasts will ignore transparent areas of the image. (Why isn't that Image field exposed to editor?)
	/// </summary>
	public class RaycastOpacity : MonoBehaviour
	{
		[InfoBox("For this to work, the sprite used by the Image must have readable pixels." +
				"This can be achieved by enabling Read/Write enabled in the advanced Texture Import Settings " +
				"for the sprite and disabling atlassing for the sprite.")]

		[SerializeField, Range(0, 1)]
		[Tooltip("0 to allow raycasts anywhere, 1 to only register raycast hits on completely opaque pixels.")]
		private float alphaThreshold = 1f;

		private void OnEnable()
		{
			GetComponent<Image>().alphaHitTestMinimumThreshold = alphaThreshold;
		}

		private void OnDisable()
		{
			GetComponent<Image>().alphaHitTestMinimumThreshold = 0;
		}
	}
}
