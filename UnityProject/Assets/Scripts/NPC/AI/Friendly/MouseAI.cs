using UnityEngine;

namespace NPC
{
	/// <summary>
	/// AI brain for mice
	/// used to get hunted by Runtime and squeak
	/// </summary>
	public class MouseAI : GenericFriendlyAI
	{
		private const bool WIRECHEW_ENABLED = true;
		// Chance as percentage
		private const int WIRECHEW_CHANCE = 3;

		protected override void MonitorExtras()
		{
			timeWaiting += Time.deltaTime;
			if (timeWaiting < timeForNextRandomAction)
			{
				return;
			}
			timeWaiting = 0f;
			timeForNextRandomAction = Random.Range(minTimeBetweenRandomActions, maxTimeBetweenRandomActions);

			if (WIRECHEW_ENABLED && Random.Range(0, 100) < WIRECHEW_CHANCE)
			{
				DoRandomWireChew();
			}
			else
			{
				DoRandomSqueak();
			}
		}

		public override void OnPetted(GameObject performer)
		{
			Squeak();
			StartFleeing(performer, 3f);
		}

		protected override void OnFleeingStopped()
		{
			BeginExploring();
		}

		private void Squeak()
		{
			SoundManager.PlayNetworkedAtPos(
				"MouseSqueek",
				gameObject.transform.position,
				Random.Range(.6f, 1.2f));

			Chat.AddActionMsgToChat(
				gameObject,
				$"{mobNameCap} squeaks!",
				$"{mobNameCap} squeaks!");
		}

		private void DoRandomSqueak()
		{
			Squeak();
		}

		private void DoRandomWireChew()
		{
			var metaTileMap = registerObject.TileChangeManager.MetaTileMap;
			var matrix = metaTileMap.Layers[LayerType.Underfloor].matrix;

			// Check if the floor plating is exposed.
			if (metaTileMap.HasTile(registerObject.LocalPosition, LayerType.Floors, true)) return;

			// Check if there's cables at this position
			var cables = matrix.GetElectricalConnections(registerObject.LocalPosition);
			if (cables == null || cables.Count < 1) return;

			// Pick a random cable from the mouse's current tile position to chew from
			var cable = cables[Random.Range(0, cables.Count - 1)];
			WireChew(cable);
		}

		private void WireChew(IntrinsicElectronicData cable)
		{
			ElectricityFunctions.WorkOutActualNumbers(cable);
			float voltage = cable.Data.ActualVoltage;

			// Remove the cable and spawn the item.
			cable.DestroyThisPlease();
			var electricalTile = registerObject.TileChangeManager
					.GetLayerTile(registerObject.WorldPosition, LayerType.Underfloor) as ElectricalCableTile;
			// Electrical tile is not null iff this is the first mousechew. Why?
			if (electricalTile != null)
			{
				Spawn.ServerPrefab(electricalTile.SpawnOnDeconstruct, registerObject.WorldPosition,
						count: electricalTile.SpawnAmountOnDeconstruct);
			}

			Electrocute(voltage);
		}

		private void Electrocute(float voltage)
		{
			var electrocution = new Electrocution(voltage, registerObject.WorldPosition);
			var performerLHB = GetComponent<LivingHealthBehaviour>();
			performerLHB.Electrocute(electrocution);
		}

		protected override void OnSpawnMob()
		{
			base.OnSpawnMob();
			BeginExploring();
		}
	}
}