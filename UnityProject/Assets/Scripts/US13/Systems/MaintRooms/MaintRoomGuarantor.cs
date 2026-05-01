using System.Collections.Generic;
using Mirror;
using UnityEngine;
using US13.Core.ObjectConnection;
using Util;

namespace US13.Systems.MaintRooms
{
	public class MaintRoomGuarantor : NetworkBehaviour, IMultitoolSlaveable
	{
		[SerializeField, SyncVar(hook = nameof(SyncMaintGenerator))]
		private MaintGenerator maintGenerator;

		[SerializeField,Tooltip("The room chosen by this object will override the room choice at ONE of these generators")]
        private List<MaintRoomGenerator> possibleRoomsToOverride;

		[SerializeField] private string roomListId;

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

		public void SelectRoom()
		{
			if(BluePrintSpawner.PickWeightedRoom(roomListId, out int chosenEntry) == false) return;
			MaintRoomGenerator roomLocation = possibleRoomsToOverride.PickRandom();
			roomLocation.ChooseRoom(chosenEntry, roomListId);
		}

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