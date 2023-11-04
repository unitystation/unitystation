using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AddressableReferences;
using Items;

namespace Objects.Drawers
{
	/// <summary>
	/// Morgue component for morgue objects. Adds additional function to the base Drawer component.
	/// TODO: Consider using hacking to replace/complement the screwdriver and emag.
	/// TODO: Add spark VFX with emag interaction.
	/// </summary>
	public class Morgue : Drawer
	{
		// Extra states over the base DrawerState enum.
		private enum MorgueState
		{
			/// <summary> Yellow morgue lights. </summary>
			ShutWithItems = 2,
			/// <summary> Green morgue lights. </summary>
			ShutWithBraindead = 3,
			/// <summary> Red morgue lights. </summary>
			ShutWithPlayer = 4
		}

		[SerializeField] private AddressableAudioSource emaggedSound;

		[SerializeField] private AddressableAudioSource buzzerToggleSound;

		[SerializeField] private AddressableAudioSource consciousnessAlarmSound;

		// Whether the morgue alarm should sound if a consciousness is present.
		private const bool ALARM_SYSTEM_ENABLED = true;
		// Whether the morgue alarm can be toggled. The LED display will still show red if a consciousness is present.
		private const bool ALLOW_BUZZER_TOGGLING = true;
		// Whether the morgue can be emagged. Permanently breaks the display and alarm (useful for hiding corpses in plain sight).
		private const bool ALLOW_EMAGGING = true;
		// Delay between alarm sounds, in seconds.
		private const int ALARM_PERIOD = 5;

		private bool ConsciousnessPresent => players.Any(player => player.Mind != null && player.Mind.IsOnline() && player.Mind.CurrentPlayScript == player );
		private bool buzzerEnabled = ALARM_SYSTEM_ENABLED;
		private bool alarmBroken = false;
		private bool alarmRunning = false;

		private List<PlayerScript> players = new List<PlayerScript>();

		#region Interactions

		public override bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver)) return true;
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag)) return true;
			if (interaction.HandObject == null) return true;

			return false;
		}

		public override void ServerPerformInteraction(HandApply interaction)
		{
			if (container.GetStoredObjects().Contains(interaction.Performer))
			{
				Chat.AddExamineMsg(interaction.Performer, "<color=red>I can't reach the controls from the inside!</color>");
				return;
			}
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver)) UseScrewdriver(interaction);
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag)
				&& interaction.HandObject.TryGetComponent<Emag>(out var emag)
				&& emag.EmagHasCharges())
			{
				UseEmag(emag, interaction);
			}
			else if (drawerState == DrawerState.Open) CloseDrawer();
			else OpenDrawer();
		}

		#endregion Interactions

		#region Server Only

		public override void CloseDrawer()
		{
			base.CloseDrawer();
			// Note: the sprite setting done in base.CloseDrawer() would be overridden (an unnecessary sprite call).
			// "Not great, not terrible."

			UpdateCloseState();
		}

		private void UseScrewdriver(HandApply interaction)
		{
#pragma warning disable CS0162 // Unreachable code detected
			if (!ALARM_SYSTEM_ENABLED || !ALLOW_BUZZER_TOGGLING) return;
#pragma warning restore CS0162 // Unreachable code detected

			ToolUtils.ServerUseToolWithActionMessages(interaction, 1f,
					$"You poke the {interaction.HandObject.name.ToLower()} around in the {name.ToLower()}'s electrical panel...",
					$"{interaction.Performer.ExpensiveName()} pokes a {interaction.HandObject.name.ToLower()} into the {name.ToLower()}'s electrical panel...",
					$"You manage to toggle {(!buzzerEnabled ? "on" : "off")} the consciousness-alerting buzzer.",
					$"{interaction.Performer.ExpensiveName()} removes the {interaction.HandObject.name.ToLower()}.",
					() => ToggleBuzzer());
		}

		private void UseEmag(Emag emag, HandApply interaction)
		{
#pragma warning disable CS0162 // Unreachable code detected
			if (!ALARM_SYSTEM_ENABLED || !ALLOW_EMAGGING) return;
#pragma warning restore CS0162 // Unreachable code detected
			if (alarmBroken) return;
			alarmBroken = true;
			emag.UseCharge(interaction);
			Chat.AddActionMsgToChat(interaction,
					"The status panel flickers and the buzzer makes sickly popping noises. You can smell smoke...",
							"You can smell caustic smoke from somewhere...");
			SoundManager.PlayNetworkedAtPos(emaggedSound, DrawerWorldPosition, sourceObj: gameObject);
			StartCoroutine(PlayEmagAnimation());
		}

		private void ToggleBuzzer()
		{
			buzzerEnabled = !buzzerEnabled;
			SoundManager.PlayNetworkedAtPos(buzzerToggleSound, DrawerWorldPosition, sourceObj: gameObject);
			StartCoroutine(PlayAlarm());
		}

		private void UpdateCloseState()
		{
			players = container.GetStoredObjects().Select(obj => obj.GetComponent<PlayerScript>()).Where(script => script != null).ToList();
			// Player mind can be null if player was respawned as the old body mind is nulled


			if (ConsciousnessPresent && !alarmBroken)
			{
				SetDrawerState((DrawerState)MorgueState.ShutWithPlayer);
				StartCoroutine(PlayAlarm());
			}
			else if (players.Any())
			{
				SetDrawerState((DrawerState)MorgueState.ShutWithBraindead);
			}
			else if (container.IsEmpty == false)
			{
				SetDrawerState((DrawerState)MorgueState.ShutWithItems);
			}
			else
			{
				SetDrawerState(DrawerState.Shut);
			}
		}

		private IEnumerator PlayAlarm()
		{
			if (!ALARM_SYSTEM_ENABLED || alarmRunning) yield break;

			alarmRunning = true;
			while (ConsciousnessPresent && buzzerEnabled && !alarmBroken)
			{
				SoundManager.PlayNetworkedAtPos(consciousnessAlarmSound, DrawerWorldPosition, sourceObj: gameObject);
				yield return WaitFor.Seconds(ALARM_PERIOD);
				if (drawerState == DrawerState.Open) break;
				UpdateCloseState();
			}
			alarmRunning = false;
		}

		private IEnumerator PlayEmagAnimation(float stateDelay = 0.10f)
		{
			var oldState = drawerState;

			SetDrawerState((DrawerState)MorgueState.ShutWithItems);
			yield return WaitFor.Seconds(stateDelay);
			SetDrawerState((DrawerState)MorgueState.ShutWithPlayer);
			yield return WaitFor.Seconds(stateDelay);
			SetDrawerState((DrawerState)MorgueState.ShutWithItems);
			yield return WaitFor.Seconds(stateDelay);
			SetDrawerState((DrawerState)MorgueState.ShutWithPlayer);
			yield return WaitFor.Seconds(stateDelay);
			SetDrawerState((DrawerState)MorgueState.ShutWithBraindead);
			yield return WaitFor.Seconds(stateDelay * 6);

			if (oldState != DrawerState.Open) UpdateCloseState();
			else SetDrawerState(DrawerState.Open);
		}

		#endregion Server Only
	}
}
