using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Chemistry;
using Cysharp.Threading.Tasks;
using Light2D;
using Logs;
using Mirror;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using US13.Actions.V2.UI;
using US13.Core;
using US13.Core.Camera;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Lifecycle;
using US13.Core.Lighting;
using US13.Core.Utils;
using US13.Health.Living.SimpleAnimal;
using US13.Health.Objects;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.HealthV2.Living.MedicalChemistry;
using US13.HealthV2.Living.PolymorphicSystems;
using US13.Managers.MatrixManager;
using US13.Managers.NetworkManagement;
using US13.Mobs;
using US13.Player;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Behaviours.Objects;
using US13.Tilemaps.Utils;
using US13.UI.Core.ProgressBar;
using US13.UI.Systems;
using Util;

namespace US13.Systems.Antagonists
{
	public partial class VampireStageProgression
	{
		[SerializeField, BoxGroup("Blood Drain")] private string bloodDrainActionId = "blood_drain";
		[SerializeField, BoxGroup("Blood Drain")] private float bloodDrainRange = 1.5f;
		[SerializeField, BoxGroup("Blood Drain")] private float bloodDrainTime = 1.5f;
		[SerializeField, BoxGroup("Blood Drain"), Range(0,100)] private int bloodDrainThreshold = 70;
		private float BloodDrainThresholdFraction => bloodDrainThreshold / 100.0f;
		[SerializeField, BoxGroup("Blood Drain"), Range(0,100)] private int bloodDrainAmount = 10;
		private float BloodDrainAmountFraction => bloodDrainAmount / 100.0f;
		[SerializeField, BoxGroup("Blood Drain"), Range(0,100)] private int bloodDrainEfficiency = 20;
		private float BloodDrainEfficiencyFraction => bloodDrainEfficiency / 100.0f;

		[SerializeField, BoxGroup("Corrupt")] private string corruptActionId = "convert";
		[SerializeField, BoxGroup("Corrupt")] private float corruptRange = 1.5f;
		[SerializeField, BoxGroup("Corrupt")] private float corruptTime = 1.5f;
		[SerializeField, BoxGroup("Corrupt")] private float corruptionAmount = 2.5f;
		[SerializeField, BoxGroup("Corrupt")] private float selfCorruptionAmount = 10.0f;

		[SerializeField, BoxGroup("Hypnotic Stare")] private float hypnoticStareDuration = 10.0f;
		[SerializeField, BoxGroup("Hypnotic Stare")] private float hypnoticStareRange = 4.5f;
		[SerializeField, BoxGroup("Hypnotic Stare"), Range(0,180)] private int hypnoticStareAngleDegrees = 45;
		[FormerlySerializedAs("hypnoticStateLightData")] [SerializeField, BoxGroup("Hypnotic Stare")] private LightData hypnoticStareLightData;
		[SerializeField, BoxGroup("Hypnotic Stare")] private LightsHolder playerLightsHolder;
		[SerializeField,BoxGroup("Hypnotic Stare")] private LightSprite stareLightSprite;
		public LightSprite ObjectLightSprite => stareLightSprite;

		private static readonly float DefaultvisibilityAnimationSpeed = 1.25f;
		private static readonly float RevertvisibilityAnimationSpeed = 0.2f;
		private static readonly Vector3 ExpandedNightVisionVisibility = new Vector3(25, 25, 42);

		[SerializeField, BoxGroup("Night Eyes")] private float darknessVisibilityMultiplier = 25.0f;
		[SerializeField, BoxGroup("Night Eyes")] private Color dimLightColour = new Color(255,255,255,10);

		[SerializeField,BoxGroup("Sanguine Cloak")] private GameObject vampireCloak;
		[SerializeField,BoxGroup("Sanguine Cloak")] private ItemStorage cloakSlotTempStorage;

		[SerializeField,BoxGroup("Sanguine Dagger")] private GameObject sanguineDaggerPrefab;
		[SerializeField,BoxGroup("Sanguine Dagger")] private int selfDamage = 20;
		[SerializeField,BoxGroup("Sanguine Dagger")] private float bloodTaken = 5;
		private int _lightId;
		private float squaredCosine;


