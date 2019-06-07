using UnityEngine;
using UnityEngine.Networking;
[RequireComponent(typeof(Pickupable))]
public class CrowbarTrigger : InputTrigger
{
    public override bool Interact (GameObject originator, Vector3 position, string hand)
    {
        //TODO:  Remove after IF2 refactor

        return false;
    }
}