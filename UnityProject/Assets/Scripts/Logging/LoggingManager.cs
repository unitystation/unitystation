using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class LoggingManager : NetworkBehaviour
{
	public static LoggingManager Instance;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}
}
