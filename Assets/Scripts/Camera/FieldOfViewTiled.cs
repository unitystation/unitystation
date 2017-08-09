using Matrix;
using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfViewTiled : MonoBehaviour
{
	public int MonitorRadius = 12;
	public int FieldOfVision = 90;
	public int InnatePreyVision = 6;
	public Dictionary<Vector2, GameObject> shroudTiles = new Dictionary<Vector2, GameObject>();
	private Vector3 lastPosition;
	private Vector2 lastDirection;
	public int WallLayer = 9;

	// This should return the current GameObject which is providing vision
	// into the fog of war - such as a security camera or a player
	public GameObject GetSightSource()
	{
		// TODO Support security cameras etc
		return PlayerManager.LocalPlayer;
	}

	// TODO Support security cameras etc
	public Vector2 GetSightSourceDirection()
	{
		return PlayerManager.LocalPlayerScript.playerSprites.currentDirection;
	}

	// Update is called once per frame
	public void Update()
	{
		// Update when we move the camera and we have a valid SightSource
		if (GetSightSource() == null)
			return;

		if (transform.hasChanged)
		{
			transform.hasChanged = false;

			if (transform.position == lastPosition && GetSightSourceDirection() == lastDirection)
				return;

			UpdateSightSourceFov(GetNearbyShroudTiles());
			lastPosition = transform.position;
			lastDirection = GetSightSourceDirection();
		}
	}
		
	public void UpdateSightSourceFov(List<Vector2> nearbyShrouds)
	{
		Vector2[] nearbyShroudsArray = nearbyShrouds.ToArray();
		List<Vector2> inFieldOFVision = new List<Vector2>();
		// Returns all shroud nodes in field of vision
		for(int i = 0; i < nearbyShroudsArray.Length ;i++){
			SetShroudStatus(nearbyShroudsArray[i], true);

			// Light close behind and around
			if (Vector2.Distance(GetSightSource().transform.position, nearbyShroudsArray[i]) < InnatePreyVision)
			{
				inFieldOFVision.Add(nearbyShroudsArray[i]);
				continue;
			}

			// In front cone
			if (Vector3.Angle(shroudTiles[nearbyShroudsArray[i]].transform.position - GetSightSource().transform.position, GetSightSourceDirection()) < FieldOfVision)
			{
				inFieldOFVision.Add(nearbyShroudsArray[i]);
				continue;
			}
		}
			
		// Loop through all tiles that are nearby and are in field of vision
		Vector2[] shroudNodes = inFieldOFVision.ToArray();
		for(int i = 0; i < shroudNodes.Length; i++){
			// There is a slight issue with linecast where objects directly diagonal to you are not hit by the cast
			// and since we are standing next to the tile we should always be able to view it, lets always deactive the shroud
			if (Vector2.Distance(shroudNodes[i], GetSightSource().transform.position) < 2)
			{
				SetShroudStatus(shroudNodes[i], false);
				continue;
			}
			// Everything else:

			// Perform a linecast to see if a wall is blocking vision of the target tile
			int WallLayerMask = 1 << WallLayer;
			int LayerMask = WallLayerMask;
			RaycastHit2D hit = Physics2D.Linecast(GetSightSource().transform.position, shroudNodes[i], LayerMask);

			// If it hits a wall we should enable the shroud
			if (hit)
			{
				if (new Vector2(hit.transform.position.x, hit.transform.position.y) != shroudNodes[i])
				{
					// Enable shroud, a wall was in the way
					SetShroudStatus(shroudNodes[i], true);
					continue;
				}
				else
				{
					// Disable shroud, the wall was our target tile
					SetShroudStatus(shroudNodes[i], false);
					continue;
				}
			}
			else
			{
				// Vision of tile not blocked by wall, disable the shroud
				SetShroudStatus(shroudNodes[i], false);
				continue;
			}
		}
	}

	// Changes a shroud to on or off
	public void SetShroudStatus(Vector2 vector2, bool enabled)
	{
		shroudTiles[vector2].GetComponent<Renderer>().enabled = enabled;
	}

	// Adds new shroud to our cache and marks it as enabled
	public GameObject RegisterNewShroud(Vector2 vector2, bool active)
	{
		GameObject shroudObject = ItemFactory.Instance.SpawnShroudTile(new Vector3(vector2.x, vector2.y, 0));
		shroudTiles.Add(vector2, shroudObject);
		SetShroudStatus(vector2, active);
		return shroudObject;
	}

	public List<Vector2> GetNearbyShroudTiles()
	{
		List<Vector2> nearbyShroudTiles = new List<Vector2>();

		// Get nearby shroud tiles based on monitor radius
		for (int offsetx = -MonitorRadius; offsetx <= MonitorRadius; offsetx++)
		{
			for (int offsety = -MonitorRadius; offsety <= MonitorRadius; offsety++)
			{
				int x = (int)GetSightSource().transform.position.x + offsetx;
				int y = (int)GetSightSource().transform.position.y + offsety;

				// TODO Registration should probably be moved elsewhere
				Matrix.MatrixNode node = Matrix.Matrix.At(new Vector2(x, y));
				if (!shroudTiles.ContainsKey(new Vector2(x, y)))
					//FIXME Need to create a IsWall() check in matrix!
				if (node.IsSpace() || !node.IsPassable())
					continue;

				if (!shroudTiles.ContainsKey(new Vector2(x, y)))
					RegisterNewShroud(new Vector2(x, y),false);

				nearbyShroudTiles.Add(new Vector2(x, y));
			}
		}

		return nearbyShroudTiles;
	}
}