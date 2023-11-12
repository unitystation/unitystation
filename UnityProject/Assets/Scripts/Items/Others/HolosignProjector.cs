using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Objects.Other;
using UnityEngine;
using Util;

namespace Items.Others
{
	public class HolosignProjector : MonoBehaviour, IServerDespawn,
		ICheckedInteractable<HandActivate>, ICheckedInteractable<PositionalHandApply>, IExaminable
	{
		[SerializeField]
		private List<GameObject> holosignPrefabs = new List<GameObject>();

		[SerializeField]
		private int maxHolosigns = 3;

		[SerializeField]
		private float timeToPlace = 3f;

		private List<Holosign> holosigns = new List<Holosign>();

		private int index = 0;

		public void RemoveHolosign(Holosign toRemove)
		{
			holosigns.Remove(toRemove);
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			RemoveAll();
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if(DefaultWillInteract.Default(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			if (interaction.IsAltClick && holosignPrefabs.Count > 1)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "You change the selected holosign");
				index++;

				if (index >= holosignPrefabs.Count)
				{
					index = 0;
				}

				return;
			}

			Chat.AddExamineMsgFromServer(interaction.Performer, "You clear the projectors holosigns");
			RemoveAll();
		}

		private void RemoveAll()
		{
			for (int i = holosigns.Count - 1; i >= 0; i--)
			{
				var holosign = holosigns[i].OrNull()?.gameObject;
				if(holosign == null) continue;

				Chat.AddActionMsgToChat(holosign, $"{holosign.ExpensiveName()} fizzles out into nothingness.");

				_ = Despawn.ServerSingle(holosign);
			}
		}

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if(DefaultWillInteract.Default(interaction, side) == false) return false;

			if (interaction.HandObject != gameObject) return false;

			//If clicking on holo sign allow it so we can clear
			if (interaction.TargetObject.OrNull()?.GetComponent<Holosign>() != null) return true;

			//Otherwise we are trying to place so see if we are at max capacity
			if (side == NetworkSide.Server && holosigns.Count >= maxHolosigns)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Projector at max holosign capacity!");
				return false;
			}

			return true;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			if (interaction.TargetObject != null && interaction.TargetObject.TryGetComponent<Holosign>(out var holosign))
			{
				//Only allow deletion of holo where this projector contains the prefab
				if (holosignPrefabs.Any(x =>
					    x.gameObject.GetComponent<PrefabTracker>().ForeverID ==
					    holosign.GetComponent<PrefabTracker>().ForeverID) == false)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, $"You cannot use this projector to clear the {interaction.TargetObject.ExpensiveName()}");
					return;
				}

				Chat.AddActionMsgToChat(interaction.Performer, $"You clear the {interaction.TargetObject.ExpensiveName()}",
					$"{interaction.Performer.ExpensiveName()} clears the {interaction.TargetObject.ExpensiveName()}");

				_ = Despawn.ServerSingle(holosign.gameObject);
				return;
			}

			if(ValidatePosition(interaction) == false) return;

			ToolUtils.ServerUseToolWithActionMessages(
				interaction, timeToPlace,
				"You start projecting the holosign...",
				$"{interaction.Performer.ExpensiveName()} starts projecting the holosign...",
				"You project the holosign.",
				$"{interaction.Performer.ExpensiveName()} projects the holosign.", () =>
				{
					SpawnHolosign(interaction);
				}
			);
		}

		private bool ValidatePosition(PositionalHandApply interaction)
		{
			var matrixAt = MatrixManager.AtPoint(interaction.WorldPositionTarget, true, interaction.PerformerPlayerScript.RegisterPlayer.Matrix.MatrixInfo);
			var holosignsAtPos = matrixAt.Matrix.GetFirst<Holosign>(interaction.TargetPosition.To3Int(), true);

			if (holosignsAtPos != null)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "There is already a holosign there!");
				return false;
			}

			if (MatrixManager.IsTotallyImpassable(interaction.WorldPositionTarget.To3Int(), true))
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "You cannot place a holosign here!");
				return false;
			}

			return true;
		}

		private void SpawnHolosign(PositionalHandApply interaction)
		{
			if(ValidatePosition(interaction) == false) return;

			var newHolosign = Spawn.ServerPrefab(holosignPrefabs[index], interaction.WorldPositionTarget.RoundToInt());
			if (newHolosign.Successful == false)
			{
				Loggy.LogError("Failed to spawn holosign!");
				return;
			}

			var holosignScript = newHolosign.GameObject.GetComponent<Holosign>();
			holosigns.Add(holosignScript);
			holosignScript.SetUp(this);
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			return $"Holosign capacity: {maxHolosigns}";
		}
	}
}