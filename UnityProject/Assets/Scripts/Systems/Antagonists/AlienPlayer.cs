using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using ScriptableObjects;
using UnityEngine;

namespace Systems.Antagonists
{
	public class AlienPlayer : NetworkBehaviour
	{
		[SerializeField]
		private SpriteHandler mainSpriteHandler;

		[SerializeField]
		private SpriteHandler mainBackSpriteHandler;

		[SerializeField]
		private List<AlienTypeDataSO> typesToChoose = new List<AlienTypeDataSO>();

		//Used to generate names
		private static int alienCount;

		//Used to generate Queen names
		private static int queenCount;

		//Current alien data SO
		private AlienTypeDataSO currentData;

		private PlayerScript playerScript;

		private void Awake()
		{
			playerScript = GetComponent<PlayerScript>();
		}

		[Server]
		public void SetNewAlienType(AlienTypes newAlien)
		{
			if(isServer == false) return;

			var typeFound = typesToChoose.Where(a => a.AlienType == newAlien).ToArray();
			if (typeFound.Length <= 0)
			{
				Logger.LogError($"Could not find alien type: {newAlien} in data list!");
				return;
			}

			currentData = typeFound[0];

			if (currentData.AlienType == AlienTypes.Queen)
			{
				queenCount++;
				playerScript.playerName = $"{currentData.AlienType.ToString()} {queenCount:D3}";
				return;
			}

			alienCount++;
			playerScript.playerName = $"{currentData.AlienType.ToString()} {alienCount:D3}";
		}

		[ContextMenu("To Queen")]
		public void ToQueen()
		{
			SetNewAlienType(AlienTypes.Queen);
		}

		public enum AlienTypes
		{
			//Three larva stages
			Larva1,
			Larva2,
			Larva3,

			Hunter,
			Sentinel,
			Praetorian,
			Drone,

			//God Save the Queen!
			Queen
		}
	}
}