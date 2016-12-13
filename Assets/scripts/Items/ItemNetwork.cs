using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI;
using PlayGroup;

namespace Items
{
    [RequireComponent(typeof (PhotonView))]
    public class ItemNetwork : MonoBehaviour
    {
        
        private Vector3 lastPos;
        //Catch the last pos of the transform at the end of the frame
        [HideInInspector]
        public PhotonView photonView;
          
        void Start()
        {
            photonView = GetComponent<PhotonView>();
            lastPos = transform.position;
        }

         
        void LateUpdate()
        {
            if (photonView != null)
            {
                if (transform.position != lastPos && PhotonNetwork.connectedAndReady) //if the item has been moved by someone then update its transform to all other clients
                {
                    CallRemoteMethod(transform.position);
                }
            }
            lastPos = transform.position;

        }

        public void CallRemoteMethod(Vector3 pos)
        {
            photonView.RPC(
                "UpdateItemTransform",
                PhotonTargets.OthersBuffered, //Called on other clients for this PhotonView ID
                new object[] { pos });


        }

        [PunRPC] 
        void UpdateItemTransform(Vector3 pos) 
        {
            if (transform.position != pos)
            {
                transform.position = pos;
                lastPos = pos;
            }

        }
    }
}