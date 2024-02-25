using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatrixMove : MonoBehaviour
{

	private NetworkedMatrixMove networkedMatrixMove;

	public NetworkedMatrixMove NetworkedMatrixMove
	{
		get
		{
			if (networkedMatrixMove == null)
			{
				networkedMatrixMove = transform.GetChild(1).GetComponent<NetworkedMatrixMove>();
			}
			return networkedMatrixMove;
		}
	}
}
