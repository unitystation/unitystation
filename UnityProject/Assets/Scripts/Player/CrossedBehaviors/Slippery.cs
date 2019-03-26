using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slippery : MonoBehaviour
{
	private RegisterItem registerItem;

	private void Awake()
	{
		registerItem = GetComponent<RegisterItem>();
	}

	private void OnEnable()
	{
		registerItem.OnCrossed.AddListener(Slip);
	}

	private void OnDisable()
	{
		registerItem.OnCrossed.RemoveListener(Slip);
	}

	private void Slip()
	{
		if (MatrixManager.IsSpaceAt(transform.position.CutToInt()))
		{
			return;
		}
		registerItem.CrossedRegisterPlayer.Stun();
		SoundManager.PlayNetworkedAtPos("Slip", transform.position);
	}
}
