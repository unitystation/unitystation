using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ForceRotation : NetworkBehaviour
{
	[SyncVar] [HideInInspector]
	public Vector3 Rotation;

	private void OnEnable()
	{
		UpdateManager.Instance.Add(CheckRotation);
	}

	private void OnDisable()
	{
		if (UpdateManager.Instance != null)
		{
			UpdateManager.Instance.Remove(CheckRotation);
		}
	}

	public void CheckRotation()
	{
		//This also ensures object is facing the right way on matrix rot
		if (transform.eulerAngles != Rotation)
		{
			transform.eulerAngles = Rotation;
			CheckPlayerBlockingState();
		}
	}

	void CheckPlayerBlockingState()
	{
		var registerPlayer = GetComponent<RegisterPlayer>();

		if (registerPlayer != null)
		{
			if (transform.eulerAngles.z != 0)
			{
				registerPlayer.IsBlocking = false;
				SpriteRenderer[] spriteRends = GetComponentsInChildren<SpriteRenderer>();
				foreach (SpriteRenderer sR in spriteRends)
				{
					sR.sortingLayerName = "Blood";
				}
			}
			else
			{
				registerPlayer.IsBlocking = true;
				SpriteRenderer[] spriteRends = GetComponentsInChildren<SpriteRenderer>();
				foreach (SpriteRenderer sR in spriteRends)
				{
					sR.sortingLayerName = "Players";
				}
			}
		}
	}
}