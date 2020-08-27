using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Machines
{
	[CreateAssetMenu(fileName = "MachineParts", menuName = "ScriptableObjects/MachineParts", order = 1)]
	public class MachineParts : ScriptableObject
	{
		[Serializable]
		public class MachinePartList
		{
			public ItemTrait itemTrait;// The machine part needed to build machine

			public GameObject basicItem; //Basic item prefab used for when deconstructing mapped machines.

			public int amountOfThisPart = 1; // Amount of that part
		}

		public GameObject machine;// Machine which will be spawned
		
		//PM: Below is commented out because I've decided to make all the machines use appropriate machine board .prefabs instead of the blank board.
		/*
		public string NameOfCircuitBoard;

		public string DescriptionOfCircuitBoard;
		*/

		public MachinePartList[] machineParts;
	}
}