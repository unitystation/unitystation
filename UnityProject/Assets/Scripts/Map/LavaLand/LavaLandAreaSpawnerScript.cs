using System;
using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

namespace Systems.Scenes
{
	public class LavaLandAreaSpawnerScript : MonoBehaviour
	{
		public AreaSizes Size;

		public bool allowSpecialSites;

		private void Start()
		{
			LavaLandManager.Instance.SpawnScripts.Add(this, Size);
		}
	}
}
