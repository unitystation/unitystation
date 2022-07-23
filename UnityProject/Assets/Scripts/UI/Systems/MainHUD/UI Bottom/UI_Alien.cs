using System;
using System.Collections.Generic;
using System.Linq;
using Systems.Antagonists;
using UI.Core.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Systems.MainHUD.UI_Bottom
{
	public class UI_Alien : MonoBehaviour
	{
		private AlienPlayer alienPlayer = null;

		private AlienMouseInputController controller = null;

		[SerializeField]
		private List<SpriteDataSO> healthSprites = new List<SpriteDataSO>();

		[SerializeField]
		private AnimatedImage healthSpriteRender = null;


		[SerializeField]
		private AnimatedImage queenFinder = null;

		[SerializeField]
		private List<SpriteDataSO> queenFinderSprites = new List<SpriteDataSO>();

		public void SetUp(AlienPlayer player)
		{
			alienPlayer = player;
			controller = alienPlayer.GetComponent<AlienMouseInputController>();
		}

		private void OnEnable()
		{
			UpdateManager.Add(OnUpdate, 1);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, OnUpdate);
		}

		private void OnUpdate()
		{
			if(alienPlayer == null) return;
			if(alienPlayer.isLocalPlayer == false) return;

			HealthCheck();

			QueenCheck();
		}

		private void HealthCheck()
		{
			if (alienPlayer.LivingHealthMasterBase.IsDead)
			{
				healthSpriteRender.SetSprite(healthSprites[8]);
				return;
			}

			if (alienPlayer.LivingHealthMasterBase.IsCrit)
			{
				healthSpriteRender.SetSprite(healthSprites[7]);
				return;
			}

			healthSpriteRender.SetSprite(healthSprites[Mathf.RoundToInt(alienPlayer.LivingHealthMasterBase.HealthPercentage() / 16.7f)]);
		}

		private void QueenCheck()
		{
			var queens =
				FindObjectsOfType<AlienPlayer>().Where(x =>
					x.IsDead == false && x.CurrentAlienType == AlienPlayer.AlienTypes.Queen).ToArray();

			if (queens.Length == 0)
			{
				queenFinder.gameObject.SetActive(false);
				return;
			}

			queenFinder.gameObject.SetActive(true);

			var closest = queens.OrderBy(x =>
				(x.RegisterPlayer.ObjectPhysics.Component.OfficialPosition - alienPlayer.RegisterPlayer.ObjectPhysics.Component.OfficialPosition).magnitude).ToArray()[0];

			var distance = (closest.RegisterPlayer.ObjectPhysics.Component.OfficialPosition -
			             alienPlayer.RegisterPlayer.ObjectPhysics.Component.OfficialPosition).magnitude;

			if (distance <= 5)
			{
				//Set to Closest arrow, doesnt have a direction
				queenFinder.SetSprite(queenFinderSprites[0]);
				return;
			}

			if (distance <= 15)
			{
				//Set to semi close arrow
				queenFinder.SetSprite(queenFinderSprites[1]);
			}
			else if (distance <= 30)
			{
				//Set to far arrow
				queenFinder.SetSprite(queenFinderSprites[2]);
			}
			else if (distance <= 50)
			{
				//Set to very far arrow
				queenFinder.SetSprite(queenFinderSprites[3]);
			}

			//How annoying the direction variants are set up like this
			//South, North, East, West, South East, South West, North East, North West

			var angle = Orientation.AngleFromUp(closest.RegisterPlayer.ObjectPhysics.Component.OfficialPosition -
			             alienPlayer.RegisterPlayer.ObjectPhysics.Component.OfficialPosition);

			switch (angle)
			{
				case var n when n.IsBetween(337.5f, 22.5f):
					//North
					queenFinder.SetVariant(1);
					return;
				case var n when n.IsBetween(22.5f, 67.5f):
					//North East
					queenFinder.SetVariant(6);
					return;
				case var n when n.IsBetween(67.5f, 112.5f):
					//East
					queenFinder.SetVariant(2);
					return;
				case var n when n.IsBetween(112.5f, 157.5f):
					//South East
					queenFinder.SetVariant(4);
					return;
				case var n when n.IsBetween(157.5f, 202.5f):
					//South
					queenFinder.SetVariant(0);
					return;
				case var n when n.IsBetween(202.5f, 247.5f):
					//South West
					queenFinder.SetVariant(5);
					return;
				case var n when n.IsBetween(247.5f, 292.5f):
					//West
					queenFinder.SetVariant(3);
					return;
				case var n when n.IsBetween(292.5f, 337.5f):
					//North West
					queenFinder.SetVariant(7);
					return;
				default:
					Logger.LogError($"Angle was: {angle} degrees, no case for it!");
					return;
			}
		}
	}
}