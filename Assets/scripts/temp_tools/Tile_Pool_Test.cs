using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MovementEffects;
using SS.TileTypes;
using SS.UI;

public class Tile_Pool_Test : MonoBehaviour {

	public Toggle theToggle;
	public Dropdown tileTypeDropDown;
	public Camera curCam;

	// Use this for initialization
	void Start () {
		tileTypeDropDown.options.Clear ();
		Timing.RunCoroutine (SetDropDown ());
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
		Construction_Floors floorType = (Construction_Floors)tileTypeDropDown.value;

		// Create the tiles through the TilePrefab controller that uses the generic pool
		GameObject floorTile = TilePrefabs.control.CreateFloorObj(floorType, position);
		ControlUI.control.click01SFX.Play ();

		//Do stuff with it:
		float randomLive = Random.Range (2f, 4f);
		FloorTile tileScript = floorTile.GetComponent<FloorTile> ();
		tileScript.WaitToReturnToPool (randomLive);
	
	}

	//COROUTINES
	IEnumerator<float> SetDropDown(){

		int typeLength = (int)Construction_Floors.NumberOfTypes;

		for (int i = 0; i < typeLength; i++) {
			//GC_Allocs?!?!?
			Construction_Floors floorName = (Construction_Floors)i;
			string tileName = floorName.ToString ();
			tileTypeDropDown.options.Add (new Dropdown.OptionData () { text = tileName });
		
		}

		yield return 0f;
	}



}
