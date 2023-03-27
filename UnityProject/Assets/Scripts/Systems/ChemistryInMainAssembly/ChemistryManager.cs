using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScriptableObjects;
using UnityEngine;

public class ChemistryManager : MonoBehaviour
{
	[SerializeField] private ChemistryReagentsSO reagentsSo;
	private static bool generatedReferences = false;


	public void Awake()
	{
		if (generatedReferences == false)
		{
			new Task(reagentsSo.GenerateReagentReactionReferences).Start();
			generatedReferences = true;
		}
	}


}
