using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SubSceneManager : MonoBehaviour
{
	private static SubSceneManager subSceneManager;

	public static SubSceneManager Instance
	{
		get
		{
			if (subSceneManager == null)
			{
				subSceneManager = FindObjectOfType<SubSceneManager>();
			}

			return subSceneManager;
		}
	}

	[SerializeField] private AwayWorldListSO awayWorldList;
}
