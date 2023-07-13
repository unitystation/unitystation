using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScriptableObjects;
using UnityEngine;

public class ChemistryManager : MonoBehaviour
{
	private static bool generatedReferences = false;
	public void Awake()
	{
#if UNITY_EDITOR
		generatedReferences = false;
#endif

		if (generatedReferences == false)
		{
			new Task(ChemistryReagentsSO.Instance.GenerateReagentReactionReferences).Start();
			generatedReferences = true;
		}
	}


}
