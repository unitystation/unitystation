using System.Collections.Generic;
using HealthV2;
using UnityEngine;
using Systems.Atmospherics;
using Tiles;

namespace Systems.Interaction
{
	/// <summary>
	/// Tiles are not prefabs, but we still want to be able to associate interaction logic with them.
	/// This abstract base scriptable object allows tiles to define their interaction logic by referencing
	/// subclasses of this class.
	///
	/// LavaStepInteraction, contains dictionary of all objects which should be lit on fire every tick
	/// </summary>
	[CreateAssetMenu(fileName = "LavaStepInteraction", menuName = "Interaction/TileInteraction/LavaStepInteraction")]
	public class LavaStepInteraction : TileStepInteraction
	{
		[SerializeField]
		private float playerMobFireStacks = 3;

		[SerializeField]
		private float objectFireDamage = 5;

		[SerializeField]
		private float fireTimer = 1f;

		//All lava tiles will use this same dictionary as this is a Scriptable object
		private Dictionary<GameObject, BasicTile> stuffToLightOnFire = new Dictionary<GameObject, BasicTile>();

		private void TryLightOnFire()
		{
			var keep = new Dictionary<GameObject, BasicTile>();

			foreach (var objectToTest in stuffToLightOnFire)
			{
				if(objectToTest.Key == null) continue;
				if(objectToTest.Key.TryGetComponent<RegisterTile>(out var registerTile) == false) continue;
				LayerTile tile = registerTile.Matrix.MetaTileMap.GetTile(registerTile.LocalPositionServer, true);

				if (tile is BasicTile basicTile)
				{
					//Work out if the object is still on the same tile as before
					if(basicTile != objectToTest.Value) continue;

					keep.Add(objectToTest.Key, objectToTest.Value);
				}
			}

			stuffToLightOnFire = keep;

			if (stuffToLightOnFire.Count == 0)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, TryLightOnFire);
				AtmosThread.runLavaFireTick = false;
				return;
			}

			foreach (var objectToLight in stuffToLightOnFire.Keys)
			{
				DamageObject(objectToLight);
			}
		}

		private void DamageObject(GameObject objectToBurn)
		{
			if (objectToBurn.TryGetComponent<PlayerHealthV2>(out var playerHealth))
			{
				playerHealth.ChangeFireStacks(playerMobFireStacks);
				return;
			}

			if (objectToBurn.TryGetComponent<LivingHealthBehaviour>(out var livingHealthBehaviour))
			{
				livingHealthBehaviour.ChangeFireStacks(playerMobFireStacks);
				return;
			}

			if (objectToBurn.TryGetComponent<Integrity>(out var integrity))
			{
				integrity.ApplyDamage(objectFireDamage, AttackType.Fire, DamageType.Burn);
			}
		}

		private void AddToFireDict(GameObject objectToAdd)
		{
			if (objectToAdd.TryGetComponent<RegisterTile>(out var registerTile) == false) return;

			LayerTile tile = registerTile.Matrix.MetaTileMap.GetTile(registerTile.LocalPositionServer, true);

			if (tile is BasicTile basicTile)
			{
				if (stuffToLightOnFire.TryGetValue(objectToAdd, out var oldBasicTile))
				{
					//If the object has moved to another lava tile, replace the dictionary value
					if (basicTile != oldBasicTile)
					{
						stuffToLightOnFire[objectToAdd] = basicTile;
					}

					ActivateFireUpdate(objectToAdd);
					return;
				}

				stuffToLightOnFire.Add(objectToAdd, basicTile);
			}

			ActivateFireUpdate(objectToAdd);
		}

		private void ActivateFireUpdate(GameObject objectToBurn)
		{
			if(AtmosThread.runLavaFireTick) return;

			//Trigger one fire tick and then every fireTimer
			DamageObject(objectToBurn);
			UpdateManager.Add(TryLightOnFire, fireTimer);

			AtmosThread.runLavaFireTick = true;
		}

		//Player enter tile interaction//
		public override bool WillAffectPlayer(PlayerScript playerScript)
		{
			return playerScript.PlayerState == PlayerScript.PlayerStates.Normal;
		}

		public override void OnPlayerStep(PlayerScript playerScript)
		{
			AddToFireDict(playerScript.gameObject);
		}

		//Object, mob, item enter tile interaction//
		public override bool WillAffectObject(GameObject eventData)
		{
			return true;
		}

		public override void OnObjectEnter(GameObject eventData)
		{
			AddToFireDict(eventData);
		}
	}
}