using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Systems.Antagonists;
using TMPro;
using UI.Core.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Systems.MainHUD.UI_Bottom
{
	public class UI_Alien : MonoBehaviour
	{
		private AlienPlayer alienPlayer;
		public AlienPlayer AlienPlayer => alienPlayer;

		private AlienMouseInputController controller;

		[SerializeField]
		private SpriteHandler healthSpriteRender = null;

		[SerializeField]
		private SpriteHandler queenFinder = null;

		[SerializeField]
		private TMP_Text plasmaText = null;

		[SerializeField]
		private GameObject evolveMenu = null;

		[SerializeField]
		private GameObject hiveMenu = null;

		[SerializeField]
		private TMP_InputField queenAnnounceText = null;

		[SerializeField]
		private GameObject queenAnnounceMenu = null;

		public void SetUp(AlienPlayer player)
		{
			alienPlayer = player;
			controller = alienPlayer.GetComponent<AlienMouseInputController>();
		}

		public void TurnOff()
		{
			evolveMenu.SetActive(false);
			hiveMenu.SetActive(false);
			queenAnnounceMenu.SetActive(false);
			gameObject.SetActive(false);
		}

		private bool focusCheck;

		#region focus Check
		void UpdateMe()
		{
			if (queenAnnounceText.isFocused && focusCheck == false)
			{
				InputFocus();
			}
			else if (queenAnnounceText.isFocused == false && focusCheck)
			{
				InputUnfocus();
			}
		}

		private void InputFocus()
		{
			focusCheck = true;
			//disable keyboard commands while input is focused
			UIManager.IsInputFocus = true;
		}
		private void InputUnfocus()
		{
			focusCheck = false;
			//disable keyboard commands while input is focused
			UIManager.IsInputFocus = false;
		}

		#endregion

		#region Lifecycle

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			UpdateManager.Add(OnUpdate, 1);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, OnUpdate);
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		#endregion

		#region OnUpdate

		private void OnUpdate()
		{
			if(alienPlayer == null) return;
			if(alienPlayer.hasAuthority == false) return;

			HealthCheck();

			QueenCheck();

			PlasmaCheck();
		}

		#endregion

		#region Health Check

		private void HealthCheck()
		{
			if (alienPlayer.LivingHealthMasterBase.IsDead)
			{
				healthSpriteRender.ChangeSprite(7, false);
				return;
			}

			if (alienPlayer.LivingHealthMasterBase.IsCrit)
			{
				healthSpriteRender.ChangeSprite(6, false);
				return;
			}

			healthSpriteRender.ChangeSprite(Mathf.RoundToInt(Mathf.Clamp((alienPlayer.LivingHealthMasterBase.HealthPercentage() / 16.7f) - 1, 0, 5)), false);
		}

		private void QueenCheck()
		{
			var queens =
				FindObjectsOfType<AlienPlayer>().Where(x =>
					x.IsDead == false && x.CurrentAlienType == AlienPlayer.AlienTypes.Queen
					&& x.gameObject != alienPlayer.gameObject).ToArray();

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
				queenFinder.ChangeSprite(0, false);
				return;
			}

			if (distance <= 15)
			{
				//Set to semi close arrow
				queenFinder.ChangeSprite(1, false);
			}
			else if (distance <= 30)
			{
				//Set to far arrow
				queenFinder.ChangeSprite(2, false);
			}
			else if (distance <= 50)
			{
				//Set to very far arrow
				queenFinder.ChangeSprite(3, false);
			}

			//How annoying the direction variants are set up like this
			//South, North, East, West, South East, South West, North East, North West

			var angle = Orientation.AngleFromUp(closest.RegisterPlayer.ObjectPhysics.Component.OfficialPosition -
			             alienPlayer.RegisterPlayer.ObjectPhysics.Component.OfficialPosition);

			switch (angle)
			{
				case var n when n.IsBetween(337.5f, 360f) || n.IsBetween(0f, 22.5f):
					//North
					queenFinder.ChangeSpriteVariant(1, false);
					return;
				case var n when n.IsBetween(22.5f, 67.5f):
					//North East
					queenFinder.ChangeSpriteVariant(6, false);
					return;
				case var n when n.IsBetween(67.5f, 112.5f):
					//East
					queenFinder.ChangeSpriteVariant(2, false);
					return;
				case var n when n.IsBetween(112.5f, 157.5f):
					//South East
					queenFinder.ChangeSpriteVariant(4, false);
					return;
				case var n when n.IsBetween(157.5f, 202.5f):
					//South
					queenFinder.ChangeSpriteVariant(0, false);
					return;
				case var n when n.IsBetween(202.5f, 247.5f):
					//South West
					queenFinder.ChangeSpriteVariant(5, false);
					return;
				case var n when n.IsBetween(247.5f, 292.5f):
					//West
					queenFinder.ChangeSpriteVariant(3, false);
					return;
				case var n when n.IsBetween(292.5f, 337.5f):
					//North West
					queenFinder.ChangeSpriteVariant(7, false);
					return;
				default:
					Loggy.LogError($"Angle was: {angle} degrees, no case for it!");
					return;
			}
		}

		#endregion

		private void PlasmaCheck()
		{
			plasmaText.text = $"{alienPlayer.CurrentPlasmaPercentage}%";
		}

		public void OpenEvolveMenu()
		{
			evolveMenu.SetActive(true);
		}

		public void OpenHiveMenu()
		{
			hiveMenu.SetActive(true);
		}

		public void OpenQueenAnnounceMenu()
		{
			queenAnnounceMenu.SetActive(true);
		}

		public void OnQueenAnnounce()
		{
			if (alienPlayer.OnCoolDown(NetworkSide.Client, alienPlayer.QueenAnnounceCooldown))
			{
				Chat.AddExamineMsgToClient("Your telepathy nerves need recharging!");
				return;
			}
			alienPlayer.StartCoolDown(NetworkSide.Client, alienPlayer.QueenAnnounceCooldown);

			alienPlayer.CmdQueenAnnounce(queenAnnounceText.text);

			queenAnnounceMenu.SetActive(false);
		}

		public void OnEvolve(AlienPlayer.AlienTypes evolveTo)
		{
			if (alienPlayer.CurrentPlasmaPercentage.Approx(100) == false)
			{
				Chat.AddExamineMsgToClient("Not enough plasma to evolve!");
				return;
			}

			//TODO maybe check for growth here on client too?

			alienPlayer.CmdEvolve(evolveTo);

			evolveMenu.SetActive(false);
		}
	}
}