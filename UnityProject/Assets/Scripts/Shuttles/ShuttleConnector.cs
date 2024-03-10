using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;
using Objects.Construction;


public class ShuttleConnector : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	public ShuttleConnector ConnectedToConnector;


	public MatrixMove RelatedMove;

	public RegisterTile RegisterTile;


	public SpriteHandler SpriteHandler;


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
	    Reregister();
		RegisterTile = this.GetComponent<RegisterTile>();
		RegisterTile.OnParentChangeComplete.AddListener(Reregister);
		var WrenchSecurable = this.GetComponent<WrenchSecurable>();
		WrenchSecurable.OnAnchoredChange.AddListener(Disconnect);
    }


    public void Reregister()
    {
	    Disconnect();
	    var NewRelatedMove = this.GetComponentInParent<MatrixMove>();
	    if (RelatedMove != null)
	    {
		    RelatedMove.NetworkedMatrixMove.RemoveConnector(this);
	    }

	    if (NewRelatedMove != null)
	    {
		    RelatedMove = NewRelatedMove;
		    NewRelatedMove.NetworkedMatrixMove.AddConnector(this);
	    }
    }
	[NaughtyAttributes.Button]

	public void Disconnect()
	{

		if (ConnectedToConnector != null)
		{
			SpriteHandler.SetCatalogueIndexSprite(0);
			ConnectedToConnector.SpriteHandler.SetCatalogueIndexSprite(0);

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

				SpriteHandler.SetCatalogueIndexSprite(1);
				ConnectedToConnector = Connector;
				Connector.SpriteHandler.SetCatalogueIndexSprite(1);
				Connector.ConnectedToConnector = this;
			}
		}
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (interaction.HandObject != null) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (ConnectedToConnector != null)
		{
			Disconnect();
		}
		else
		{
			TryConnectAdjacent();
		}
	}
}
