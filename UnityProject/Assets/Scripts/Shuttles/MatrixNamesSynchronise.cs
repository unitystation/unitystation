using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MatrixNamesSynchronise : NetworkBehaviour
{
	public string MatrixNamesSet = "";

	[SyncVar(hook = nameof(SyncMatrixName))]
	public string MatrixName;

    // Start is called before the first frame update
    private void Start()
    {
	    if (string.IsNullOrEmpty(MatrixNamesSet))
	    {

		    MatrixNamesSet = transform.parent.name;
	    }

	    SyncMatrixName(MatrixNamesSet, MatrixNamesSet);
    }

    public void SyncMatrixName(string OldName, string NewVal)
    {
	    MatrixName = NewVal;
	    transform.parent.name = NewVal;
	    transform.parent.GetChild(0).name = NewVal;
    }

}
