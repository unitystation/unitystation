using System;
using System.Collections.Generic;
using System.Text;
using Mirror;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Managers;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.Systems.Explosions;
using US13.UI.Systems.MainHUD.UI_Bottom;
using US13.UI.Systems.Tooltips.HoverTooltips;
using Util;
using Random = UnityEngine.Random;

namespace US13.Objects.Wallmounts
{
	public class ClockWallmount : NetworkBehaviour, IExaminable, IHoverTooltip, IEmpAble, ICheckedInteractable<HandApply>
	{
		[SyncVar] private DateTime UST;

		[SerializeField] private AddressableAudioSource tickSound;

		private bool messedWith = false;
		private const float TICK_TIME = 1.85f;

		private void Start()
		{
			if (CustomNetworkManager.IsServer == false) return;
			InGameTimeManager.Instance.OnUpdateTime += SetCorrectTime;
			UpdateManager.Add(PlaySound, TICK_TIME);
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer == false) return;
			InGameTimeManager.Instance.OnUpdateTime -= SetCorrectTime;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PlaySound);
		}

		private void PlaySound()
		{
			SoundManager.PlayNetworkedAtPos(tickSound, gameObject.AssumedWorldPosServer());
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			var report = new StringBuilder();
			report.AppendLine($"UST currently is: {UST}");
			report.AppendLine($"UTC currently is: {InGameTimeManager.Instance.UtcTime}");
			return report.ToString();
		}

		public string HoverTip()
		{
			return "There's a small analog screen below the arms that displays the date in more detail.";
		}

		public string CustomTitle() => null;

		public Sprite CustomIcon() => null;

		public List<Sprite> IconIndicators() => null;

		public List<TextColor> InteractionsStrings()
		{
			List<TextColor> interactions = new List<TextColor>();
			TextColor text = new TextColor
			{
				Text = "Shift+Left Click: Read time.",
				Color = IntentColors.Help
			};
			interactions.Add(text);
			return interactions;
		}

		[Server]
		private void MessWithMagnetTime()
		{
			UST = UST.AddHours(Random.Range(1, 5));
		}

		[Server]
		private void SetCorrectTime()
		{
			UST = InGameTimeManager.Instance.UniversalSpaceTime;
		}

		public void OnEmp(int EmpStrength)
		{
			MessWithMagnetTime();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.Intent != Intent.Help)
			{
				return false;
			}

			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			messedWith = !messedWith;
			var msg = messedWith ? "messed with the time" : "corrected the time";
			if (messedWith)
			{
				MessWithMagnetTime();
			}
			else
			{
				SetCorrectTime();
			}
			Chat.AddExamineMsg(interaction.Performer, $"You {msg}.");
		}
	}
}