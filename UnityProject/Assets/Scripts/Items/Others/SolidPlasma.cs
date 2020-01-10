using Mirror;
using System;
using UnityEngine;

public class SolidPlasma : NetworkBehaviour
{
	public float Amount { get; private set; } = 10f;
	private bool burningPlasma = false;
	private float burnSpeed = 1f;
	private Action fuelExhausted;

	[Server]
	public void StartBurningPlasma(float _burnSpeed, Action fuelExhaustedEvent)
	{
		fuelExhausted = fuelExhaustedEvent;

		if (Amount > 0f)
		{
			burningPlasma = true;
			burnSpeed = _burnSpeed;
			UpdateManager.Instance.Add(UpdateMe);
		}
		else
		{
			fuelExhausted.Invoke();
		}
	}

	[Server]
	public void StopBurningPlasma()
	{
		burningPlasma = false;
		if (UpdateManager.Instance != null)
		{
			UpdateManager.Instance.Remove(UpdateMe);
		}
	}

	void OnDisable()
	{
		if (burningPlasma)
		{
			UpdateManager.Instance.Remove(UpdateMe);
		}
	}

	//UpdateManager
	void UpdateMe()
	{
		if (burningPlasma)
		{
			Amount -= (0.05f * Time.deltaTime) * burnSpeed;
			if (Amount <= 0f)
			{
				burningPlasma = false;
				fuelExhausted.Invoke();
				if (UpdateManager.Instance != null)
				{
					UpdateManager.Instance.Remove(UpdateMe);
				}
			}
		}
	}
}