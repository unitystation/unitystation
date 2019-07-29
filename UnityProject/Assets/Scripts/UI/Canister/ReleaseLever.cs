
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main logic for the release lever.
/// </summary>
public class ReleaseLever : MonoBehaviour
{
	//the shadow on the upper part of the lever
	public Shadow upperShadow;
	public float ShadowDistance = 10;

	public void OnToggled(bool isOpen)
	{
		//fix the shadow and rotate
		if (isOpen)
		{
			transform.rotation = Quaternion.Euler(0,0,90);
			upperShadow.effectDistance = new Vector2(-ShadowDistance, -ShadowDistance);
		}
		else
		{
			transform.rotation = Quaternion.identity;
			upperShadow.effectDistance = new Vector2(ShadowDistance, -ShadowDistance);
		}
	}

}
