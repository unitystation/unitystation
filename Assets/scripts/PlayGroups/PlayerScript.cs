using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UI;

namespace PlayGroup
{
	public class PlayerScript: NetworkBehaviour
	{
		// the maximum distance the player needs to be to an object to interact with it
		public float interactionDistance = 2f;
		[SerializeField]
		Behaviour[] componentsToEnable;
		[SyncVar(hook = "OnNameChange")]
		public string playerName = " ";

		public override void OnStartClient()
		{
			//Local player is set a frame or two after OnStartClient
			//Wait to check if this is local player
			StartCoroutine(CheckIfNetworkPlayer());
			base.OnStartClient();
		}

		//isLocalPlayer is always called after OnStartClient
		public override void OnStartLocalPlayer()
		{
			Init();
			base.OnStartLocalPlayer();
		}

		void Init()
		{
			if (isLocalPlayer) { 
				for (int i = 0; i < componentsToEnable.Length; i++) {
					componentsToEnable[i].enabled = true;
				}
				if (!UIManager.Instance.playerListUIControl.window.activeInHierarchy) {
					UIManager.Instance.playerListUIControl.window.SetActive(true);
				}
				PlayerManager.SetPlayerForControl(this.gameObject);
				CmdTrySetName(PlayerPrefs.GetString("PlayerName"));
			}
		}

		[Command]
		void CmdTrySetName(string name)
		{
			playerName = PlayerList.Instance.CheckName(name);
		}
		// On playerName variable change across all clients, make sure obj is named correctly
		// and set in Playerlist for that client
		public void OnNameChange(string newName)
		{
			gameObject.name = newName;
			if (!PlayerList.Instance.connectedPlayers.ContainsKey(newName)) {
				PlayerList.Instance.connectedPlayers.Add(newName, gameObject);
			}
			transform.parent = PlayerList.Instance.transform;
			PlayerList.Instance.RefreshPlayerListText();
		}

		//This fixes the bug of master client setting equipment before the UI is read (because it is the one that loads the map)
		IEnumerator CheckIfNetworkPlayer()
		{
			yield return new WaitForSeconds(1f);
			if(!isLocalPlayer){
				OnNameChange(playerName);
			}
		}

		public float DistanceTo(Vector3 position)
		{
			return (transform.position - position).magnitude;
		}

		public bool IsInReach(Transform transform)
		{
			return DistanceTo(transform.position) <= interactionDistance;
		}
	
	}
}
