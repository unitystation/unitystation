using System;
using UnityEngine;
using Systems.Cargo;
using Mirror;
using Shared.Systems.ObjectConnection;

namespace Objects.Research
{
	public class ArtifactConsole : NetworkBehaviour, IMultitoolSlaveable
	{
		private ItemStorage itemStorage;

		public Artifact ConnectedArtifact { get; set; }

		[NonSerialized, SyncVar(hook = nameof(SyncConsoleData))] internal ArtifactData InputData = new ArtifactData();

		public Action StateChange;
		[field: SerializeField] public bool CanRelink { get; set; } = true;
		private void Awake()
		{
			itemStorage = GetComponent<ItemStorage>();
		}

		[Command(requiresAuthority = false)]
		internal void CmdSetInputData(ArtifactData InputDataClient, NetworkConnectionToClient sender = null) //TODO This is insecure due to hacked client can say anything in ArtifactData
		{
			if (sender == null) return;
			if (Validations.CanApply(PlayerList.Instance.Get(sender).Script, this.gameObject, NetworkSide.Server, false, ReachRange.Standard) == false) return;
			InputData = InputDataClient;
		}

		[Server]
		internal void SetInputDataServer(ArtifactData InputDataServer)
		{
			InputData = InputDataServer;
		}

		private void UpdateGUI()
		{
			StateChange?.Invoke();
		}

		private void SyncConsoleData(ArtifactData oldData, ArtifactData newData)
		{
			if (oldData.Equals(newData)) return;

			InputData = newData;
			UpdateGUI();
		}

		#region Multitool Interaction

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.Artifact;
		IMultitoolMasterable IMultitoolSlaveable.Master => ConnectedArtifact;
		bool IMultitoolSlaveable.RequireLink => false;

		bool IMultitoolSlaveable.TrySetMaster(GameObject performer, IMultitoolMasterable master)
		{
			SetMaster(master);
			return true;
		}

		void IMultitoolSlaveable.SetMasterEditor(IMultitoolMasterable master)
		{
			SetMaster(master);
		}

		private void SetMaster(IMultitoolMasterable master)
		{
			if (master is Artifact arti && arti != ConnectedArtifact)
			{
				SubscribeToServerEvent(arti);
			}
			else if (ConnectedArtifact != null)
			{
				UnSubscribeFromServerEvent();
			}
			UpdateGUI();
		}

		public void SubscribeToServerEvent(Artifact arti)
		{
			UnSubscribeFromServerEvent();
			ConnectedArtifact = arti;

		}

		public void UnSubscribeFromServerEvent()
		{
			if (ConnectedArtifact == null) return;
			ConnectedArtifact = null;
		}
		#endregion
	}
}