		private static readonly StandardProgressActionConfig progressConfig =
			new StandardProgressActionConfig(StandardProgressActionType.Afflict, false, false, true, false, true);

		private bool isOn = true;
		private bool cloakEquipped = false;

		[SyncVar(hook = nameof(SyncNightVision))]
		private bool nightEyesEnabled = false;


		private void Awake()
		{
			squaredCosine = Mathf.Cos(hypnoticStareAngleDegrees * 0.5f * Mathf.Deg2Rad);
			_lightId = Guid.NewGuid().GetHashCode();
			hypnoticStareLightData.Id = _lightId;
			hypnoticStareLightData.lightSpriteObject = connectedPlayer.netIdentity;
		}

		private void UpdateLights(bool newOn)
		{
			isOn = newOn;
			if (isOn)
			{
				playerLightsHolder?.AddLight(hypnoticStareLightData);
				playerLightsHolder?.SetDirty();
				playerLightsHolder?.UpdateLights();
			}
			else playerLightsHolder?.RemoveLight(hypnoticStareLightData);
		}

		public void BloodDrain(Vector2 worldMousePosition)
		{
			if (connectedPlayer == null)
			{
				Loggy.Error("How did we manage to run this without a player?");
				return;
			}

			var matrix = connectedPlayer.gameObject.RegisterTile().Matrix;


			var playersOnTile = matrix.Get<PlayerScript>(worldMousePosition.To3Int().ToLocal(matrix).CutToInt(), CustomNetworkManager.IsServer).ToList();
			if (playersOnTile.Count != 0)
			{
				if(TryDrainPlayer(worldMousePosition, matrix, playersOnTile[0]) == false) TryForceEndCooldown(bloodDrainActionId);
				return;
			}

			//Account for V1 mobs while we still have them.
			//Vampires need to be able to drain mobs like rats
			//Once MobsV1 is removed completely, this section of the method may be removed (Except for the warning message at the bottom)
			var mobsOnTile = matrix.Get<SimpleAnimal>(worldMousePosition.To3Int().ToLocal(matrix).CutToInt(), CustomNetworkManager.IsServer).ToList();
			if (mobsOnTile.Count != 0)
			{
				if(TryDrainMob(worldMousePosition, matrix, mobsOnTile[0]) == false) TryForceEndCooldown(bloodDrainActionId);
				return;
			}

			TryForceEndCooldown(bloodDrainActionId);
			Chat.AddWarningMsgFromServer(connectedPlayer.gameObject, "There is no creature here to drain!");
		}

		private void TryForceEndCooldown(string actionId)
		{
			connectedPlayer.PlayerButtonedActions?.ServerEndCooldown(actionId);
		}

		private bool TryDrainPlayer(Vector2 worldMousePosition, Matrix matrix, PlayerScript firstPlayerOnTile)
		{
			var pos = worldMousePosition.To3Int().ToLocal();
			if (Vector3.Distance(connectedPlayer.gameObject.AssumedWorldPosServer().ToLocalInt(matrix), pos) > bloodDrainRange)
			{
				Chat.AddWarningMsgFromServer(connectedPlayer.gameObject, "You are too far away to drain this creature!");
				return false;
			}

			ReagentPoolSystem victimReagentPool = firstPlayerOnTile.playerHealth?.reagentPoolSystem;
			TeamData currentTeam = firstPlayerOnTile.Mind?.AntagPublic?.CurTeam?.Data;
			if (victimReagentPool == null || currentTeam == vampireTeam)
			{
				Chat.AddWarningMsgFromServer(connectedPlayer.gameObject, "You do not have the ability to drain this creature!");
				return false;
			}

			if (victimReagentPool.BloodPool.Total < victimReagentPool.NormalBlood * BloodDrainThresholdFraction)
			{
				Chat.AddWarningMsgFromServer(connectedPlayer.gameObject, "This creature does not have enough blood to drain!");
				return false;
			}

			var bar = StandardProgressAction.Create(progressConfig, () =>
				{
					Chat.AddExamineMsg(connectedPlayer.gameObject, $"You successfully to drain {firstPlayerOnTile.visibleName}'s blood.");
					Chat.AddWarningMsgFromServer(firstPlayerOnTile.gameObject, "You feel a small prick on your neck.");

					ReagentMix extractedBlood = victimReagentPool.BloodPool?.Take(victimReagentPool.NormalBlood * BloodDrainAmountFraction);
					if (extractedBlood == null) return;

					float gainedBlood = extractedBlood.Total * BloodDrainEfficiencyFraction;
					ReagentPool.BloodPool?.Add(CommonSicknesses.Instance.VampirismReagent, gainedBlood);
					connectedPlayer.playerHealth?.HealDamageOnAll(connectedPlayer.gameObject, gainedBlood, DamageType.Brute);
				})
				.ServerStartProgress(firstPlayerOnTile.RegisterPlayer, bloodDrainTime, connectedPlayer.gameObject);
			if (bar != null)
			{
				Chat.AddExamineMsg(connectedPlayer.gameObject, $"You begin to drain {firstPlayerOnTile.visibleName}'s blood.");
			}

			return true;
		}

