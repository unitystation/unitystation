using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls loading screens (except for start up scene)
/// </summary>
public class LoadingScreenManager : MonoBehaviour
{
	private static LoadingScreenManager _loadingScreenManager;

	public static LoadingScreenManager Instance
	{
		get
		{
			if (_loadingScreenManager == null)
			{
				_loadingScreenManager = FindObjectOfType<LoadingScreenManager>();
			}

			return _loadingScreenManager;
		}
	}

	[SerializeField] private GameObject loadingScreen;
}
