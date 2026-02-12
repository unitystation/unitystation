using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Logs;
using Mirror;
using NaughtyAttributes;
using Newtonsoft.Json;
using SecureStuff;
using UnityEngine;
using US13.Core.GameGizmos;
using US13.Core.ObjectConnection;
using US13.Managers.MatrixManager;
using US13.MapSaver;
using US13.Variable_Viewer;
using Util;

namespace US13.Systems.MaintRooms
{
	public class MaintRoomGuarantor : NetworkBehaviour, IMultitoolSlaveable
	{
		[SerializeField, SyncVar(hook = nameof(SyncMaintGenerator))]
		private MaintGenerator maintGenerator;

		[SerializeField,Tooltip("The room chosen by this object will override the room choice at ONE of these generators")]
        private List<MaintRoomGenerator> possibleRoomsToOverride;


		[SerializeField, Tooltip("The list of possible rooms to guarantee. Can be used to create variants of a room needed to spawn")]
		private List<WeightedRoomEntry> possibleRoomsWeighted = new List<WeightedRoomEntry>();

		private MaintRoomSO selectedRoom = null;

		public void SyncMaintGenerator(MaintGenerator oldGen, MaintGenerator newGen)
		{
			if (maintGenerator == newGen) return;
			maintGenerator = newGen;


			if (oldGen != null)
			{
				oldGen.RemoveGuarantor(this);
			}

			if (maintGenerator == null) return;
			maintGenerator.AddGuarantor(this);
		}

		#region Generation

		public void SelectRoom()
		{
			if(PickWeightedRoom(out selectedRoom) == false) return;

			MaintRoomGenerator roomLocation = possibleRoomsToOverride.PickRandom();
			roomLocation.SelectRoom(selectedRoom);
		}

		private bool PickWeightedRoom(out MaintRoomSO room)
		{
			room = null;

			int totalWeight = 0;
			int chosenWeight = 0;
			int currentTotal = 0;

			foreach (WeightedRoomEntry entry in possibleRoomsWeighted)
			{
				totalWeight += entry.weight;
			}


			chosenWeight = UnityEngine.Random.Range(0, totalWeight + 1);

			for (int i = 0; i < possibleRoomsWeighted.Count; i++)
			{
				WeightedRoomEntry entry = possibleRoomsWeighted[i];
				currentTotal += entry.weight;
				if (chosenWeight > currentTotal) continue;
				room = entry.roomToSpawn;
				int startingIndex = i;
				while (MaintRoomSO.TryRegisterRoomAsSpawned(room) == false)
				{
					i++;
					if (i >= possibleRoomsWeighted.Count) i = 0;
					if (i == startingIndex) return false;

					room = possibleRoomsWeighted[i].roomToSpawn;
				}

				return true;
			}
			return false;
		}

		#endregion

		#region Connection

		public MultitoolConnectionType ConType => MultitoolConnectionType.MaintGeneratorExclusionZone;
		public bool CanRelink => true;

		public IMultitoolMasterable Master
		{
			get => maintGenerator as MaintGenerator;
			set { maintGenerator = value as MaintGenerator; }
		}

		public bool RequireLink => true;

		public bool TrySetMaster(GameObject performer, IMultitoolMasterable master)
		{
			Master = master;
			if (Master != null)
			{
				var generator = (Master as MaintGenerator);
				generator?.RemoveGuarantor(this);
			}

			Master = master;
			if (Master != null)
			{
				var generator = (Master as MaintGenerator);
				generator?.AddGuarantor(this);
			}
			return true;
		}

		public void SetMasterEditor(IMultitoolMasterable master)
		{
			Master = master;
		}

		#endregion
	}
}