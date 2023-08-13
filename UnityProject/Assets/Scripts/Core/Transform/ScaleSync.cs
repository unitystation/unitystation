using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Scripts.Core.Transform
{

    public class ScaleSync : NetworkBehaviour
    {
	    [SyncVar(hook = nameof(SyncScale))]
	    private Vector3 scaleTransform = new Vector3(1f, 1f, 1f);

	    public override void OnStartClient()
	    {
		    SyncScale(transform.localScale, scaleTransform);
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

