using System.Collections;
using System.Collections.Generic;
using Managers;
using UnityEngine;

public class CentralCommandBeacon : MonoBehaviour
{
	public GuidanceBuoy CentCommGuidanceBuoy;
	private CentComm CentComm;

	void Start()
	{
		CentComm = GameManager.Instance.GetComponent<CentComm>();
		CentComm.CentCommGuidanceBuoy = CentCommGuidanceBuoy;
	}
}