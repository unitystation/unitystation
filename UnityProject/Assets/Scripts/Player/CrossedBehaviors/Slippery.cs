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
		registerItem.crossed.AddListener(Slip);
	}

	private void OnDisable()
	{
		registerItem.crossed.RemoveListener(Slip);
	}

	private void Slip(RegisterPlayer registerPlayer)
	{
		if (MatrixManager.IsSpaceAt(registerItem.WorldPosition))
		{
			return;
		}
		registerPlayer.Stun();
		SoundManager.PlayNetworkedAtPos("Slip", registerItem.Position);
	}
}
