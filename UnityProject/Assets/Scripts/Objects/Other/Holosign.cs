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

		private void OnDestruction(DestructionInfo info)
		{
			if(projector == null) return;

			projector.RemoveHolosign(this);
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			if(projector == null) return;

			projector.RemoveHolosign(this);
		}
	}
}