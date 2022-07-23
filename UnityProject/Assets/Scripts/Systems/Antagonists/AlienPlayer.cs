using System;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
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

		[SerializeField]
		private AlienTypes startingAlienType = AlienTypes.Larva1;

		//Used to generate names
		private static int alienCount;

		//Used to generate Queen names
		private static int queenCount;

		//Current alien data SO
		private AlienTypeDataSO currentData;
		private AlienTypes CurrentAlienType => currentData.AlienType;

		private AlienMode currentAlienMode;

		//Plasma value (increase by being on weeds)
		private int currentPlasma;
		public int CurrentPlasma => currentPlasma;

		private PlayerScript playerScript;
		private LivingHealthMasterBase livingHealthMasterBase;
		private Rotatable rotatable;

		#region LifeCycle

		private void Awake()
		{
			playerScript = GetComponent<PlayerScript>();
			livingHealthMasterBase = GetComponent<LivingHealthMasterBase>();
			rotatable = GetComponent<Rotatable>();
		}

		private void OnEnable()
		{
			UpdateManager.Add(OnUpdate, 1f);
			livingHealthMasterBase.OnConsciousStateChangeServer.AddListener(OnConsciousHealthChange);
			rotatable.OnRotationChange.AddListener(OnRotation);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, OnUpdate);
			livingHealthMasterBase.OnConsciousStateChangeServer.RemoveListener(OnConsciousHealthChange);
			rotatable.OnRotationChange.RemoveListener(OnRotation);
		}

		private void Start()
		{
			SetNewPlayer(startingAlienType);
		}

		#endregion

		#region Setup

		[Server]
		public void SetNewPlayer(AlienTypes newAlien)
		{
			if(isServer == false) return;

			Evolve(newAlien);

			if (currentData.AlienType == AlienTypes.Queen)
			{
				queenCount++;
				playerScript.playerName = $"{currentData.Name} {queenCount:D3}";
				return;
			}

			alienCount++;
			playerScript.playerName = $"{currentData.Name} {alienCount:D3}";
		}

		[ContextMenu("To Queen")]
		public void ToQueen()
		{
			SetNewPlayer(AlienTypes.Queen);
		}

		#endregion

		#region Update

		private void OnUpdate()
		{
			if(CustomNetworkManager.IsServer == false) return;

			if(currentData == null) return;

			//Dead...
			if(livingHealthMasterBase.IsDead) return;

			LarvaUpdate();
		}

		#endregion

		#region Larva

		private int growth;

		private void LarvaUpdate()
		{
			if (CurrentAlienType != AlienTypes.Larva1 && CurrentAlienType != AlienTypes.Larva2) return;

			if(currentPlasma <= 0) return;

			//If we are larva 1 or two then we need to continue growing to mature
			growth++;

			if(growth <= 100) return;
			growth = 0;

			Evolve(CurrentAlienType == AlienTypes.Larva1 ? AlienTypes.Larva2 : AlienTypes.Larva3);
		}

		#endregion

		#region Evolution

		private void Evolve(AlienTypes newAlien)
		{
			if (livingHealthMasterBase.IsDead)
			{
				Chat.AddExamineMsgFromServer(gameObject, "You are dead, you cannot evolve!");
				return;
			}

			if (livingHealthMasterBase.ConsciousState == ConsciousState.UNCONSCIOUS)
			{
				Chat.AddExamineMsgFromServer(gameObject, "You are unconscious, you cannot evolve!");
				return;
			}

			var typeFound = typesToChoose.Where(a => a.AlienType == newAlien).ToArray();
			if (typeFound.Length <= 0)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"Unable to evolve to {newAlien.ToString()}");
				Logger.LogError($"Could not find alien type: {newAlien.ToString()} in data list!");
				return;
			}

			currentData = typeFound[0];

			currentAlienMode = AlienMode.Normal;

			ChangeAlienSprite(currentAlienMode);
		}

		#endregion

		#region Healh

		private void OnConsciousHealthChange(ConsciousState oldState, ConsciousState newState)
		{
			if (newState == ConsciousState.DEAD)
			{
				OnDeath();
				return;
			}
		}

		#endregion

		#region Death

		private void OnDeath()
		{

		}

		#endregion

		#region Sprites

		private void ChangeAlienSprite(AlienMode newSprite)
		{
			mainBackSpriteHandler.PushClear();

			switch (newSprite)
			{
				case AlienMode.Normal:
					SetSpriteSO(currentData.Normal);
					return;
				case AlienMode.Dead:
					SetSpriteSO(currentData.Dead);
					return;
				case AlienMode.Pounce:
					SetSpriteSO(currentData.Pounce);
					return;
				case AlienMode.Sleep:
					SetSpriteSO(currentData.Sleep);
					return;
				case AlienMode.Unconscious:
					SetSpriteSO(currentData.Unconscious);
					return;
				case AlienMode.Running:
					SetSpriteSO(currentData.Running);
					return;
				case AlienMode.Crawling:
					SetSpriteSO(currentData.Front, true);
					return;
			}
		}

		private void SetSpriteSO(SpriteDataSO newSprite, bool doBack = false)
		{
			if (newSprite == null)
			{
				//Don't have custom sprite just use normal
				mainSpriteHandler.SetSpriteSO(currentData.Normal);
				return;
			}

			mainSpriteHandler.SetSpriteSO(newSprite);

			if(doBack == false) return;

			mainBackSpriteHandler.SetSpriteSO(currentData.Back);
		}

		private void OnRotation(OrientationEnum newRotation)
		{
			int spriteVariant = 0;
			switch (newRotation)
			{
				case OrientationEnum.Up_By0:
					spriteVariant = 1;
					break;
				case OrientationEnum.Right_By270:
					spriteVariant = 2;
					break;
				case OrientationEnum.Down_By180:
					spriteVariant = 0;
					break;
				case OrientationEnum.Left_By90:
					spriteVariant = 3;
					break;
			}

			mainSpriteHandler.ChangeSpriteVariant(spriteVariant, false);
			mainBackSpriteHandler.ChangeSpriteVariant(spriteVariant, false);
		}

		#endregion

		public enum AlienMode
		{
			Normal,
			Dead,
			Pounce,
			Sleep,
			Unconscious,
			Running,
			Crawling
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