using Matrix;
using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lighting;
using Events;
using UnityEngine.Profiling;

public class CameraOcclusion : MonoBehaviour
{
    public GameObject ShroudPrefab;
    public int MonitorRadius = 12;
    public int FieldOfVision = 90;
    public int InnatePreyVision = 6;
	private Dictionary<Vector2, Shroud> shroudTiles = new Dictionary<Vector2, Shroud>();
    private Vector3 lastPosition;
    private Vector2 lastDirection;
    public int WallLayer = 9;
	public bool includeLights = false;

	GameObject ShroudContainer;

	struct Line {
		public Vector2 start;
		public Vector2 end;
		public Color color;
		public Line(Vector2 start, Vector2 end, Color color) {
			this.start = start;
			this.end = end;
			this.color = color;
		}
	}

	List<Line> GizmoRays = new List<Line>();

    // Update is called once per frame
    public void Update()
    {
        // Update when we move the camera and we have a valid SightSource
        if (GetSightSource() == null)
            return;

        if (transform.hasChanged)
        {
			UpdateShroud ();
        }
    }

	public void UpdateShroud() {
		Profiler.BeginSample("GizmoClear");
		GizmoRays.Clear ();
		Profiler.EndSample();
	
		if(includeLights)
		EventManager.UpdateLights();
		
		transform.hasChanged = false;

		//if (transform.position == lastPosition && GetSightSourceDirection() == lastDirection)
		//	return;
		Profiler.BeginSample("UpdateSightSourceFov");
		UpdateSightSourceFov(GetNearbyShroudTiles());
		Profiler.EndSample();
		lastPosition = transform.position;
		lastDirection = GetSightSourceDirection();
	}

	void OnDrawGizmos() {
		if (GizmoRays != null) {
			foreach (Line line in GizmoRays) {
				Gizmos.color = line.color;
				Gizmos.DrawLine(line.start, line.end);
			}
		}
	}

	//FIXME make this secure, set it up for the demo
	public void TurnOffShroud(){
		foreach (KeyValuePair<Vector2,Shroud> s in shroudTiles) {
			s.Value.gameObject.SetActive(false);
		}
		this.enabled = false;
	}

	public List<Shroud> GetShrouds() {
		List<Shroud> shrouds = new List<Shroud>();
		foreach (KeyValuePair<Vector2,Shroud> s in shroudTiles) {
			shrouds.Add(s.Value);
		}
		return shrouds;
	}

	public List<Shroud> GetShroudsInDistanceOfPoint(int distance, Vector2 point) {
		List<Shroud> shrouds = new List<Shroud>();
		foreach (KeyValuePair<Vector2,Shroud> s in shroudTiles) {
			if (Vector2.Distance(s.Key, point) <= distance) {
				shrouds.Add(s.Value);
			}
		}
		return shrouds;
	}

    // Returns all shroud nodes in field of vision
    public List<Vector2> GetInFieldOfVision(List<Vector2> inputShrouds)
    {
        List<Vector2> inFieldOFVision = new List<Vector2>();
		for(int i = 0; i < inputShrouds.Count; i++)
        {


            // Light close behind and around
			if (Vector2.Distance(GetSightSource().transform.position, inputShrouds[i]) < InnatePreyVision)
            {
				inFieldOFVision.Add(inputShrouds[i]);
                continue;
            }

            //In front cone
			if (Vector3.Angle(shroudTiles[inputShrouds[i]].transform.position - GetSightSource().transform.position, GetSightSourceDirection()) < FieldOfVision)
            {
				inFieldOFVision.Add(inputShrouds[i]);
                continue;
            }
        }

        return inFieldOFVision;
    }

    public void UpdateSightSourceFov(List<Vector2> nearbyShrouds)
    {
        // Mark all tiles as shrouded that are nearby
		Profiler.BeginSample("SetShroudStatusForEach");
		for (int i = 0; i < nearbyShrouds.Count; i++)
        {
			SetShroudStatus(nearbyShrouds[i], true);
        }
		Profiler.EndSample();
			
        // Loop through all tiles that are nearby and are in field of vision
		Profiler.BeginSample("FOVShroudForEach");
		List<Vector2> allShrouds = GetInFieldOfVision(nearbyShrouds);
		for(int i = 0; i < allShrouds.Count; i ++)
        {
            int WallLayerMask = 1 << WallLayer;
            int LayerMask = WallLayerMask;

			//get the points on the face we should raytrace
			var tileFacePoints = GetFacePointsForTile(allShrouds[i]);

			bool isClear = false;

			//for each shroud tile
			foreach (Vector2 targetTile in tileFacePoints) {
				RaycastHit2D hitTest = Physics2D.Linecast(GetSightSource().transform.position, targetTile, LayerMask);

				//GizmoRays.Add(new Line(GetSightSource().transform.position, targetTile, new Color (1, 0, 0, 0.5F)));
				//hit an object that we want to keep
				if (hitTest.transform != null && new Vector2(hitTest.transform.position.x, hitTest.transform.position.y) == allShrouds[i])
				{
					isClear = true;
					GizmoRays.Add(new Line(GetSightSource().transform.position, targetTile, new Color (1, 0, 0, 0.5F)));
				}
				//no obstruction for ray against testing tile
				else if (hitTest.transform == null) {
					isClear = true;
					GizmoRays.Add(new Line(GetSightSource().transform.position, targetTile, new Color (0, 0, 1, 0.5F)));
				}
			}

			if (isClear) {
				// Vision of tile not blocked by wall, disable the shroud
				SetShroudStatus(allShrouds[i], false);
				continue;
			} else {
				// Enable shroud, a wall was in the way
				SetShroudStatus(allShrouds[i], true);
				continue;
			}
        }
		Profiler.EndSample();
    }

