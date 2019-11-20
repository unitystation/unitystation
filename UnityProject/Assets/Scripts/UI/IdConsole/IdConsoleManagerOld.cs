using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Only used for old ID console system for stress testing nettabs.
/// </summary>
public class IdConsoleManagerOld : MonoBehaviour
{
	public List<JobType> IgnoredJobs = new List<JobType>();
	public List<IdAccessCategory> AccessCategories = new List<IdAccessCategory>();
	private static IdConsoleManagerOld instance;

	public static IdConsoleManagerOld Instance
	{
		get
		{
			if (instance == null)
			{
				instance = FindObjectOfType<IdConsoleManagerOld>();
			}
			return instance;
		}
	}
}

[System.Serializable]
public class IdAccessCategory
{
	public string CategoryName;
	public List<IdAccess> IdAccessList;
	public Color CategoryColor;
	public Color CategoryPressedColor;
}

[System.Serializable]
public class IdAccess
{
	public string AccessName;
	public Access RelatedAccess;
}