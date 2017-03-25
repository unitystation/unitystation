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
        [SyncVar]
        public string playerName = " ";

        public override void OnStartClient()
        {
            Init();
            base.OnStartClient();
        }

        //isLocalPlayer is always called after OnStartClient
        public override void OnStartLocalPlayer()
        {
            Init();
            base.OnStartLocalPlayer();
        }

        void Init(){
            if (isLocalPlayer)
            { 
                for (int i = 0; i < componentsToEnable.Length; i++) {
                    componentsToEnable[i].enabled = true;
                }
                StartCoroutine("WaitForMapLoad");
                if (!UIManager.Instance.playerListUIControl.window.activeInHierarchy)
                {
                    UIManager.Instance.playerListUIControl.window.SetActive(true);
                }
                SetName(PlayerPrefs.GetString("PlayerName"));
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

        [Command]
        public void CmdSetPlayerName(string name){
            playerName = name;
        }

        void SetName(string name){
            //Add it to the global playerlist
            PlayerList.Instance.AddPlayer(gameObject, name);
        }
    }
}
