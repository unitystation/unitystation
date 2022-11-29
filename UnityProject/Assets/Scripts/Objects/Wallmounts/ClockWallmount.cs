using System;
using System.Collections.Generic;
using System.Text;
using AddressableReferences;
using Managers;
using Messages.Server.SoundMessages;
using Mirror;
using Systems.Explosions;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Objects.Wallmounts
{
	public class ClockWallmount : NetworkBehaviour, IExaminable, IHoverTooltip, IEmpAble, ICheckedInteractable<HandApply>
	{
		[SyncVar] private DateTime UST;

		[SerializeField] private AddressableAudioSource tickSound;

		private bool messedWith = false;
		private const float TICK_TIME = 1.75f;

		private void Awake()
		{
			InGameTimeManager.Instance.OnUpdateTime += SetCorrectTime;
			UpdateManager.Add(PlaySound, TICK_TIME);
		}

		private void OnDisable()
		{
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

		public string CustomTitle()
		{
			return null;
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return null;
		}

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