using System;
using System.Collections.Generic;
using Objects.Other;
using UnityEngine;

namespace Items.Others
{
	public class HolosignProjector : MonoBehaviour, IServerDespawn, ICheckedInteractable<TileApply>,
		ICheckedInteractable<HandActivate>, ICheckedInteractable<HandApply>, IExaminable
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

		public bool WillInteract(TileApply interaction, NetworkSide side)
		{
			if(DefaultWillInteract.Default(interaction, side) == false) return false;

			if(interaction.HandObject != gameObject) return false;

			if (side == NetworkSide.Server && holosigns.Count >= maxHolosigns)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Projector at max holosign capacity!");
				return false;
			}

			return true;
		}

		public void ServerPerformInteraction(TileApply interaction)
		{
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

		private bool ValidatePosition(TileApply interaction)
		{
			var matrixAt = MatrixManager.AtPoint(interaction.WorldPositionTarget, true, interaction.PerformerPlayerScript.registerTile.Matrix.MatrixInfo);
			var holosignsAtPos = matrixAt.Matrix.GetFirst<Holosign>(interaction.TargetCellPos, true);

			if (holosignsAtPos != null)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "There is already a holosign there!");
				return false;
			}

			return true;
		}

		private void SpawnHolosign(TileApply interaction)
		{
			if(ValidatePosition(interaction) == false) return;

			var newHolosign = Spawn.ServerPrefab(holosignPrefabs[index], interaction.WorldPositionTarget);
			if (newHolosign.Successful == false)
			{
				Logger.LogError("Failed to spawn holosign!");
				return;
			}

			var holosignScript = newHolosign.GameObject.GetComponent<Holosign>();
			holosigns.Add(holosignScript);
			holosignScript.SetUp(this);
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

				Chat.AddLocalMsgToChat($"{holosign.ExpensiveName()} fizzles out into nothingness", holosign);

				_ = Despawn.ServerSingle(holosign);
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if(DefaultWillInteract.Default(interaction, side) == false) return false;

			if (interaction.HandObject != gameObject) return false;

			if (interaction.TargetObject.OrNull()?.GetComponent<Holosign>() == null) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			Chat.AddActionMsgToChat(interaction.Performer, $"You clear the {interaction.TargetObject.ExpensiveName()}",
				$"{interaction.Performer.ExpensiveName()} clears the {interaction.TargetObject.ExpensiveName()}");

			interaction.TargetObject.GetComponent<Holosign>().DestroyHolosign();
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			return $"Holosign capacity: {maxHolosigns}";
		}
	}
}