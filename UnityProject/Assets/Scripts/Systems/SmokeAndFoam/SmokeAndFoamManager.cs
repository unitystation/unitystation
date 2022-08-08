using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using Managers;
using Tiles;
using UnityEngine;

public class SmokeAndFoamManager : SingletonManager<SmokeAndFoamManager>
{
	public LayerTile WallFoam;
	public LayerTile BaseFoam;

	public OverlayTile OverlayTileFoam;
	public OverlayTile OverlayTileSmoke;
	public void OnEnable()
	{
		UpdateManager.Add(UpdateMe, 1);
		EventManager.AddHandler(Event.RoundEnded, RoundEnd);
		UpdateManager.Add(SpreadUpdate, 0.15f);
	}

	public void OnDisable()
	{
		UpdateManager.Remove( CallbackType.PERIODIC_UPDATE,UpdateMe);
		UpdateManager.Remove( CallbackType.PERIODIC_UPDATE,SpreadUpdate);
	}

	public void RoundEnd()
	{
		ActiveNodes.Clear();
	}

	public List<SourceReservoir> ActiveNodes = new List<SourceReservoir>();

	public static void StartSmokeAt(Vector3 PositionWorld, ReagentMix Container, int AmountOfSmoke )
	{
		SourceReservoir SourceReservoir = new SourceReservoir();

		SourceReservoir.ReagentPurity = Container.Total / (Container.Total + AmountOfSmoke);
		SourceReservoir.PerTile = Container.Take(1);
		SourceReservoir.PerTile.Multiply(SourceReservoir.ReagentPurity);
		SourceReservoir.StacksLeft = AmountOfSmoke;

		var Matrix = MatrixManager.AtPoint(PositionWorld, true);
		if (Matrix == MatrixManager.Instance.spaceMatrix.MatrixInfo)
		{
			Matrix = MatrixManager.MainStationMatrix; //TODO Maybe change to proximity thing
		}

		var Node =  Matrix.MetaDataLayer.Get(PositionWorld.ToLocal(Matrix).RoundToInt(), true, true);

		SourceReservoir.SpreadToNode(null, Node.SmokeNode);
		Instance.ActiveNodes.Add(SourceReservoir);
	}

	public static void StartFoamAt(Vector3 PositionWorld, ReagentMix Container, int AmountOfFoam, bool WallFoam  = false, bool SmartFoam  = false)
	{
		FoamSourceReservoir SourceReservoir = new FoamSourceReservoir();

		SourceReservoir.ReagentPurity = Container.Total / (Container.Total + AmountOfFoam);
		SourceReservoir.PerTile = Container.Take(1);
		SourceReservoir.PerTile.Multiply(SourceReservoir.ReagentPurity);
		SourceReservoir.StacksLeft = AmountOfFoam;
		SourceReservoir.SmartFoam = SmartFoam;
		SourceReservoir.WallFoam = WallFoam;

		var Matrix = MatrixManager.AtPoint(PositionWorld, true);
		if (Matrix == MatrixManager.Instance.spaceMatrix.MatrixInfo)
		{
			Matrix = MatrixManager.MainStationMatrix; //TODO Maybe change to proximity thing
		}

		var Node =  Matrix.MetaDataLayer.Get(PositionWorld.ToLocal(Matrix).RoundToInt(), true, true);

		SourceReservoir.SpreadToNode(null, Node.FoamNode);
		Instance.ActiveNodes.Add(SourceReservoir);
	}

	public void UpdateMe()
	{
		for (int i = ActiveNodes.Count - 1; i >= 0; i--)
		{
			ActiveNodes[i].Update();
		}
	}

	public void SpreadUpdate()
	{
		foreach (var ActiveNode in ActiveNodes)
		{
			if (ActiveNode.StacksLeft > 0)
			{
				ActiveNode.SpreadUpdate();
			}
		}
	}

}
