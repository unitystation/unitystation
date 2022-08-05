using NaughtyAttributes;
using UnityEngine;
using Objects.Atmospherics;
using Tilemaps.Behaviours.Layers;
using UnityEditor.SceneManagement;
using Objects.Disposals;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Used for stacking tiles Since thats what happens in the Underfloor stuff
/// </summary>
[ExecuteInEditMode]
public class UnderFloorLayer : Layer
{
	[SerializeField]
	[InfoBox("These are used to convert the UnderfloorLayer into Electrical, Pipe and Disposal layers, you need to set them before clicking convert")]
	private ElectricalLayer electricalLayer;

	[SerializeField]
	private PipeLayer pipeLayer;

	[SerializeField]
	private DisposalsLayer disposalsLayer;

#if UNITY_EDITOR

	[Button]
	public void Convert()
	{
		if (electricalLayer == null)
		{
			Logger.LogError($"Missing electrical layer!");
			return;
		}

		if (pipeLayer == null)
		{
			Logger.LogError($"Missing pipe layer!");
			return;
		}

		if (disposalsLayer == null)
		{
			Logger.LogError($"Missing disposals layer!");
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

		EditorUtility.SetDirty(electricalLayer);
		EditorUtility.SetDirty(pipeLayer);
		EditorUtility.SetDirty(disposalsLayer);
		EditorSceneManager.MarkSceneDirty(gameObject.scene);
		EditorSceneManager.SaveScene(gameObject.scene);
	}

	[Button]
	private void ClearLayer()
	{
		tilemap.ClearAllTiles();

		EditorUtility.SetDirty(this);
		EditorSceneManager.MarkSceneDirty(gameObject.scene);
		EditorSceneManager.SaveScene(gameObject.scene);
	}

#endif
}
