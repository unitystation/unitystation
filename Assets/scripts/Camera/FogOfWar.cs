using Matrix;
using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    public GameObject ShroudPrefab;
    public int MonitorRadius = 12;
    public int FieldOfVision = 90;
    public int InnatePreyVision = 6;
    private Dictionary<Vector2, GameObject> shroudTiles = new Dictionary<Vector2, GameObject>();
    private Vector3 lastPosition;
    private Vector2 lastDirection;
    public int WallLayer = 9;

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
			GizmoRays.Clear ();

            transform.hasChanged = false;

            if (transform.position == lastPosition && GetSightSourceDirection() == lastDirection)
                return;

            UpdateSightSourceFov(GetNearbyShroudTiles());
            lastPosition = transform.position;
            lastDirection = GetSightSourceDirection();
        }
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
		foreach (KeyValuePair<Vector2,GameObject> s in shroudTiles) {
			s.Value.SetActive(false);
		}
		this.enabled = false;
	}

    // Returns all shroud nodes in field of vision
    public List<Vector2> GetInFieldOfVision(List<Vector2> inputShrouds)
    {
        List<Vector2> inFieldOFVision = new List<Vector2>();
        foreach (Vector2 inputShroud in inputShrouds)
        {


            // Light close behind and around
            if (Vector2.Distance(GetSightSource().transform.position, inputShroud) < InnatePreyVision)
            {
                inFieldOFVision.Add(inputShroud);
                continue;
            }

            //In front cone
            if (Vector3.Angle(shroudTiles[inputShroud].transform.position - GetSightSource().transform.position, GetSightSourceDirection()) < FieldOfVision)
            {
                inFieldOFVision.Add(inputShroud);
                continue;
            }
        }

        return inFieldOFVision;
    }

    public void UpdateSightSourceFov(List<Vector2> nearbyShrouds)
    {
        // Mark all tiles as shrouded that are nearby
        foreach (Vector2 nearbyShroud in nearbyShrouds)
        {
            SetShroudStatus(nearbyShroud, true);
        }

        // Loop through all tiles that are nearby and are in field of vision
        foreach (Vector2 inFieldOfVisionShroud in GetInFieldOfVision(nearbyShrouds))
        {
            int WallLayerMask = 1 << WallLayer;
            int LayerMask = WallLayerMask;

			//get the points on the face we should raytrace
			var tileFacePoints = GetFacePointsForTile(inFieldOfVisionShroud);

			//check if any points made contact
			RaycastHit2D hit = new RaycastHit2D();

			bool isClear = false;

			//for each shroud tile
			foreach (Vector2 targetTile in tileFacePoints) {
				RaycastHit2D hitTest = Physics2D.Linecast(GetSightSource().transform.position, targetTile, LayerMask);

				//hit an object that we want to keep
				if (hitTest.transform != null && new Vector2(hitTest.transform.position.x, hitTest.transform.position.y) == inFieldOfVisionShroud)
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
				SetShroudStatus (inFieldOfVisionShroud, false);
				continue;
			} else {
				// Enable shroud, a wall was in the way
				SetShroudStatus(inFieldOfVisionShroud, true);
				continue;
			}
        }
    }

	public List<Vector2> GetFacePointsForTile(Vector2 referenceVector) {
		//get the points on the face we should raytrace
		var topFace = new Vector2(referenceVector.x, referenceVector.y + 0.50f);
		var bottomFace = new Vector2(referenceVector.x, referenceVector.y - 0.50f);
		var leftFace = new Vector2(referenceVector.x - 0.50f, referenceVector.y);
		var rightFace = new Vector2(referenceVector.x + 0.50f, referenceVector.y);

		var topLeftFace = new Vector2(referenceVector.x - 0.60f, referenceVector.y + 0.60f);
		var topRightFace = new Vector2(referenceVector.x + 0.60f, referenceVector.y + 0.60f);
		var bottomLeftFace = new Vector2(referenceVector.x - 0.60f, referenceVector.y - 0.60f);
		var bottomRightFace = new Vector2(referenceVector.x + 0.60f, referenceVector.y - 0.60f);

		//collect points
		List<Vector2> checkPoints = new List<Vector2>();
		checkPoints.Add (topFace);
		checkPoints.Add (bottomFace);
		checkPoints.Add (leftFace);
		checkPoints.Add (rightFace);

		checkPoints.Add (topLeftFace);
		checkPoints.Add (topRightFace);
		checkPoints.Add (bottomLeftFace);
		checkPoints.Add (bottomRightFace);

		return checkPoints;
	}

    // Changes a shroud to on or off
    public void SetShroudStatus(Vector2 vector2, bool enabled)
    {
        GameObject shroud = this.shroudTiles[vector2];
        foreach (SpriteRenderer renderer in shroud.GetComponentsInChildren<SpriteRenderer>())
        {
            renderer.enabled = enabled;
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
		return PlayerManager.LocalPlayer.GetComponent<PlayerSprites>().currentDirection;
	}
}
