using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Health.Sickness
{
	/// <summary>
	/// Sickness subsystem manager
	/// </summary>
	public class SicknessManager : MonoBehaviour
	{
		public List<Sickness> Sicknesses;

		private List<PlayerSickness> sickPlayers;

		private static SicknessManager sicknessManager;
		private Thread sicknessThread;
		private float startedTime;
		private System.Random random;
		private BlockingCollection<SymptomManifestation> blockingCollectionSymptoms;

		[SerializeField]
		private GameObject contagionPrefab = null;

		public static SicknessManager Instance
		{
			get
			{
				if (!sicknessManager)
				{
					sicknessManager = FindObjectOfType<SicknessManager>();
				}

				return sicknessManager;
			}
		}

		private void OnEnable()
		{
			UpdateManager.Add(SicknessUpdate, 1);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, SicknessUpdate);
		}

		private void Start()
		{
			sickPlayers = new List<PlayerSickness>();
		}

		private void SicknessUpdate()
		{
			// Since unity can provide Time.time only in the main thread, we update for our running thread at the begining of frame.
			startedTime = Time.time;

			// Process all enqueued symptoms individually, not bound to the current frame
			SymptomManifestation symptomManifestation;
			while (blockingCollectionSymptoms.TryTake(out symptomManifestation))
				TriggerStageSymptom(symptomManifestation);
		}

		private void Awake()
		{
			// We can't use UnityEngine.Random because it can be called only in the main thread.
			random = new System.Random();
			sicknessThread = new Thread(ProcessSickness);
			sicknessThread.Start();

			blockingCollectionSymptoms = new BlockingCollection<SymptomManifestation>();
		}

		private void ProcessSickness()
		{
			while (true)
			{
				if ((GameManager.Instance != null) && (GameManager.Instance.CurrentRoundState == RoundState.Started))
				{
					lock (sickPlayers)
					{
						sickPlayers.RemoveAll(p => p.sicknessAfflictions.Count == 0);

						foreach (PlayerSickness playerSickness in sickPlayers)
						{
							// Don't check sickness for unconcious and dead players.
							if ((playerSickness.playerHealth != null) && (playerSickness.playerHealth.ConsciousState < ConsciousState.UNCONSCIOUS))
							{
								playerSickness.sicknessAfflictions.RemoveAll(p => p.IsHealed);

								lock (playerSickness.sicknessAfflictions)
								{
									foreach (SicknessAffliction sicknessAffliction in playerSickness.sicknessAfflictions)
									{
										CheckSicknessProgression(sicknessAffliction);
										CheckSymptomOccurence(sicknessAffliction, playerSickness.playerHealth);
										Thread.Sleep(100);
									}
								}
							}
							Thread.Sleep(100);
						}
					}
				}
				Thread.Sleep(100);
			}
		}

		/// <summary>
		/// Check if we should trigger due symptoms
		/// </summary>
		private void CheckSymptomOccurence(SicknessAffliction sicknessAffliction, PlayerHealth playerHealth)
		{
			Sickness sickness = sicknessAffliction.Sickness;

			// Loop on each reached stage to see if a symptom should trigger
			for (int stage = 0; stage < sickness.SicknessStages.Count; stage++)
			{
				if (stage < sicknessAffliction.CurrentStage)
				{
					float? stageNextOccurence = sicknessAffliction.GetStageNextOccurence(stage);
					if ((stageNextOccurence != null) && (startedTime > stageNextOccurence))
					{
						SicknessStage sicknessStage = sickness.SicknessStages[stage];

						// Since many symptoms need to be called within the main thread, we invoke it
						sicknessManager.blockingCollectionSymptoms.Add(new SymptomManifestation(sicknessAffliction, stage, playerHealth));

						if (sicknessStage.RepeatSymptom)
						{
							ScheduleStageSymptom(sicknessAffliction, stage);
						}
					}
				}
			}
		}

		/// <summary>
		/// Check if we should progress the current sickness.  Progress if it we are due.
		/// </summary>
		/// <param name="sicknessAffliction">A sickness of the player</param>
		private void CheckSicknessProgression(SicknessAffliction sicknessAffliction)
		{
			Sickness sickness = sicknessAffliction.Sickness;

			// Check if we are already at the final stage of a sickness
			if (sicknessAffliction.CurrentStage < sickness.SicknessStages.Count)
			{
				// Loop on each stage of the sickness to see if we have now reached the stage
				// Makes the sickness progress
				int totalSecondCount = 0;
				for (int stage = 0; stage < sickness.SicknessStages.Count; stage++)
				{
					// If the stage is isn't reached yet, we check if it's time to progress toward it
					if (stage > (int)sicknessAffliction.CurrentStage - 1)
					{
						// Check if it's time to progress to the new stage
						if ((startedTime - sicknessAffliction.ContractedTime) > totalSecondCount)
						{
							// It's time to progress to the new stage
							ScheduleStageSymptom(sicknessAffliction, stage);
						}
						else
						{
							// It's not time yet, no point checking for further stages
							break;
						}
					}

					totalSecondCount += sickness.SicknessStages[stage].SecondsBeforeNextStage;
				}
			}
		}

		/// <summary>
		/// Trigger the symptom for a specific stage.
		/// </summary>
		/// <param name="asyncSymptom">The symptom to be applied (including player reference and such)</param>
		private void TriggerStageSymptom(SymptomManifestation symptomManifestation)
		{
			// Sometimes, the symptom get queued before the player dies and is unqueud when the player is dead/unconcious.
			// This prevents the symptom to be shown.
			if (symptomManifestation.PlayerHealth.ConsciousState >= ConsciousState.UNCONSCIOUS)
				return;

				switch (symptomManifestation.SicknessAffliction.Sickness.SicknessStages[symptomManifestation.Stage].Symptom)
			{
				case SymptomType.Wellbeing:
					PerformSymptomWellbeing(symptomManifestation);
					break;
				case SymptomType.Immune:
					PerformSymptomImmune(symptomManifestation);
					break;
				case SymptomType.Cough:
					PerformSymptomCough(symptomManifestation);
					break;
				case SymptomType.Sneeze:
					PerformSymptomSneeze(symptomManifestation);
					break;
				case SymptomType.CustomMessage:
					PerformSymptomCustomMessage(symptomManifestation);
					break;
			}
		}

		/// <summary>
		/// Schedule the next occurence of the symptom of a particular stage
		/// </summary>
		/// <param name="sicknessAffliction">A sickness afflicting a player</param>
		/// <param name="stage">The stage for which the symptom must be scheduled</param>
		private void ScheduleStageSymptom(SicknessAffliction sicknessAffliction, int stage)
		{
			Sickness sickness = sicknessAffliction.Sickness;
			SicknessStage sicknessStage = sickness.SicknessStages[stage];

			if (sicknessStage.RepeatMaxDelay < sicknessStage.RepeatMinDelay)
			{
				Logger.LogError($"Sickness: {sickness.SicknessName}. Repeatable sickness symptoms should always have a RepeatMaxDelay ({sicknessStage.RepeatMaxDelay}) >= RepeatMinDelay ({sicknessStage.RepeatMinDelay})", Category.Health);
				return;
			}

			if (sicknessStage.RepeatMinDelay < 5)
			{
				Logger.LogError($"Sickness: {sickness.SicknessName}. Repeatable sickness symptoms should have a RepeatMinDelay ({sicknessStage.RepeatMinDelay}) >= 5.  Think of the server performance.", Category.Health);
				return;
			}

			sicknessAffliction.ScheduleStageNextOccurence(stage, startedTime + random.Next((int)sicknessStage.RepeatMinDelay, (int)sicknessStage.RepeatMaxDelay));
		}

		/// <summary>
		/// Perform the sneeze symptom for afflicted player.
		/// </summary>
		private void PerformSymptomSneeze(SymptomManifestation symptomManifestation)
		{
			GameObject performer = symptomManifestation.PlayerHealth.gameObject;
			Chat.AddActionMsgToChat(performer, "You sneeze.", $"{performer.name} sneezes");

			if (symptomManifestation.SicknessAffliction.Sickness.Contagious)
			{
				SpawnContagion(symptomManifestation);
			}
		}

		/// <summary>
		/// Perform the sneeze symptom for afflicted player.
		/// </summary>
		private void PerformSymptomCough(SymptomManifestation symptomManifestation)
		{
			GameObject performer = symptomManifestation.PlayerHealth.gameObject;
			Chat.AddActionMsgToChat(performer, "You cough.", $"{performer.name} coughs");

			if (symptomManifestation.SicknessAffliction.Sickness.Contagious)
			{
				SpawnContagion(symptomManifestation);
			}
		}

		/// <summary>
		/// Perform the custom message symptom.
		/// This will spawn a random message for the affected player and perhaps it's public counterpart for the observers
		/// </summary>
		private void PerformSymptomCustomMessage(SymptomManifestation symptomManifestation)
		{
			GameObject performer = symptomManifestation.PlayerHealth.gameObject;

			CustomMessageParameter customMessageParameter = (CustomMessageParameter)symptomManifestation.SicknessAffliction.Sickness.SicknessStages[symptomManifestation.Stage].ExtendedSymptomParameters;

			int randomMessage = Random.Range(0, customMessageParameter.CustomMessages.Count);
			CustomMessage customMessage = customMessageParameter.CustomMessages[randomMessage];

			string privateMessage = (customMessage.privateMessage ?? "").Replace("%PLAYERNAME%", performer.name);
			string publicMessage = (customMessage.publicMessage ?? "").Replace("%PLAYERNAME%", performer.name);

			if (string.IsNullOrWhiteSpace(publicMessage))
				Chat.AddExamineMsg(performer, privateMessage);
			else
				Chat.AddActionMsgToChat(performer, privateMessage, publicMessage);
		}

		/// <summary>
		/// This will remove the sickness from the player, healing him.
		/// </summary>
		private void PerformSymptomWellbeing(SymptomManifestation symptomManifestation)
		{
			symptomManifestation.PlayerHealth.RemoveSickness(symptomManifestation.SicknessAffliction.Sickness);
		}

		/// <summary>
		/// This will remove the sickness from the player, healing him.  This will also make him immune for the current round.
		/// </summary>
		private void PerformSymptomImmune(SymptomManifestation symptomManifestation)
		{
			symptomManifestation.PlayerHealth.ImmuneSickness(symptomManifestation.SicknessAffliction.Sickness);
		}

		/// <summary>
		/// This will spawn a contagion spot with the specific sickness in it
		/// at the player position and in the 3 spots in front of him.
		/// Like so:
		///     o
		///    xo
		///     o
		/// </summary>
		/// <param name="symptomManifestation"></param>
		private void SpawnContagion(SymptomManifestation symptomManifestation)
		{
			Vector3Int position = symptomManifestation.PlayerHealth.gameObject.RegisterTile().WorldPositionServer;
			Directional playerDirectional = symptomManifestation.PlayerHealth.PlayerMove.PlayerDirectional;

			// Player position
			SpawnContagionSpot(symptomManifestation, position);

			// In front
			SpawnContagionSpot(symptomManifestation, position + playerDirectional.CurrentDirection.Vector);

			// Front left
			SpawnContagionSpot(symptomManifestation, position + (Quaternion.Euler(0, 0, -45) * playerDirectional.CurrentDirection.Vector));

			// Front Right
			SpawnContagionSpot(symptomManifestation, position + (Quaternion.Euler(0, 0, 45) * playerDirectional.CurrentDirection.Vector));
		}

		private void SpawnContagionSpot(SymptomManifestation symptomManifestation, Vector3 position)
		{
			SpawnResult spawnResult = Spawn.ServerPrefab(contagionPrefab, position, null, null, 1, null, true);

			if (spawnResult.Successful)
				spawnResult.GameObject.GetComponent<Contagion>().Sickness = symptomManifestation.SicknessAffliction.Sickness;
		}

		// Add this player as a sick player
		public void RegisterSickPlayer(PlayerSickness playerSickness)
		{
			lock(sickPlayers)
			{
				if (!sickPlayers.Contains(playerSickness))
					sickPlayers.Add(playerSickness);
			}
		}

		// Remove this player from the sick players
		public void UnregisterHealedPlayer(PlayerSickness playerSickness)
		{
			lock (sickPlayers)
			{
				if (!sickPlayers.Contains(playerSickness))
					sickPlayers.Remove(playerSickness);
			}
		}

		private void OnDestroy()
		{
			blockingCollectionSymptoms.Dispose();
		}
	}
}
