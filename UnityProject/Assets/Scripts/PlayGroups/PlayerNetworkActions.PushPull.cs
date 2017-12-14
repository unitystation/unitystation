using PlayGroup;
using UnityEngine;
using UnityEngine.Networking;

public partial class PlayerNetworkActions : NetworkBehaviour
{
    [HideInInspector] public bool isPulling;

    [Command]
    public void CmdPullObject(GameObject obj)
    {
        if (isPulling)
        {
            var cObj = gameObject.GetComponent<PlayerSync>().pullingObject;
            cObj.GetComponent<PushPull>().pulledBy = null;
            gameObject.GetComponent<PlayerSync>().pullObjectID = NetworkInstanceId.Invalid;
        }

        var pulled = obj.GetComponent<PushPull>();

        //check if the object you want to pull is another player
        if (pulled.isPlayer)
        {
            var playerS = obj.GetComponent<PlayerSync>();
            //Anything that the other player is pulling should be stopped
            if (playerS.pullingObject != null)
            {
                var otherPNA = obj.GetComponent<PlayerNetworkActions>();
                otherPNA.CmdStopOtherPulling(playerS.pullingObject);
            }
        }
        //Other player is pulling object, send stop on that player
        if (pulled.pulledBy != null)
        {
            if (pulled.pulledBy != gameObject)
            {
                pulled.GetComponent<PlayerNetworkActions>().CmdStopPulling(obj);
            }
        }


        if (pulled != null)
        {
            var pS = GetComponent<PlayerSync>();
            pS.pullObjectID = pulled.netId;
            isPulling = true;
        }
    }

    //if two people try to pull the same object
    [Command]
    public void CmdStopOtherPulling(GameObject obj)
    {
        var objA = obj.GetComponent<PushPull>();
        if (objA.pulledBy != null)
        {
            objA.pulledBy.GetComponent<PlayerNetworkActions>().CmdStopPulling(obj);
        }
    }

    [Command]
    public void CmdStopPulling(GameObject obj)
    {
        if (!isPulling)
        {
            return;
        }

        isPulling = false;
        var pulled = obj.GetComponent<PushPull>();
        if (pulled != null)
        {
            //			//this triggers currentPos syncvar hook to make sure registertile is been completed on all clients
            //			pulled.currentPos = pulled.transform.position;

            var pS = gameObject.GetComponent<PlayerSync>();
            pS.pullObjectID = NetworkInstanceId.Invalid;
            pulled.pulledBy = null;
        }
    }

    [Command]
    public void CmdTryPush(GameObject obj, Vector3 pos)
    {
        var pushed = obj.GetComponent<PushPull>();
        if (pushed != null)
        {
            pushed.serverPos = pos;
        }
    }
}