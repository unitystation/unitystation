﻿using System;
using System.Collections;
using System.Collections.Generic;
using Antagonists;
using HealthV2;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Blob
{
	/// <summary>
	/// Class that controls the blob events before blob emerges
	/// </summary>
	public class BlobStarter : NetworkBehaviour
	{

		public bool bypass = false;

		public bool bypass2 = false;

		private BlobStates state = BlobStates.Start;

		private BlobBody bodyPart = BlobBody.Stomach;

		private float stateTimer = 0f;

		private float internalTimer = 0f;

		private const float updateTime = 1f;

		private const float MessageFrequency = 20f;

		private const float TimeToMiddle = 60f;

		private const float TimeToEnd = 240f;

		private bool lastMsg;

		private List<string> GenericMiddlePhrases = new List<string>
		{
			"You feel in pain, and want to hide away",
			"You want to lay down in the dark",
			"You want to sit down somewhere quiet",
			"You keep quiet",
			"You feel safer alone",
			"You feel in pain"
		};

		private List<string> middlePhrasesStomach = new List<string>
		{
			"Your stomach hurts",
			"You feel your insides in pain, but dont want help",
			"Your stomach gurgles"
		};

		private List<string> middlePhrasesHead = new List<string>
		{
			"Your head hurts",
			"Your stomach gurgles"
		};

		private List<string> middlePhrasesAss = new List<string>
		{
			"Your ass hurts",
			"You feel your insides in pain, but dont want help",
			"Your stomach gurgles"
		};

		private void Start()
		{
			var random = Random.Range(0,100);

			if (random >= 75)
			{
				bodyPart = BlobBody.Head;
			}
			else if (random <= 1)
			{
				bodyPart = BlobBody.Ass;
			}
		}

		private void OnEnable()
		{
			UpdateManager.Add(PeriodicUpdate, updateTime);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
		}

		private void PeriodicUpdate()
		{
			if (!CustomNetworkManager.IsServer) return;

			stateTimer += updateTime;

			internalTimer += updateTime;

			if (bypass)
			{
				FormBlob();
				return;
			}

			if (bypass2)
			{
				state = BlobStates.End;
			}

			CheckState();
		}

		#region ServerSide

		private void CheckState()
		{
			switch (state)
			{
				case BlobStates.Start:
					if (Random.Range(0, 100) <= 75) break;
					StartState();
					break;
				case BlobStates.Middle:
					MiddleState();
					break;
				case BlobStates.End:
					EndState();
					break;
				default:
					Logger.LogError("Unused state", Category.Blob);
					break;
			}
		}

		private void StartState()
		{
			string message;

			switch (bodyPart)
			{
				case BlobBody.Head:
					message = "You have a killer headache today. Wonder whats caused that.";
					break;
				case BlobBody.Ass:
					message = "Feels like you've sat on something strange recently.";
					break;
				case BlobBody.Stomach:
					message = "You feel nauseous, its probably nothing to worry about.";
					break;
				default:
					message = "You feel nauseous, its probably nothing to worry about.";
					break;
			}

			Chat.AddExamineMsgFromServer(gameObject, message);

			stateTimer = 0f;
			state = BlobStates.Middle;
		}

		private void MiddleState()
		{
			//Check to see whether middle state should start
			if(stateTimer <= TimeToMiddle) return;

			//If middle state has been running 240 seconds, change to end state!
			if (stateTimer >= TimeToEnd)
			{
				state = BlobStates.End;
				internalTimer = 0f;
				return;
			}

			//Every 20 seconds have chance for message
			if(internalTimer <= MessageFrequency) return;

			// 75% theres no message this time this second
			if (Random.Range(0, 100) <= 75) return;

			internalTimer = 0f;

			string message;

			if (Random.Range(0, 100) <= 25)
			{
				switch (bodyPart)
				{
					case BlobBody.Head:
						message = middlePhrasesHead.GetRandom();
						break;
					case BlobBody.Ass:
						message = middlePhrasesAss.GetRandom();
						break;
					case BlobBody.Stomach:
						message = middlePhrasesStomach.GetRandom();
						break;
					default:
						message = middlePhrasesStomach.GetRandom();
						break;
				}
			}
			else
			{
				message = GenericMiddlePhrases.GetRandom();
			}

			Chat.AddExamineMsgFromServer(gameObject, $"<color=#FF151F>{message}</color>");
		}

		private void EndState()
		{
			if (internalTimer >= 60)
			{
				Chat.AddActionMsgToChat(gameObject, $"<color=#FF151F>You explode from your {bodyPart}, a new being has been born.</color>",
					$"<color=#FF151F>{gameObject.ExpensiveName()} explodes into a pile of mush.</color>");
				FormBlob();
				return;
			}

			if(lastMsg) return;

			lastMsg = true;

			string message;

			switch (bodyPart)
			{
				case BlobBody.Head:
					message = "Your head hurts so bad, feels like it might explode";
					break;
				case BlobBody.Ass:
					message = "Your ass feels like its about to implode";
					break;
				case BlobBody.Stomach:
					message = "You feel like you're about to sick up your stomach";
					break;
				default:
					message = "You feel like you're about to sick up your stomach";
					break;
			}

			message += "\n You start to count down from 60 in your head...";

			Chat.AddExamineMsgFromServer(gameObject, $"<color=#FF151F>{message}</color>");
		}

		/// <summary>
		/// Spawn blob player and gib old body!
		/// </summary>
		private void FormBlob()
		{
			var playerScript = gameObject.GetComponent<PlayerScript>();

			if (playerScript.IsDeadOrGhost) return;

			var bound = MatrixManager.MainStationMatrix.Bounds;

			//Teleport user to random location on station if outside radius of 600 or on a space tile
			if (((gameObject.AssumedWorldPosServer() - MatrixManager.MainStationMatrix.GameObject.AssumedWorldPosServer())
				.magnitude > 600f) || MatrixManager.IsSpaceAt(gameObject.GetComponent<PlayerSync>().ServerPosition, true))
			{
				Vector3 position = new Vector3(Random.Range(bound.xMin, bound.xMax), Random.Range(bound.yMin, bound.yMax), 0);
				while (MatrixManager.IsSpaceAt(Vector3Int.FloorToInt(position), true) || MatrixManager.IsWallAtAnyMatrix(Vector3Int.FloorToInt(position), true))
				{
					position = new Vector3(Random.Range(bound.xMin, bound.xMax), Random.Range(bound.yMin, bound.yMax), 0);
				}

				gameObject.GetComponent<PlayerSync>().SetPosition(position, true);
			}

			var spawnResult = Spawn.ServerPrefab(AntagManager.Instance.blobPlayerViewer, gameObject.GetComponent<PlayerSync>().ServerPosition, gameObject.transform.parent);

			if (!spawnResult.Successful)
			{
				Logger.LogError("Failed to spawn blob!", Category.Blob);
				return;
			}

			spawnResult.GameObject.GetComponent<PlayerScript>().mind = playerScript.mind;

			playerScript.mind = null;

			PlayerSpawn.ServerTransferPlayerToNewBody(connectionToClient, spawnResult.GameObject, gameObject, EVENT.BlobSpawned, playerScript.characterSettings);

			//Start the blob control script
			spawnResult.GameObject.GetComponent<BlobPlayer>().BlobStart();

			gameObject.GetComponent<LivingHealthMasterBase>().Harvest();

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
			Destroy(this);
		}

		#endregion
	}

	public enum BlobStates
	{
		Start,
		Middle,
		End
	}

	public enum BlobBody
	{
		Stomach,
		Head,
		Ass
	}
}
