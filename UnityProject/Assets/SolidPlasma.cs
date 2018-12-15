using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SolidPlasma : NetworkBehaviour
{
	public float Amount { get; private set; } = 20;
	private bool burnPlasma = false;
	private float burnSpeed = 1f;

	void OnEnable()
	{
		UpdateManager.Instance.Add(UpdateMe);
	}

	void OnDisable()
	{
		if (UpdateManager.Instance != null)
		{
			UpdateManager.Instance.Remove(UpdateMe);
		}
	}

	[Server]
	public void StartBurningPlasma(float _burnSpeed)
	{
		if (Amount > 0f)
		{
			burnPlasma = true;
			burnSpeed = _burnSpeed;
		}
	}

	[Server]
	public void StopBurningPlasma()
	{
		burnPlasma = false;
	}

	//UpdateManager
	void UpdateMe()
	{
		if (burnPlasma)
		{
			Amount -= (0.05f * Time.deltaTime) * burnSpeed;
			if (Amount <= 0f)
			{
				burnPlasma = false;
			}
		}
	}
}