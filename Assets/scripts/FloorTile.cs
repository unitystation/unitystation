using UnityEngine;
using System.Collections;
using SS.TileTypes;

public class FloorTile : MonoBehaviour {


	private SpriteRenderer tileRenderer;
	private Sprite[] spriteSheet;
	public Construction_Floors floorType;
	public GameObject spriteObj;
	public bool[] passable;

	// Use this for initialization
	void Start () {

		ResetTile ();

	}

	void ResetTile(){

		passable = new bool[4]{ true, true, true, true };


	}

	/// <summary>
	/// Set tile characteristics 
	/// </summary>
	public void SetTile(Construction_Floors typeOfFloor, Vector2 position){
		spriteSheet = Resources.LoadAll<Sprite> ("turf/floors");
		if (spriteSheet == null) {

			Debug.LogError ("DID NOT LOAD SPRITESHEET");
		}
		tileRenderer = spriteObj.GetComponent<SpriteRenderer> ();
		floorType = typeOfFloor;
		tileRenderer.sprite = spriteSheet [(int)floorType];
		transform.position = position;

	}

	// Update is called once per frame
	void Update () {
	
	}

	//For returning back to the pool
	 void ReturnToPool(){
		//Reset to default state and put it back in the pool
		ResetTile ();
		PoolManager.PoolDestroy (this.gameObject);

	}

	//FOR TESTING DEV (remove when done)
	public void WaitToReturnToPool(float time){

		Invoke ("ReturnToPool", time);

	}

}