		private bool TryDrainMob(Vector2 worldMousePosition, Matrix matrix, SimpleAnimal firstMobOnTile)
		{
			var pos = worldMousePosition.To3Int().ToLocal();
			if (Vector3.Distance(connectedPlayer.gameObject.AssumedWorldPosServer().ToLocalInt(matrix), pos) > bloodDrainRange)
			{
				Chat.AddWarningMsgFromServer(connectedPlayer.gameObject, "You are too far away to drain this creature!");
				return false;
			}

			float bloodToDrain = firstMobOnTile.maxHealth * BloodDrainAmountFraction;
			if(firstMobOnTile.OverallHealth < firstMobOnTile.maxHealth * BloodDrainThresholdFraction)
			{
				Chat.AddWarningMsgFromServer(connectedPlayer.gameObject, "This creature does not have enough blood to drain!");
				return false;
			}

			var bar = StandardProgressAction.Create(progressConfig, () =>
			{
				Chat.AddExamineMsg(connectedPlayer.gameObject, $"You successfully to drain {firstMobOnTile.name}'s blood.");
				ReagentPool.BloodPool.Add(CommonSicknesses.Instance.VampirismReagent, bloodToDrain * BloodDrainEfficiencyFraction * 2.5f); //this times 10 is just to make V1 mobs give some blood despite low health
				connectedPlayer.playerHealth.HealDamageOnAll(connectedPlayer.gameObject, bloodToDrain * BloodDrainEfficiencyFraction * 2.5f, DamageType.Brute);
				firstMobOnTile.ApplyDamage(connectedPlayer.gameObject, bloodToDrain, AttackType.Internal, DamageType.Brute);
			})
				.ServerStartProgress(firstMobOnTile.gameObject.RegisterTile(), bloodDrainTime, connectedPlayer.gameObject);
			if (bar != null)
			{
				Chat.AddExamineMsg(connectedPlayer.gameObject, $"You begin to drain {firstMobOnTile.name}'s blood.");
			}

			return true;
		}

		public void HypnoticStare(Vector2 worldMousePosition)
		{
			_ = HypnoticStareAsync(worldMousePosition);
		}

