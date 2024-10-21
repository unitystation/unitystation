using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Admin.Logs;
using HealthV2;
using UnityEngine;
using Random = UnityEngine.Random;
using Health.Sickness;
using Items.Others;
using Logs;
using Mirror;
using Newtonsoft.Json;
using Systems.Character;
using UI.Objects.Medical.Cloning;

namespace Objects.Medical
{
	/// <summary>
	/// Main component for cloning console.
	/// </summary>
	[RequireComponent(typeof(ItemStorage))]
	public class CloningConsole : MonoBehaviour, ICheckedInteractable<HandApply>, IServerSpawn
	{
		[SerializeField] private GameObject paperPrefab;

		private DNAScanner scanner;
		/// <summary>
		/// Scanner this is attached to. Null if none found.
		/// </summary>
		public DNAScanner Scanner => scanner;

		private CloningPod cloningPod;
		/// <summary>
		/// Cloning pod this is attached to. Null if none found.
		/// </summary>
		public CloningPod CloningPod => cloningPod;

		private CloningRecord currentRecord;
		public CloningRecord CurrentRecord => currentRecord;

		private GUI_Cloning consoleGUI;
		private RegisterTile registerTile;
		private ClosetControl closet;
		private ItemStorage recordsStorage;
		private HasNetworkTab networkTab;

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			closet = GetComponent<ClosetControl>();
			recordsStorage = GetComponent<ItemStorage>();
			networkTab = GetComponent<HasNetworkTab>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			scanner = null;
			cloningPod = null;
			consoleGUI = null;

			//TODO: Support persistance of this info somewhere, such as to a circuit board.
			//scan for adjacent dna scanner and cloning pod
			StartCoroutine(CheckForAdjacentScanners());

			if (cloningPod)
			{
				cloningPod.console = this;
			}
		}

		private IEnumerator CheckForAdjacentScanners()
		{
			var checks = 0;
			while (checks <= 4)
			{
				scanner = MatrixManager.GetAdjacent<DNAScanner>(registerTile.WorldPositionServer, true).FirstOrDefault();
				cloningPod = MatrixManager.GetAdjacent<CloningPod>(registerTile.WorldPositionServer, true).FirstOrDefault();
				if (scanner != null && cloningPod != null)
				{
					break;
				}
				yield return WaitFor.Seconds(0.5f);
				checks++;
			}

			Chat.AddActionMsgToChat(gameObject,
				scanner == null
					? $"The {gameObject.ExpensiveName()} beeps an error code, indicating that it cannot find a nearby scanner to connect to."
					: $"The {gameObject.ExpensiveName()} beeps a succes code, indicating that it connected to the {scanner.gameObject.ExpensiveName()} succesfully.");

			Chat.AddActionMsgToChat(gameObject,
				cloningPod == null
					? $"The {gameObject.ExpensiveName()} beeps an error code, indicating that it cannot find a cloning pod nearby."
					: $"The {gameObject.ExpensiveName()} beeps a succes code, indicating that it connected to the {cloningPod.gameObject.ExpensiveName()} succesfully.");
		}

		/// <summary>
		/// Toggle the locking / unlocking of the scanner.
		/// </summary>
		public void ServerToggleLock()
		{
			if (Inoperable())
			{
				UpdateInoperableStatus();
				return;
			}
			if (closet.IsOpen == false)
			{
				closet.SetLock(closet.IsLocked ? ClosetControl.Lock.Unlocked : ClosetControl.Lock.Locked);
				scanner.statusString = closet.IsLocked ? "Scanner locked." : "Scanner unlocked.";
			}
			else
			{
				scanner.statusString = "Scanner is not closed.";
			}
		}

		[RightClickMethod()]
		[NaughtyAttributes.Button()]

		public void Scan()
		{
			if (Inoperable())
			{
				UpdateInoperableStatus();
				return;
			}

			if (scanner.occupant)
			{
				var mob = scanner.occupant;
				var mobID = scanner.occupant.mobID;
				var playerScript = mob.GetComponent<PlayerScript>();
				if (playerScript.OrNull()?.Mind?.bodyMobID != mobID)
				{
					scanner.statusString = "Bad mind/body interface.";
					return;
				}

				CreateRecord(mob, playerScript);
				scanner.statusString = "Subject successfully scanned.";
			}
			else
			{
				scanner.statusString = "Scanner is empty.";
			}
		}

		private bool Inoperable()
		{
			return !(scanner && scanner.RelatedAPC && scanner.Powered);
		}

		private void UpdateInoperableStatus()
		{
			if (scanner == null)
			{
				Loggy.LogError("[CloningConsole/UpdateInoperableStatus()] - The scanner is not connected to this console.");
				Chat.AddActionMsgToChat(gameObject,
					$"A {gameObject.ExpensiveName()} crashes momentarily before coming back to life with an error that says 'No scanner connected..'");
				StartCoroutine(CheckForAdjacentScanners());
				return;
			}
			if (scanner.RelatedAPC == null)
			{
				scanner.statusString = "Scanner not connected to APC!";
				return;
			}

			if (scanner.Powered == false)
			{
				scanner.statusString = "Voltage too low!";
			}
		}

		public void ServerTryClone(CloningRecord record)
		{
			if (cloningPod && cloningPod.CanClone())
			{
				Mind mind = NetworkUtils.FindObjectOrNull(record.mindID)?.GetComponent<Mind>();
				if (mind == null)
				{
					return;
				}
				else
				{
					record.mind = mind;
				}
				CloneableStatus status = mind.GetCloneableStatus(record.mobID);

				if (status == CloneableStatus.Cloneable)
				{
					cloningPod.ServerStartCloning(record);
					recordsStorage.ServerDropAll();
					consoleGUI.ViewMainPage();
				}
				else
				{
					cloningPod.UpdateStatusString(status);
				}
			}
		}

		private void CreateRecord(LivingHealthMasterBase livingHealth, PlayerScript playerScript)
		{
			var record1 = new CloningRecord();
			record1.UpdateRecord(livingHealth, playerScript);
			AdminLogsManager.AddNewLog(
				null,
				$"{gameObject.ExpensiveName()} at {gameObject.AssumedWorldPosServer()}" +
				$" has created a new cloning record for {playerScript.playerName}.",
				LogCategory.RoundFlow);
			var paper1 = Spawn.ServerPrefab(paperPrefab, gameObject.AssumedWorldPosServer());
			paper1.GameObject.GetComponent<Paper>().SetServerString(record1.Copy());
		}

		public void UpdateDisplay()
		{
			consoleGUI.OrNull()?.UpdateDisplay();
		}

		public void RegisterConsoleGUI(GUI_Cloning guiCloning)
		{
			consoleGUI = guiCloning;
		}

		public void RemoveRecord()
		{
			recordsStorage.ServerDropAll();
			currentRecord = null;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject == null)
			{
				networkTab.ServerPerformInteraction(interaction);
				return;
			}
			if (interaction.HandObject.TryGetComponent<Paper>(out var p) == false) return;
			var record = CloningRecord.FromString(p.ServerString);
			if (record == null)
			{
				Chat.AddExamineMsg(interaction.Performer, "The console spits the paper out back into your hand..");
				return;
			}
			else
			{
				Chat.AddExamineMsg(interaction.Performer, "The paper disappears wihtin the cloning console.");
			}
			recordsStorage.ServerTryTransferFrom(interaction.HandSlot);
			currentRecord = record;
			consoleGUI?.UpdateDisplay();
		}
	}
}
