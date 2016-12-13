using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI;
using PlayGroup;

namespace Items
{
    public class ItemNetwork : MonoBehaviour
    {
        
        private Vector3 lastPos;
        //Catch the last pos of the transform at the end of the frame
        [HideInInspector]
        public PhotonView photonView;
          
        void Start()
        {
            photonView = gameObject.AddComponent<PhotonView>();
            lastPos = transform.position;
        }

         
        void LateUpdate()
        {
            if (photonView != null)
            {
                if (transform.position != lastPos) //if the item has been moved by someone then update its transform to all other clients
                {
                    CallRemoteMethod(transform.position, transform.rotation);
                }
            }
            lastPos = transform.position;

        }

        public void CallRemoteMethod(Vector3 pos, Quaternion rot)
        {
            GetComponent<PhotonView>().RPC(
                "UpdateItemTransform",
                PhotonTargets.AllBufferedViaServer,
                new object[] { pos, rot });


        }

        [PunRPC] 
        void UpdateItemTransform(Vector3 pos, Quaternion rot) //Called on all clients for this PhotonView ID
        {
            transform.position = pos;
            transform.rotation = rot;

        }

        // THIS IS AN EXAMPLE ON HOW TO USE SERIALIZEVIEW
        //        void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        //        {
        //
        //            if (stream.isWriting)
        //            {
        //                stream.SendNext(transform.position);
        //                stream.SendNext(transform.localScale);
        //
        //            }
        //            else
        //            {
        //                transform.position = (Vector3)stream.ReceiveNext();
        //                transform.localScale = (Vector3)stream.ReceiveNext();
        //
        //            }
        //
        //        }

    }
}