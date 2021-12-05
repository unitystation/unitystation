using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Managers;
using ScriptableObjects;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;
using Objects.Science;
using TileManagement;

namespace Systems.Scenes
{
	public class LavaLandManager : SingletonManager<LavaLandManager>
	{

		//temp stuff, allows for maps to have a teleport to lava land mapped if they want it.:
		/// <summary>
		/// Temp until shuttle landings possible
		/// </summary>
		[HideInInspector] public QuantumPad LavaLandBase2;

		/// <summary>
		/// Temp until shuttle landings possible
		/// </summary>
		[HideInInspector] public QuantumPad LavaLandBase1;

		/// <summary>
		/// Temp until shuttle landings possible
		/// </summary>
		[HideInInspector] public QuantumPad LavaLandBase1Connector;

		/// <summary>
		/// Temp until shuttle landings possible
		/// </summary>
		[HideInInspector] public QuantumPad LavaLandBase2Connector;

		public List<LavaLandRandomAreaSO> areaSOs = new List<LavaLandRandomAreaSO>();

		public LavaLandRandomAreaSO GetCorrectSOFromSize(AreaSizes size)
		{
			foreach (var areaSO in areaSOs)
			{
				if (areaSO.AreaSize == size)
				{
					return areaSO;
				}
			}

			return null;
		}

		//Temp until shuttle landings
		public void SetQuantumPads()
		{
			if (LavaLandBase1 != null && LavaLandBase1Connector != null)
			{
				LavaLandBase1.connectedPad = LavaLandBase1Connector;
				LavaLandBase1Connector.connectedPad = LavaLandBase1;
			}

			if (LavaLandBase2 != null && LavaLandBase2Connector != null)
			{
				LavaLandBase2.connectedPad = LavaLandBase2Connector;
				LavaLandBase2Connector.connectedPad = LavaLandBase2;
			}
		}
	}
}
