using UnityEngine;

/// <summary>
/// Controls FoV visibility, at the moment it is using messages from FieldOfViewStencil to switch it
/// </summary>
public class FovDoorController : MonoBehaviour
{
	public Material normalMat;
	public Material greyScaleMat;

	private SpriteRenderer tileSpriteRenderer;

	private SpriteRenderer[] cacheRends;

	void Awake()
	{
		cacheRends = GetComponentsInChildren<SpriteRenderer>(true);
		TurnOnDoorFov();
	}

	//Broadcast msg from FieldOfViewStencil:
	public void TurnOnDoorFov()
	{
		for (int i = 0; i < cacheRends.Length; i++)
		{
			cacheRends[i].material = greyScaleMat;
		}
	}

	public void TurnOffDoorFov()
	{
		for (int i = 0; i < cacheRends.Length; i++)
		{
			cacheRends[i].material = normalMat;
		}
	}
}

