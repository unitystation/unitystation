using UnityEngine;
using System.Collections.Generic;
using Tiles;

namespace Systems.Research
{
	/// <summary>
	/// Terraforms the surrounding enviroment by replacing tiles with the specified tiles and placing prefabs.
	/// </summary>
	[CreateAssetMenu(fileName = "TerraformAreaEffect", menuName = "ScriptableObjects/Systems/Artifacts/TerraformAreaEffect")]
	public class TerraformAreaEffect : AreaEffectBase
	{

		[SerializeField] private List<GameObject> objectsToSpawn = new List<GameObject>();
		[SerializeField] private List<LayerTile> tilesToSpawn = new List<LayerTile>();

		[SerializeField, Range(0, MAX_CHANCE)] private int objectChance = 10;
		private const int MAX_CHANCE = 100;

		public override void DoEffectAura(GameObject centeredAround)
		{
			MatrixInfo matrixInfo = centeredAround.RegisterTile().Matrix.MatrixInfo;
			Vector3Int center = centeredAround.RegisterTile().WorldPositionServer;

			Vector3Int globalPos = EffectShape.CreateEffectShape(effectShapeType, center, AuraRadius).PickRandom();
			Vector3Int localPos = MatrixManager.WorldToLocalInt(globalPos, matrixInfo.Matrix);

			
			LayerTile tileToPlace = tilesToSpawn.PickRandom();
			LayerType typeToReplace = tileToPlace.LayerType;

			if(typeToReplace == LayerType.Floors) typeToReplace = LayerType.Base; //Floors just need a base tile not a floor

			if (matrixInfo.MetaTileMap.HasTile(localPos, typeToReplace) == false) return;
			
			matrixInfo.MetaTileMap.SetTile(localPos, tileToPlace);
			
			int rand = Random.Range(0, 100);
			if(rand <= objectChance && matrixInfo.MetaTileMap.IsAtmosPassableAt(localPos,true))
			{
				GameObject objToSpawn = objectsToSpawn.PickRandom();
				Spawn.ServerPrefab(objToSpawn, SpawnDestination.At(globalPos));
			}
			
		}
	}
}
