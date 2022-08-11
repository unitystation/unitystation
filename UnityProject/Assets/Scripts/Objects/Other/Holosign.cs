using System;
using Items.Others;
using UnityEngine;

namespace Objects.Other
{
	public class Holosign : MonoBehaviour, IServerDespawn
	{
		private Integrity integrity;

		private HolosignProjector projector;

		private void Awake()
		{
			integrity = GetComponent<Integrity>();
		}

		private void OnDisable()
		{
			integrity.OnWillDestroyServer.RemoveListener(OnDestruction);
		}

		public void SetUp(HolosignProjector newProjector)
		{
			projector = newProjector;

			integrity.OnWillDestroyServer.AddListener(OnDestruction);
		}

		public void DestroyHolosign()
		{
			if(projector == null) return;

			projector.RemoveHolosign(this);
		}

		private void OnDestruction(DestructionInfo info)
		{
			DestroyHolosign();
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			DestroyHolosign();
		}
	}
}