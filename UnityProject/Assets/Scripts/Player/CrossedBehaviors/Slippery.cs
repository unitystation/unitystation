using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slippery : MonoBehaviour
{
	private RegisterItem registerItem;
	private CustomNetTransform customNetTransform;

	private void Awake()
	{
		registerItem = GetComponent<RegisterItem>();
		customNetTransform = GetComponent<CustomNetTransform>();
	}

	private void OnEnable()
	{
		registerItem.crossed.AddListener(Slip);
	}

	private void OnDisable()
	{
		registerItem.crossed.RemoveListener(Slip);
	}

	private void Slip(RegisterPlayer registerPlayer)
	{
		if (MatrixManager.IsSpaceAt(registerItem.WorldPositionServer, true) || customNetTransform.IsBeingThrown)
		{
			return;
		}
		registerPlayer.Slip(true);
	}
}
