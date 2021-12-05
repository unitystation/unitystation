using System;
using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

namespace Systems.Scenes
{
	public class LavaLandAreaSpawnerScript : MonoBehaviour
	{
		public LavaLandGenerator LavaLandGenerator;

		public AreaSizes Size;

		public bool allowSpecialSites;

		private void Start()
		{
			LavaLandGenerator.SpawnScripts.Add(this, Size);
		}
	}
}
