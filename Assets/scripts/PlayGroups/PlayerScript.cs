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
		Behaviour[] componentsToDisable;

		void Start()
		{
			if (!isLocalPlayer) {
				for (int i = 0; i < componentsToDisable.Length; i++) {
					componentsToDisable[i].enabled = false;
				}
			} else {
				StartCoroutine("WaitForMapLoad");
				//TODO: Player name adding
				gameObject.name = "user-uNet";
				if (!UIManager.Instance.playerListUIControl.window.activeInHierarchy) {
					UIManager.Instance.playerListUIControl.window.SetActive(true);
				}
				//Add it to the global playerlist
				PlayerList.Instance.AddPlayer(gameObject);
			}
		}

		//This fixes the bug of master client setting equipment before the UI is read (because it is the one that loads the map)
		IEnumerator WaitForMapLoad()
		{
			yield return new WaitForSeconds(1f);
			PlayerManager.SetPlayerForControl(this.gameObject);
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
