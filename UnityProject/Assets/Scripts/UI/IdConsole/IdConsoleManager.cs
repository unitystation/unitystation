using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdConsoleManager : MonoBehaviour
{
	public List<JobType> IgnoredJobs = new List<JobType>();
	public List<IdAccessCategory> AccessCategories = new List<IdAccessCategory>();
	private static IdConsoleManager instance;

	public static IdConsoleManager Instance
	{
		get
		{
			if (instance == null)
			{
				instance = FindObjectOfType<IdConsoleManager>();
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