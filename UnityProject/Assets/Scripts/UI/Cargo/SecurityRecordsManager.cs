using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecurityRecordsManager : MonoBehaviour
{
	public List<SecurityRecord> SecurityRecords = new List<SecurityRecord>();

	private static SecurityRecordsManager instance;
	
	public static SecurityRecordsManager Instance
	{
		get
		{
			if (instance == null)
			{
				instance = FindObjectOfType<SecurityRecordsManager>();
			}
			return instance;
		}
	}

}
