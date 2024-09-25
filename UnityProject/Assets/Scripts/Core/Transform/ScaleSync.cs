using System;
using Mirror;
using SecureStuff;
using UnityEngine;

namespace Scripts.Core.Transform
{

    public class ScaleSync : NetworkBehaviour
    {
	    [SyncVar(hook = nameof(SyncScale)), SerializeField]
	    [PlayModeOnly] private Vector3 scaleTransform = new Vector3(1f, 1f, 1f);



	    public override void OnStartClient()
	    {
		    if (CustomNetworkManager.IsServer)
		    {
			    SyncScale(transform.localScale, transform.localScale);
		    }
		    else
		    {
			    SyncScale(transform.localScale, scaleTransform);
		    }

		    base.OnStartClient();
	    }


	    public void SetScale(Vector3 newScale)
	    {
		    SyncScale(transform.localScale, newScale);
	    }

	    private void SyncScale(Vector3 oldVec, Vector3 newVec)
	    {
		    scaleTransform = newVec;
		    transform.localScale = newVec;
	    }
    }
}

