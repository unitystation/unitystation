using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using Objects.Atmospherics;
using Tilemaps.Behaviours.Layers;
using UnityEditor.SceneManagement;

#if UNITY_EDITOR
using Objects.Disposals;
using Tiles;
using UnityEditor;
#endif

/// <summary>
/// Used for stacking tiles Since thats what happens in the Underfloor stuff
/// </summary>
[ExecuteInEditMode]
public class UnderFloorLayer : Layer
{
	public ElectricalLayer ElectricalLayer;

	public PipeLayer PipeLayer;

	public DisposalsLayer DisposalsLayer;

#if UNITY_EDITOR

	[Button]
	public void Convert()
	{
		if (ElectricalLayer == null)
		{
			Logger.LogError($"Missing electrical layer!");
			return;
		}

		if (PipeLayer == null)
		{
			Logger.LogError($"Missing pipe layer!");
			return;
		}

		if (DisposalsLayer == null)
		{
			Logger.LogError($"Missing disposals layer!");
			return;
		}

		//Clear them first?
		ElectricalLayer.Tilemap.ClearAllTiles();
		PipeLayer.Tilemap.ClearAllTiles();
		DisposalsLayer.Tilemap.ClearAllTiles();

		foreach (var coord in tilemap.cellBounds.allPositionsWithin)
		{
			var tile = tilemap.GetTile(coord);
			var tileColour = tilemap.GetColor(coord);
			var tileMatrix = tilemap.GetTransformMatrix(coord);

			if (tile is ElectricalCableTile electrical)
			{
				ElectricalLayer.Tilemap.SetTile(coord, electrical);
				ElectricalLayer.Tilemap.SetColor(coord, tileColour);
				ElectricalLayer.Tilemap.SetTransformMatrix(coord, tileMatrix);
				continue;
			}

			if (tile is PipeTile pipeTile)
			{
				PipeLayer.Tilemap.SetTile(coord, pipeTile);
				PipeLayer.Tilemap.SetColor(coord, tileColour);
				PipeLayer.Tilemap.SetTransformMatrix(coord, tileMatrix);
				continue;
			}

			if (tile is DisposalPipe disposalPipe)
			{
				DisposalsLayer.Tilemap.SetTile(coord, disposalPipe);
				DisposalsLayer.Tilemap.SetColor(coord, tileColour);
				DisposalsLayer.Tilemap.SetTransformMatrix(coord, tileMatrix);
			}
		}

		EditorUtility.SetDirty(ElectricalLayer);
		EditorUtility.SetDirty(PipeLayer);
		EditorUtility.SetDirty(DisposalsLayer);
		EditorSceneManager.MarkSceneDirty(gameObject.scene);
		EditorSceneManager.SaveScene(gameObject.scene);
	}

#endif
}
