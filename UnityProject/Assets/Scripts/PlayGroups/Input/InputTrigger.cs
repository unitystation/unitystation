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
            Trigger(transform.localPosition);
        }
        
        
        public void Trigger(Vector3 position)
        {
            Interact(position);
        }

        private void Interact(Vector3 position)
        {
            Interact(PlayerManager.LocalPlayerScript.gameObject, position, UIManager.Hands.CurrentSlot.eventName);
        }

        public void Interact(GameObject originator, string hand)
        {
            Interact(originator, transform.localPosition, hand);
        }

        public abstract void Interact(GameObject originator, Vector3 position, string hand);
    }
}