using Logs;
using NaughtyAttributes;
using UnityEngine;
using Objects.Atmospherics;
using Tilemaps.Behaviours.Layers;
using Objects.Disposals;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Used for stacking tiles Since thats what happens in the Underfloor stuff
/// </summary>
[ExecuteInEditMode]
public class UnderFloorLayer : Layer
{
#if UNITY_EDITOR

	[Button]
	public void Convert()
	{
		var electricalLayer = transform.parent.GetComponentInChildren<ElectricalLayer>();
		var pipeLayer = transform.parent.GetComponentInChildren<PipeLayer>();
		var disposalsLayer = transform.parent.GetComponentInChildren<DisposalsLayer>();

		if (electricalLayer == null)
		{
			Loggy.LogError($"Missing electrical layer!");
			return;
		}

		if (pipeLayer == null)
		{
			Loggy.LogError($"Missing pipe layer!");
			return;
		}

		if (disposalsLayer == null)
		{
			Loggy.LogError($"Missing disposals layer!");
			return;
		}

		//Clear them first?
		electricalLayer.Tilemap.ClearAllTiles();
		pipeLayer.Tilemap.ClearAllTiles();
		disposalsLayer.Tilemap.ClearAllTiles();

		foreach (var coord in tilemap.cellBounds.allPositionsWithin)
		{
			var tile = tilemap.GetTile(coord);
			var tileColour = tilemap.GetColor(coord);
			var tileMatrix = tilemap.GetTransformMatrix(coord);

			if (tile is ElectricalCableTile electrical)
			{
				electricalLayer.Tilemap.SetTile(coord, electrical);
				electricalLayer.Tilemap.SetColor(coord, tileColour);
				electricalLayer.Tilemap.SetTransformMatrix(coord, tileMatrix);
				continue;
			}

			if (tile is PipeTile pipeTile)
			{
				pipeLayer.Tilemap.SetTile(coord, pipeTile);
				pipeLayer.Tilemap.SetColor(coord, tileColour);
				pipeLayer.Tilemap.SetTransformMatrix(coord, tileMatrix);
				continue;
			}

			if (tile is DisposalPipe disposalPipe)
			{
				disposalsLayer.Tilemap.SetTile(coord, disposalPipe);
				disposalsLayer.Tilemap.SetColor(coord, tileColour);
				disposalsLayer.Tilemap.SetTransformMatrix(coord, tileMatrix);
			}
		}

		var currentPos = gameObject.transform.localPosition;
		electricalLayer.gameObject.transform.localPosition = currentPos;
		pipeLayer.gameObject.transform.localPosition = currentPos;
		disposalsLayer.gameObject.transform.localPosition = currentPos;

		EditorUtility.SetDirty(electricalLayer);
		EditorUtility.SetDirty(pipeLayer);
		EditorUtility.SetDirty(disposalsLayer);

		//Clear underfloor layer
		tilemap.ClearAllTiles();
		EditorUtility.SetDirty(this);

		EditorSceneManager.MarkSceneDirty(gameObject.scene);
		EditorSceneManager.SaveScene(gameObject.scene);
	}

#endif
}
