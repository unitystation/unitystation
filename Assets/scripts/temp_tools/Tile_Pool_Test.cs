using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using SS.TileTypes;
using SS.UI;

public class Tile_Pool_Test : MonoBehaviour {

	public Toggle theToggle;
	public Camera curCam;

	// Use this for initialization
	void Start () {

	}

	void Update(){
		if (theToggle.isOn) {
		if (Input.GetButtonDown ("Fire1")) {
				BuildATile(curCam.ScreenToWorldPoint(Input.mousePosition));
		}
		}
	}


	/*
	 * Example of building a tile with the Tile Pool
	 */

	void BuildATile(Vector2 position){

		//Access the TilePrefabs static class and return a gObj from CreateFloor constructor
		GameObject floorTile = TilePrefabs.control.CreateFloorObj(Construction_Floors.floors_0,position);
		ControlUI.control.click01SFX.Play ();

		//Do stuff with it:
		float randomLive = Random.Range (2f, 4f);
		FloorTile tileScript = floorTile.GetComponent<FloorTile> ();
		tileScript.WaitToReturnToPool (randomLive);
	
	}



}
