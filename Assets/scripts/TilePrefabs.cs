using UnityEngine;
using System.Collections;
using SS.TileTypes;

//[RequireComponent(typeof (PoolManager))]
public class TilePrefabs : MonoBehaviour {

	public static TilePrefabs control;

	/* 
	 * Prototype Notes:
	 * 
	 * Use this static class to Instantiate a tile
	 * 
	 * The constructor works by passing a prefab object to
	 * the pool manager and the pool manager will handle
	 * gameobject instantiation 
	 * 
	 * Remember when building prefabs to return the object back to
	 * the pool instead of destroying it
	 * 
	 * USE: PoolManager.PoolDestroy (this.gameObject);
	 * Also reset your objects back to a default state before 
	 * returning it to the Pool
	 * 
	 * Once the object is active, then characteristics are set
	 * on that object. So aim for generic prefabs
	 * 
	 * 
	 */

//	private PoolManager poolManager;


	//Prefabs:

	private GameObject floorPrefab;


	void Awake(){

		if (control == null) {
		
			control = this;
		
		} else {
		
			Destroy (this);
		
		}

	}

	// Use this for initialization
	void Start () {
//		poolManager = GetComponent<PoolManager> ();
		floorPrefab = Resources.Load ("tiles/construction/FloorTile")as GameObject;

	}

	// Constructors

	/// <summary>
	/// Create a Construction_Floor tile
	/// Return the GameObject
	/// </summary>
	public GameObject CreateFloorObj(Construction_Floors floorType, Vector2 position){
		
		GameObject floorTile = PoolManager.PoolInstantiate (floorPrefab, position, Quaternion.identity);
		FloorTile tileScript = floorTile.GetComponent<FloorTile> ();
		tileScript.SetTile (floorType, position);

		return floorTile;
	}

	/// <summary>
	/// Create a Construction_Floor tile
	/// Build and forget
	/// </summary>
	public void CreateFloor(Construction_Floors floorType, Vector2 position){

		GameObject floorTile = PoolManager.PoolInstantiate (floorPrefab, position, Quaternion.identity);
		FloorTile tileScript = floorTile.GetComponent<FloorTile> ();
		tileScript.SetTile (floorType, position);

	}
	

}

//TYPES
namespace SS.TileTypes{

	// Making the names the same as the name of the individual sprite 
	// to make them easier to find and reference
	// the enum as cast as an int in the sprite selector
	public enum Construction_Floors {

		floors_0,
		floors_1 //etc etc etc

	}


}

