using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Logs;
using Mirror;
using Shuttles;
using TileManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using System.Linq;


public class ShuttleConnector : NetworkBehaviour
{
	public ShuttleConnector ConnectedToConnector;


	public MatrixMove RelatedMove;

	public RegisterTile RegisterTile;


	public List<Vector2> Directions = new List<Vector2>(){
	
		new Vector2(1,0),
		new Vector2(-1,0),
		new Vector2(0,1),
		new Vector2(0,-1),
	};

	public void OnDestroy()
	{
		if (RelatedMove != null)
		{
			RelatedMove.NetworkedMatrixMove.RemoveConnector(this);
		}
		if (ConnectedToConnector != null)
		{
			ConnectedToConnector.ConnectedToConnector = null;
		}

	}


	// Start is called before the first frame update
    void Start()
    {
	    RelatedMove = this.GetComponentInParent<MatrixMove>();
	    if (RelatedMove != null)
	    {
		    RelatedMove.NetworkedMatrixMove.AddConnector(this);
	    }
		RegisterTile = this.GetComponent<RegisterTile>();
    }

	[NaughtyAttributes.Button]

	public void Disconnect()
	{

		if (ConnectedToConnector != null)
		{
			ConnectedToConnector.ConnectedToConnector = null;
			ConnectedToConnector = null;
		}
	}


	[NaughtyAttributes.Button]
	public void TryConnectAdjacent()
	{
		foreach( var  Direction in Directions){
			var lookat = this.transform.position + Direction.To3();
			var  Matrix = MatrixManager.AtPoint(lookat,true);

			if (RegisterTile.Matrix.MatrixInfo == Matrix || MatrixManager.Instance.spaceMatrix.MatrixInfo == Matrix){
				continue;
			}
			var LocalLookAt = lookat.ToLocalInt(Matrix);
			var Connector =  Matrix.Matrix.GetFirst<ShuttleConnector>(LocalLookAt, true);
			if (Connector != null)
			{
				if (Connector.RelatedMove.NetworkedMatrixMove.ConnectedShuttleConnectors.Any(x => x.ConnectedToConnector != null)) continue;

				ConnectedToConnector = Connector;
				Connector.ConnectedToConnector = this;
			}
		}
	}
}
