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
		registerItem.OnCrossed += Slip;
	}

	private void OnDisable()
	{
		registerItem.OnCrossed -= Slip;
	}

	private void Slip()
	{
		registerItem.CrossedRegisterPlayer.Stun(4f);
		SoundManager.PlayNetworkedAtPos("Slip", transform.position);
	}
}