		private async UniTaskVoid HypnoticStareAsync(Vector2 worldMousePosition)
		{
			if (connectedPlayer == null)
			{
				Loggy.Error("How did we manage to run this without a player?");
				return;
			}

			UpdateLights(true);
			connectedPlayer.RegisterPlayer.ServerStun(1.0f, false, false, true, null, false); //Lock up inputs for the ability

			Vector3 positionOrigin = gameObject.AssumedWorldPosServer();
			Vector3 facingVector = connectedPlayer.CurrentDirection.ToLocalVector3();

			var nearbyPlayers = ComponentsTracker<LivingHealthMasterBase>.GetAllNearbyTypesToTarget(connectedPlayer.gameObject, hypnoticStareRange, bypassInventories: false);
			foreach(var player in nearbyPlayers)
			{
				if(player.gameObject == gameObject) continue;
				Vector3 targetPosition = player.gameObject.AssumedWorldPosServer();
				Vector3 relativeVector = targetPosition - positionOrigin;

				if (relativeVector.sqrMagnitude > hypnoticStareRange * hypnoticStareRange) continue;

				float dotProduct = Vector3.Dot(relativeVector, facingVector);
				if (dotProduct <= 0f) continue;
				bool isInCone = dotProduct * dotProduct > squaredCosine * relativeVector.sqrMagnitude;
				if (isInCone == false) continue;

				//The above code uses the identity A dot B = |A||B|cos(theta) to quickly verify the cone without needing any trig functions, divisions or squares
				//Squared Cosine is evaluated on awake

				var result = MatrixManager.Linecast(
					positionOrigin, LayerTypeSelection.Walls, null,
					targetPosition, DEBUG: false);
				if (result.ItHit) continue;

				player.playerScript.RegisterPlayer.ServerSleep(hypnoticStareDuration);
			}

			await UniTask.WaitForSeconds(1.0f);
			UpdateLights(false);
		}

		public void Convert(Vector2 worldMousePosition)
		{
			if (connectedPlayer == null)
			{
				Loggy.Error("How did we manage to run this without a player?");
				return;
			}

			var matrix = connectedPlayer.gameObject.RegisterTile().Matrix;


			var playersOnTile = matrix.Get<PlayerScript>(worldMousePosition.To3Int().ToLocal(matrix).CutToInt(), CustomNetworkManager.IsServer).ToList();
			if (playersOnTile.Count != 0)
			{
				if(TryConvertPlayer(worldMousePosition, matrix, playersOnTile[0]) == false) TryForceEndCooldown(corruptActionId);
				return;
			}

			TryForceEndCooldown(corruptActionId);
			Chat.AddWarningMsgFromServer(connectedPlayer.gameObject, "There is no creature here to corrupt!");
		}

		private bool TryConvertPlayer(Vector2 worldMousePosition, Matrix matrix, PlayerScript firstPlayerOnTile)
		{
			var pos = worldMousePosition.To3Int().ToLocal();
			if (Vector3.Distance(connectedPlayer.gameObject.AssumedWorldPosServer().ToLocalInt(matrix), pos) > bloodDrainRange)
			{
				Chat.AddWarningMsgFromServer(connectedPlayer.gameObject, "You are too far away to corrupt this creature!");
				return false;
			}

			ReagentPoolSystem victimReagentPool = firstPlayerOnTile.playerHealth?.reagentPoolSystem;
			TeamData currentTeam = firstPlayerOnTile.Mind?.AntagPublic?.CurTeam?.Data;
			if (victimReagentPool == null || currentTeam == vampireTeam)
			{
				Chat.AddWarningMsgFromServer(connectedPlayer.gameObject, "You do not have the ability to corrupt this creature!");
				return false;
			}

			var bar = StandardProgressAction.Create(progressConfig, () =>
			{
				Chat.AddExamineMsg(connectedPlayer.gameObject, $"You successfully corrupt {firstPlayerOnTile.visibleName}'s blood.");
				Chat.AddWarningMsgFromServer(firstPlayerOnTile.gameObject, "You feel a small prick on your neck.");
			}).ServerStartProgress(firstPlayerOnTile.RegisterPlayer, corruptTime, connectedPlayer.gameObject);
			if (bar != null)
			{
				Chat.AddExamineMsg(connectedPlayer.gameObject, $"You begin to corrupt {firstPlayerOnTile.visibleName}'s blood.");
				victimReagentPool.BloodPool.Add(CommonSicknesses.Instance.VampirismReagent, corruptionAmount);
				ReagentPool.BloodPool.Add(CommonSicknesses.Instance.VampirismReagent, selfCorruptionAmount);
			}

			return true;
		}

		public void ToggleNightVision(Vector2 worldMousePosition)
		{
			nightEyesEnabled = !nightEyesEnabled;
		}

