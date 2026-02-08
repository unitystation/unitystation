using System.Collections;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;
using US13.Core.Addressables;
using US13.Core.Camera;
using US13.Core.Chat;
using US13.Core.Sprite_Handler;
using US13.Effects;
using US13.Managers;
using US13.Messages.Client.GhostRoles;
using US13.Player;
using US13.ScriptableObjects;
using US13.Strings;
using US13.Systems.GhostRoles;
using US13.UI.Core.Windows.TeleportWindow;
using US13.UI.Systems.MainHUD.UI_Bottom.Ghost;

namespace US13.UI.Systems.MainHUD.UI_Bottom
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
			TeleportWindow.onTeleportToVectorWorld += TeleportUtils.TeleportGhostToWorldPosition;
			DetermineGhostHearText();
			RequestAvailableGhostRolesMessage.SendMessage();
		}

		private void OnDisable()
		{
			TeleportWindow.onTeleportRequested -= TeleportUtils.TeleportLocalGhostTo;
			TeleportWindow.onTeleportToVectorWorld -= TeleportUtils.TeleportGhostToWorldPosition;
		}

		public void JumpToMob()
		{
			TeleportWindow.SetWindowTitle("Jump To Mob");
			TeleportWindow.OrbitOnTeleport = false;
			TeleportWindow.gameObject.SetActive(true);
			TeleportWindow.GenerateButtons(TeleportUtils.GetMobDestinations());
		}

		public void Orbit()
		{
			TeleportWindow.SetWindowTitle("Orbit a Mob");
			TeleportWindow.OrbitOnTeleport = true;
			TeleportWindow.gameObject.SetActive(true);
			TeleportWindow.GenerateButtons(TeleportUtils.GetMobDestinations());
		}

		public void ReenterCorpse()
		{
			PlayerManager.LocalMindScript.CmdGhostCheck();
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
			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRespawnPlayer();
			Camera.main.GetComponent<CameraEffectControlScript>().EnsureAllEffectsAreDisabled();
		}

		public void ToggleAllowCloning()
		{
			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdToggleAllowCloning();
		}

		public void ToggleGhostHearRange()
		{
			Chat.Instance.GhostHearAll = !Chat.Instance.GhostHearAll;
			DetermineGhostHearText();
		}

		public void NewGhostRoleAvailable(GhostRoleData role, GhostRoleClient clientrole)
		{
			if (gameObject.activeSelf == false) return;
			ghostRoleSpriteHandler.SetSpriteSO(role.Sprite, networked: false);
			if (roleBtnAnimating) return; // Drop rapid subsequent notifications

			if (clientrole != null)
			{
				clientrole.OnTimerExpired += UpdateIcon;
			}


			StartCoroutine(GhostRoleNotify(role));
		}

		public void UpdateIcon()
		{

			if (GhostRoleManager.Instance.clientAvailableRoles.Count == 0)
			{
				ghostRoleSpriteHandler.SetCatalogueIndexSprite(0, networked: false);
				roleBtnAnimating = false;
				return;
			}

			var firstOrDefault = GhostRoleManager.Instance.clientAvailableRoles.OrderByDescending(x => x.Key).FirstOrDefault();
			var data = firstOrDefault.Value;
			NewGhostRoleAvailable(GhostRoleManager.Instance.GhostRoles[data.RoleListIndex], data);
		}

		private void DetermineGhostHearText()
		{
			ghostHearText.text = Chat.Instance.GhostHearAll ? "HEAR\r\n \r\nLOCAL" : "HEAR\r\n \r\nALL";
		}

		private IEnumerator GhostRoleNotify(GhostRoleData role)
		{
			roleBtnAnimating = true;

			Chat.AddExamineMsgToClient($"<size={ChatTemplates.LargeText}>Ghost role <b>{role.Name}</b> is available!</size>");
			_ = SoundManager.Play(CommonSounds.Instance.Notice2);
			ghostRoleAnimator.TriggerAnimation();

			yield return WaitFor.Seconds(5);

			roleBtnAnimating = false;
		}

		public void AdminGhostInventoryDrop()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			if (PlayerManager.LocalPlayerScript != null)
			{
				AdminCommandsManager.Instance.CmdAdminGhostDropItem();
			}
		}

		public void AdminGhostInvSmash()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			if (PlayerManager.LocalPlayerScript != null)
			{
				AdminCommandsManager.Instance.CmdAdminGhostSmashItem();
			}
		}
	}
}
