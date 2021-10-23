using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;
using ScriptableObjects;
using UI.Core.Windows;
using UI.Windows;
using Systems.Teleport;
using AdminCommands;
using Effects;


namespace UI.Systems.Ghost
{
	public class UI_GhostOptions : MonoBehaviour
	{
		[SerializeField]
		private Text ghostHearText = null;
		[SerializeField, BoxGroup("Ghost Role Button")]
		private AnimateIcon ghostRoleAnimator = default;
		[SerializeField, BoxGroup("Ghost Role Button")]
		private SpriteHandler ghostRoleSpriteHandler = default;

		private TeleportWindow TeleportWindow => UIManager.TeleportWindow;
		private GhostRoleWindow GhostRoleWindow => UIManager.GhostRoleWindow;

		public GameObject AdminGhostInventory;

		private bool roleBtnAnimating = false;

		private void OnEnable()
		{
			TeleportWindow.onTeleportRequested += TeleportUtils.TeleportLocalGhostTo;
			TeleportWindow.onTeleportToVector += TeleportUtils.TeleportLocalGhostTo;
			DetermineGhostHearText();
		}

		private void OnDisable()
		{
			TeleportWindow.onTeleportRequested -= TeleportUtils.TeleportLocalGhostTo;
			TeleportWindow.onTeleportToVector -= TeleportUtils.TeleportLocalGhostTo;
		}

		public void JumpToMob()
		{
			TeleportWindow.SetWindowTitle("Jump To Mob");
			TeleportWindow.gameObject.SetActive(true);
			TeleportWindow.GenerateButtons(TeleportUtils.GetMobDestinations());
		}

		public void Orbit() { }

		public void ReenterCorpse()
		{
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdGhostCheck();
		}

		public void Teleport()
		{
			TeleportWindow.SetWindowTitle("Jump to Place");
			TeleportWindow.gameObject.SetActive(true);
			TeleportWindow.GenerateButtons(TeleportUtils.GetSpawnDestinations());
		}

		public void GhostRoleBtn()
		{
			GhostRoleWindow.gameObject.SetActive(!GhostRoleWindow.gameObject.activeSelf);
		}

		public void Respawn()
		{
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRespawnPlayer();
			Camera.main.GetComponent<CameraEffects.CameraEffectControlScript>().EnsureAllEffectsAreDisabled();
		}

		public void ToggleAllowCloning()
		{
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleAllowCloning();
		}

		public void ToggleGhostHearRange()
		{
			Chat.Instance.GhostHearAll = !Chat.Instance.GhostHearAll;
			DetermineGhostHearText();
		}

		public void NewGhostRoleAvailable(GhostRoleData role)
		{
			ghostRoleSpriteHandler.SetSpriteSO(role.Sprite, networked: false);
			if (roleBtnAnimating) return; // Drop rapid subsequent notifications

			StartCoroutine(GhostRoleNotify(role));
		}

		private void DetermineGhostHearText()
		{
			ghostHearText.text = Chat.Instance.GhostHearAll ? "HEAR\r\n \r\nLOCAL" : "HEAR\r\n \r\nALL";
		}

		private IEnumerator GhostRoleNotify(GhostRoleData role)
		{
			roleBtnAnimating = true;

			Chat.AddExamineMsgToClient($"<size=48>Ghost role <b>{role.Name}</b> is available!</size>");
			_ = SoundManager.Play(CommonSounds.Instance.Notice2);
			ghostRoleAnimator.TriggerAnimation();

			yield return WaitFor.Seconds(5);
			ghostRoleSpriteHandler.ChangeSprite(0, networked: false);

			roleBtnAnimating = false;
		}

		public void AdminGhostInventoryDrop()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			if (PlayerManager.PlayerScript != null)
			{
				AdminCommandsManager.Instance.CmdAdminGhostDropItem();
			}
		}

		public void AdminGhostInvSmash()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			if (PlayerManager.PlayerScript != null)
			{
				AdminCommandsManager.Instance.CmdAdminGhostSmashItem();
			}
		}
	}
}