		public void SyncNightVision(bool oldState, bool newState)
		{
			nightEyesEnabled = newState;
			if (connectedPlayer == false) return;
			ApplyEffects(newState);
		}

		private void ApplyEffects(bool state)
		{
			var finalState = state;

			if(connectedPlayer != PlayerManager.LocalPlayerScript) return; //Only should toggle night vision if this mind is the local player
			if (Camera.main == null || Camera.main.TryGetComponent<CameraEffectControlScript>(out var effects) == false) return;

			effects.AdjustPlayerVisibility(
				finalState ? ExpandedNightVisionVisibility : effects.MinimalVisibilityScale,
				finalState ? DefaultvisibilityAnimationSpeed : RevertvisibilityAnimationSpeed);
			effects.ToggleNightEyesState(finalState);

			if (PlayerManager.LocalPlayerScript == null) return;
			DimPlayerLightController dimLightController = PlayerManager.LocalPlayerScript.DimPlayerLightController;

			if (dimLightController != null && state)
			{
				dimLightController.lightColor = dimLightColour;
				dimLightController.UpdateLightData(DimPlayerLightController.DEFAULT_SIZE * darknessVisibilityMultiplier, true);
			}
			else if(dimLightController != null) dimLightController.ResetToDefault();
		}

		public void SpectralCloak(Vector2 worldMousePosition)
		{
			if (cloakEquipped == false) EquipCloak();
			else UnEquipCloak();
		}

		private void EquipCloak()
		{
			cloakEquipped = true;
			if (cloakSlotTempStorage == false) return;
			List<ItemSlot> neckSlots = connectedPlayer.DynamicItemStorage.GetNamedItemSlots(NamedSlot.neck);
			if (neckSlots.Count() != 0) Inventory.Inventory.ServerTransfer(neckSlots[0], cloakSlotTempStorage.GetIndexedItemSlot(0));
			GameObject cloakObject = Spawn.ServerPrefab(vampireCloak).GameObject;
			Inventory.Inventory.ServerAdd(cloakObject, neckSlots[0]);
			neckSlots[0].ServerSetLock(true);
		}

		private void UnEquipCloak()
		{
			cloakEquipped = false;
			if (cloakSlotTempStorage == false) return;
			List<ItemSlot> neckSlots = connectedPlayer.DynamicItemStorage.GetNamedItemSlots(NamedSlot.neck);
			neckSlots[0].ServerSetLock(false);
			if (Inventory.Inventory.ServerTransfer(cloakSlotTempStorage.GetIndexedItemSlot(0), neckSlots[0],
				    ReplacementStrategy.DespawnOther) == false)
			{
				Inventory.Inventory.ServerDespawn(neckSlots[0]);
			}
		}

		public void SummonSanguineDagger(Vector2 worldMousePosition)
		{
			List<ItemSlot> handSlots = connectedPlayer.Mind?.Body?.DynamicItemStorage.GetHandSlots();
			handSlots = handSlots?.FindAll(h => h.IsEmpty); //Get all empty hands
			if (handSlots == null || handSlots.Any() == false)
			{
				Chat.AddWarningMsgFromServer(connectedPlayer.gameObject, "You do not have any free hands to perform this action!");
				return;
			}

			GameObject sanguineDagger = Spawn.ServerPrefab(sanguineDaggerPrefab).GameObject;
			Inventory.Inventory.ServerAdd(sanguineDagger, handSlots[0], ReplacementStrategy.DropOther, true);

			connectedPlayer.Mind?.Body?.playerHealth?.ApplyDamageToRandomBodyPart(sanguineDagger, selfDamage, AttackType.Internal, DamageType.Brute);
			Chat.AddExamineMsgFromServer(connectedPlayer.gameObject, "You draw forth your own blood to form a sanguine dagger.");

			if(sanguineDagger.TryGetComponent<SanguineDagger>(out var daggerComponent) == false) return;

			ReagentMix bloodToTake = connectedPlayer.playerHealth?.reagentPoolSystem?.BloodPool?.CloneSample(bloodTaken);
			if (bloodToTake == null) return;

			daggerComponent.FillReagentMix(bloodToTake);
		}
	}
}
