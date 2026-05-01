using UnityEngine;
using US13.Objects.Traps;
using US13.Systems.Clearance;
using US13.Tilemaps.Behaviours.Meta;

namespace US13.Systems.MaintRooms
{
	public class GenericBlueprintSpawner : ItemMatrixSystemInit, IGenericTrigger
	{
		public int SelectedRoom { get; private set; } = -1;
		[field: SerializeField] public string RoomListId { get; private set; } = "GenericBlueprintSpawner";

		[SerializeField] private TriggerType triggerType;
		public TriggerType TriggerType => triggerType;
		private bool triggerState = false;

		[SerializeField] private bool triggerOnSpawn = true;
		[SerializeField, Tooltip("If this spawner uses a unique list, set this to true so it can be cleaned up when no longer required.")]
		private bool clearListAfterSpawn = false;

		public override void Initialize()
		{
			if(triggerOnSpawn) TriggerGeneration();
		}

		public void OnTrigger()
		{
			switch (TriggerType)
			{
				case TriggerType.Active:
					triggerState = true;
					ChooseRoom(SelectedRoom);
					TriggerGeneration();
					break;
				case TriggerType.Toggle:
					triggerState = !triggerState;
					if (triggerState)
					{
						ChooseRoom(SelectedRoom);
						TriggerGeneration();
					}
					break;
				case TriggerType.HoldState:
					if (triggerState == false)
					{
						ChooseRoom(SelectedRoom);
						TriggerGeneration();
						triggerState = true;
					}
					break;
			}
		}
		public void OnTriggerWithClearance(IClearanceSource source) { OnTrigger(); }

		public void OnTriggerEnd()
		{
			if(TriggerType == TriggerType.Active) triggerState = false;
		}


		public void ChooseRoom(int roomToChoose = -1, string newListId = null)
		{
			if (roomToChoose == -1) BluePrintSpawner.PickWeightedRoom(RoomListId, out roomToChoose);
			else if(newListId != null) RoomListId = newListId;

			SelectedRoom = roomToChoose;
		}

		private void TriggerGeneration(bool isEditor = false)
		{
			BluePrintSpawner.SpawnRandomRoom(networkedMatrix.matrix, transform.localPosition.CutToInt(), RoomListId, isEditor, SelectedRoom);
			if (clearListAfterSpawn) BluePrintSpawner.UnregisterBlueprintList(RoomListId);

			SelectedRoom = -1;
		}
	}
}