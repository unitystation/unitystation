using System.Collections.Concurrent;
using System.Collections.Generic;
using Managers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Health.Sickness
{
	/// <summary>
	/// Sickness subsystem manager
	/// </summary>
	public class SicknessManager : SingletonManager<SicknessManager>
	{
		public List<Sickness> Sicknesses;

		public List<MobSickness> sickPlayers = new List<MobSickness>();
		private SicknessThread sicknessThread;

		public BlockingCollection<SymptomManifestation> blockingCollectionSymptoms = new BlockingCollection<SymptomManifestation>();

		[SerializeField]
		private GameObject contagionPrefab = null;

		private void Awake()
		{
			base.Awake();
			sicknessThread = gameObject.AddComponent<SicknessThread>();
			// We can't use UnityEngine.Random because it can be called only in the main thread.
			sicknessThread.random = new System.Random();
		}

		private void OnEnable()
		{
			UpdateManager.Add(SicknessUpdate, 1);
			EventManager.AddHandler(Event.PostRoundStarted, OnPostRoundStart);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, SicknessUpdate);
			EventManager.RemoveHandler(Event.PostRoundStarted, OnPostRoundStart);
			sicknessThread.StopThread();
			blockingCollectionSymptoms?.Dispose();
		}

		private void OnPostRoundStart()
		{
			if (CustomNetworkManager.IsServer == false) return;

			sickPlayers = new List<MobSickness>();
			blockingCollectionSymptoms = new BlockingCollection<SymptomManifestation>();
			sicknessThread.StartThread();
		}

		//Server side only
		private void SicknessUpdate()
		{
			if (CustomNetworkManager.IsServer == false) return;

			// Since unity can provide Time.time only in the main thread, we update for our running thread at the begining of frame.
			sicknessThread.startedTime = Time.time;

			// Process all enqueued symptoms individually, not bound to the current frame
			SymptomManifestation symptomManifestation;
			while (blockingCollectionSymptoms.TryTake(out symptomManifestation))
			{
				TriggerStageSymptom(symptomManifestation);
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
			if (symptomManifestation.MobHealth.ConsciousState >= ConsciousState.UNCONSCIOUS)
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
		/// Perform the sneeze symptom for afflicted player.
		/// </summary>
		private void PerformSymptomSneeze(SymptomManifestation symptomManifestation)
		{
			GameObject performer = symptomManifestation.MobHealth.gameObject;
			Chat.AddActionMsgToChat(performer, "You sneeze.", $"{performer.ExpensiveName()} sneezes!");

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
			GameObject performer = symptomManifestation.MobHealth.gameObject;
			Chat.AddActionMsgToChat(performer, "You cough.", $"{performer.ExpensiveName()} coughs!");

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
			GameObject performer = symptomManifestation.MobHealth.gameObject;

			CustomMessageParameter customMessageParameter = (CustomMessageParameter)symptomManifestation.SicknessAffliction.Sickness.SicknessStages[symptomManifestation.Stage].ExtendedSymptomParameters;

			int randomMessage = Random.Range(0, customMessageParameter.CustomMessages.Count);
			CustomMessage customMessage = customMessageParameter.CustomMessages[randomMessage];

			string performerName = performer.ExpensiveName();
			string privateMessage = (customMessage.privateMessage ?? "").Replace("%PLAYERNAME%", performerName);
			string publicMessage = (customMessage.publicMessage ?? "").Replace("%PLAYERNAME%", performerName);

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
			symptomManifestation.MobHealth.RemoveSickness(symptomManifestation.SicknessAffliction.Sickness);
		}

		/// <summary>
		/// This will remove the sickness from the player, healing him.  This will also make him immune for the current round.
		/// </summary>
		private void PerformSymptomImmune(SymptomManifestation symptomManifestation)
		{
			symptomManifestation.MobHealth.ImmuneSickness(symptomManifestation.SicknessAffliction.Sickness);
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
			Vector3Int position = symptomManifestation.MobHealth.gameObject.RegisterTile().WorldPositionServer;
			Rotatable directional = symptomManifestation.MobHealth.GetComponent<Rotatable>();

			// Player position
			SpawnContagionSpot(symptomManifestation, position);

			if(directional == null) return;

			// In front
			SpawnContagionSpot(symptomManifestation, position + directional.CurrentDirection.ToLocalVector3());

			// Front left
			SpawnContagionSpot(symptomManifestation, position + (Quaternion.Euler(0, 0, -45) * directional.CurrentDirection.ToLocalVector3()));

			// Front Right
			SpawnContagionSpot(symptomManifestation, position + (Quaternion.Euler(0, 0, 45) * directional.CurrentDirection.ToLocalVector3()));
		}

		private void SpawnContagionSpot(SymptomManifestation symptomManifestation, Vector3 position)
		{
			SpawnResult spawnResult = Spawn.ServerPrefab(contagionPrefab, position, null, null, 1, null, true);

			if (spawnResult.Successful && spawnResult.GameObject.TryGetComponent<Contagion>(out var contagion))
			{
				contagion.Sickness = symptomManifestation.SicknessAffliction.Sickness;
				contagion.Sickness.SetCure();
			}
		}

		// Add this player as a sick player
		public void RegisterSickPlayer(MobSickness mobSickness)
		{
			lock (sickPlayers)
			{
				if (!sickPlayers.Contains(mobSickness))
					sickPlayers.Add(mobSickness);
			}
		}

		// Remove this player from the sick players
		public void UnregisterHealedPlayer(MobSickness mobSickness)
		{
			lock (sickPlayers)
			{
				if (!sickPlayers.Contains(mobSickness))
					sickPlayers.Remove(mobSickness);
			}
		}
	}
}