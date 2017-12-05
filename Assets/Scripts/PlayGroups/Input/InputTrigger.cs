using System;
using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.Networking;

namespace InputControl
{

    public abstract class InputTrigger : NetworkBehaviour
    {
        //		public static readonly Dictionary<NetworkInstanceId, float> interactCache 
        //			= new Dictionary<NetworkInstanceId, float>(10);
        //
        //		public static void Touch(GameObject gameObject)
        //		{
        //			if ( !gameObject ) return;
        //			var networkIdentity = gameObject.GetComponent<NetworkIdentity>();
        //			if ( !networkIdentity ) return;
        //			var networkInstanceId = networkIdentity.netId;
        //			var time = Time.time;
        ////			Debug.LogFormat("Touched {0}({1}) at {2}", gameObject.name, networkInstanceId, time);
        //			Touch(networkInstanceId, time);
        //		}
        //
        //		private static void Touch(NetworkInstanceId objectId, float time)
        //		{
        //			interactCache[objectId] = time;
        //		}

        public void Trigger()
        {
            Interact();
        }

        private void Interact()
        {
            Interact(PlayerManager.LocalPlayerScript.gameObject, UIManager.Hands.CurrentSlot.eventName);
        }

        public abstract void Interact(GameObject originator, string hand);
    }
}