	public List<Vector2> GetFacePointsForTile(Vector2 referenceVector) {
		Vector2 playerPosition = new Vector2(GetSightSource().transform.position.x, GetSightSource().transform.position.y);

		List<Vector2> checkPoints = new List<Vector2>();
		//Vector2 direction = (referenceVector - playerPosition).normalized;
		//TODO: there is a better way to do this, i cant remember how right now, use this ^^

		//topright
		if (playerPosition.x > referenceVector.x && playerPosition.y > referenceVector.y ) {
			checkPoints.Add (new Vector2 (referenceVector.x + 0.60f, referenceVector.y + 0.60f));
		}
		//topleft
		else if (playerPosition.x < referenceVector.x && playerPosition.y > referenceVector.y ) {
			checkPoints.Add (new Vector2 (referenceVector.x - 0.60f, referenceVector.y + 0.60f));
		}
		//bottomright
		else if (playerPosition.x > referenceVector.x && playerPosition.y < referenceVector.y ) {
			checkPoints.Add ( new Vector2(referenceVector.x + 0.60f, referenceVector.y - 0.60f));
		}
		//bottomleft
		else if (playerPosition.x < referenceVector.x && playerPosition.y < referenceVector.y ) {
			checkPoints.Add (new Vector2 (referenceVector.x - 0.60f, referenceVector.y - 0.60f));
		}
		//top
		else if (playerPosition.x == referenceVector.x && playerPosition.y < referenceVector.y ) {
			checkPoints.Add (new Vector2 (referenceVector.x, referenceVector.y));
		}
		//bottom
		else if (playerPosition.x == referenceVector.x && playerPosition.y > referenceVector.y ) {
			checkPoints.Add (new Vector2 (referenceVector.x, referenceVector.y));
		}
		//left
		else if (playerPosition.x < referenceVector.x && playerPosition.y == referenceVector.y ) {
			checkPoints.Add (new Vector2 (referenceVector.x, referenceVector.y));
		}
		//right
		else if (playerPosition.x > referenceVector.x && playerPosition.y == referenceVector.y ) {
			checkPoints.Add (new Vector2 (referenceVector.x, referenceVector.y));
		}
		//center
		else if (playerPosition.x == referenceVector.x && playerPosition.y == referenceVector.y ) {
			checkPoints.Add (new Vector2 (referenceVector.x, referenceVector.y));
		}

		return checkPoints;
	}

    // Changes a shroud to on or off
    public void SetShroudStatus(Vector2 vector2, bool enabled)
    {
        Shroud shroud = this.shroudTiles[vector2];

		var spriteRenderer = shroud.GetComponent<SpriteRenderer>();
		if (spriteRenderer != null) {
			if (!includeLights) {
				spriteRenderer.enabled = enabled;
				return;
			}

			//if this tile has no shroud, get its lighting information
			if (!enabled) {
				
				//either load the shrouds light information else make it dark
				if (shroud.Lights.Count > 0) {
					//start off in full darkness
					float totalLight = 0.05f;

					//add brightness
					foreach (LightSource checkingLight in shroud.Lights) {
						float distanceToLight = Vector2.Distance (shroud.transform.position, checkingLight.transform.position);
						float max = checkingLight.MaxRange;
						float offset = checkingLight.MaxRange - distanceToLight;
						float percent = offset / max;
						totalLight += percent;
					}

					shroud.CurrentBrightness = totalLight;
					totalLight = 1 - totalLight;

					//keep lighting in normal ranges
					if (totalLight > 0.95F) {
						totalLight = 0.95F;
					}
					else if (totalLight < 0) {
						totalLight = 0.0F;
					}

					spriteRenderer.material.SetColor ("_Color", new Color (0, 0, 0, totalLight)); 
				} else {
					spriteRenderer.material.SetColor("_Color", new Color (0, 0, 0, 0.95F)); 
				}
			} else {
				spriteRenderer.material.SetColor("_Color", new Color (0, 0, 0, 1.0F)); 
			}
		}
    }

    // Adds new shroud to our cache and marks it as enabled
    public GameObject RegisterNewShroud(Vector2 vector2, bool active)
    {
		if (ShroudContainer == null) {
			ShroudContainer = new GameObject("Shroud Container");
		}

        GameObject shroudObject = Instantiate(ShroudPrefab, new Vector3(vector2.x, vector2.y, 0), Quaternion.identity);
		shroudObject.transform.parent = ShroudContainer.transform;
		shroudTiles.Add(vector2, shroudObject.GetComponent<Shroud>());
        SetShroudStatus(vector2, active);
        return shroudObject;
    }

    public List<Vector2> GetNearbyShroudTiles()
    {
        List<Vector2> nearbyShroudTiles = new List<Vector2>();

		if (GetSightSource () == null)
			return nearbyShroudTiles;
		
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
                    if (node.IsSpace())
                        continue;

                if (!shroudTiles.ContainsKey(new Vector2(x, y)))
                    RegisterNewShroud(new Vector2(x, y), false);

                nearbyShroudTiles.Add(new Vector2(x, y));
            }
        }

        return nearbyShroudTiles;
    }

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
		if (PlayerManager.LocalPlayer == null)
			return Vector2.zero;

		var playerSprites = PlayerManager.LocalPlayer.GetComponent<PlayerSprites>();
		if (playerSprites != null) {
			return playerSprites.currentDirection;
		}

		return Vector2.zero;
	}
}
