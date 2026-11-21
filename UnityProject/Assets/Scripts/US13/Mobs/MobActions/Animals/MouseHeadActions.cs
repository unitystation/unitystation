using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Lifecycle;
using US13.Health;
using US13.HealthV2.Living.BodyParts;
using US13.Items.Food;
using US13.Managers;
using US13.Managers.NetworkManagement;
using US13.Messages.Server.SoundMessages;
using US13.Objects.Pipes;
using US13.Objects.Pipes.Devices;
using US13.Player;
using US13.Systems.Electricity.FunctionsAndClasses;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Behaviours.Meta.Atmospherics.Data;
using US13.Tilemaps.Utils;
using US13.UI.Core.ProgressBar;
using Util;
using Random = UnityEngine.Random;

namespace US13.Mobs.MobActions.Animals
{
	public class MouseHeadActions : MonoBehaviour
	{
		[SerializeField] private AddressableAudioSource squeakSound = null;
		[SerializeField] private string EnterVentID = "vent_crawl";

		[SerializeField] private BodyPart relatedPart;

		public void Squeak(Vector2 worldMousePosition)
		{
			AudioSourceParameters audioSourceParameters = default(AudioSourceParameters);
			audioSourceParameters.Pitch = Random.Range(.6f, 1.2f);
			SoundManager.PlayNetworkedAtPos(squeakSound, relatedPart.HealthMaster.ObjectBehaviour.OfficialPosition, audioSourceParameters);

			Chat.AddActionMsgToChat(
				gameObject,
				$"{relatedPart.HealthMaster.playerScript.name} squeaks!",
				$"{relatedPart.HealthMaster.playerScript.name} squeaks!");
		}

		public void EnterVent(Vector2 worldMousePosition)
		{
			var matrix = relatedPart.HealthMaster.playerScript.RegisterPlayer.Matrix;
			var ventsOnTile = matrix.Get<AirVent>(worldMousePosition.To3Int().ToLocal(matrix).CutToInt(), CustomNetworkManager.IsServer).ToList();
			if (ventsOnTile.Count != 0)
			{
				TryEnterVent(relatedPart.HealthMaster.playerScript, ventsOnTile[0], ventsOnTile[0].pipeMix);
				return;
			}

			var scrubbersOnTile = matrix.Get<Scrubber>(worldMousePosition.To3Int().ToLocal(matrix).CutToInt(), CustomNetworkManager.IsServer).ToList();
			if (scrubbersOnTile.Count != 0)
			{
				TryEnterVent(relatedPart.HealthMaster.playerScript, scrubbersOnTile[0], scrubbersOnTile[0].PipeMix);
				return;
			}

			relatedPart.HealthMaster.playerScript.PlayerButtonedActions?.ServerEndCooldown(EnterVentID);
			Chat.AddWarningMsgFromServer(relatedPart.HealthMaster.gameObject, "There is no pipe access on this tile!");
		}

		private void TryEnterVent(PlayerScript player, MonoPipe monoPipe, GasMix pipeMix)
		{
			Chat.AddActionMsgToChat(gameObject, $"You start to enter the {monoPipe.gameObject.ExpensiveName()}",
				$"{relatedPart.HealthMaster.playerScript.visibleName} starts to enter the {monoPipe.gameObject.ExpensiveName()}!");

			var cfg = new StandardProgressActionConfig(StandardProgressActionType.Escape);
			StandardProgressAction.Create(
				cfg,
				() => monoPipe.FinishEnteringPipe(player, pipeMix)
			).ServerStartProgress(ActionTarget.Object(monoPipe.registerTile), 5, player.gameObject);
		}

		public void EatAtLocation(Vector2 worldMousePosition)
		{
			var matrix = relatedPart.HealthMaster.playerScript.RegisterPlayer.Matrix;
			var foodOnTile = matrix.Get<Edible>(worldMousePosition.To3Int().ToLocal(matrix).CutToInt(), CustomNetworkManager.IsServer).ToList();
			if (foodOnTile.Count != 0)
			{
				EatEdibles(foodOnTile);
				return;
			}

			Vector3Int cell = worldMousePosition.To3().ToLocalInt(matrix);
			if (isExposedWire(matrix, cell) == false)
			{
				Chat.AddWarningMsgFromServer(relatedPart.HealthMaster.gameObject, "There is nothing to chew at this location!");
				return;
			}

			var electricalMatrix = matrix.MetaTileMap.Layers[LayerType.Electrical].Matrix;

			// Check if there's cables at this position
			var cables = electricalMatrix.GetElectricalConnections(cell);
			if (cables == null || cables.List.Count < 1) return;

			var cable = cables.List.PickRandom();

			ElectricityFunctions.WorkOutActualNumbers(cable);
			float voltage = cable.Data.ActualVoltage;
			var worldPosition = cell.ToWorldInt(matrix);

			// Remove the cable and spawn the item.
			var electricalTile = matrix.MetaTileMap.GetTile(cell, LayerType.Electrical) as ElectricalCableTile;
			if (electricalTile == true)
				Spawn.ServerPrefab(electricalTile.SpawnOnDeconstruct, worldPosition,
					count: electricalTile.SpawnAmountOnDeconstruct);

			cable.DestroyThisPlease();

			Electrocution electrocution = new Electrocution(voltage, worldPosition);
			relatedPart.HealthMaster.Electrocute(electrocution);
		}

		private static readonly Func<Matrix, Vector3Int, bool> isExposedWire =
			(mat, cell) => mat.MetaTileMap.GetTile(cell, LayerType.Floors) is null &&
			               mat.MetaTileMap.GetTile(cell, LayerType.Electrical) is not null &&
			               mat.MetaTileMap.IsAtmosPassableAt(cell, true);


		private void EatEdibles(List<Edible>  edibles)
		{
			foreach(var food in edibles)
			{
				if (food.enabled == false) continue;
				var bodyPart = food.GetComponent<BodyPart>();
				if (bodyPart != null && bodyPart.HealthMaster == relatedPart.HealthMaster) continue;

				food.TryConsume(relatedPart.HealthMaster.gameObject, relatedPart.HealthMaster.gameObject);
			}
		}
	}
}
