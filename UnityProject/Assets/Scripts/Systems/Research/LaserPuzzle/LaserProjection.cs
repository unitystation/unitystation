using System.Collections;
using System.Collections.Generic;
using Objects.Engineering;
using UnityEngine;
using Util;
using Objects.Research;

public class LaserProjection : MonoBehaviour
{
	public LayerTypeSelection ProjectionLayerTypeSelection;
	public LayerMask LayerMask;

	public LaserLine LaserLinePrefab;

	public List<LaserLine> LaserLines = new List<LaserLine>();


	public ResearchLaserProjector ResearchLaserProjector;

	private bool Destroyed = false;


	public void Initialise(GameObject Source, Vector2 WorldDirection, ResearchLaserProjector _ResearchLaserProjector )
	{
		ResearchLaserProjector = _ResearchLaserProjector;
		var hits = MatrixManager.RayCast(Source.transform.position, WorldDirection, 15,  ProjectionLayerTypeSelection, LayerMask);


		if (hits.ItHit == false) return;

		if (hits.CollisionHit.GameObject == null) return;
		var Plinth = hits.CollisionHit.GameObject.GetComponent<ItemPlinth>();
		if (Plinth == null) return;


		var line = Instantiate(LaserLinePrefab, this.transform);
		line.SetUpLine(Source, Source.transform.position  ,Plinth.gameObject,Plinth.transform.position, new TechnologyAndBeams(), this );
		LaserLines.Add(line);


		if (Plinth.HasItem == false)
		{
			_ResearchLaserProjector.SynchroniseLaser(LaserLines);
			return;
		}

		var Identify = Plinth.DisplayedItem.GetComponent<PrefabTracker>();

		if (_ResearchLaserProjector.researchServer.Techweb.TestedPrefabs.Contains(Identify.ForeverID))
		{
			_ResearchLaserProjector.SynchroniseLaser(LaserLines);
			return;
		}


		var ItemResearchPotential = Plinth.DisplayedItem.GetComponent<ItemResearchPotential>();



		foreach (var Design in ItemResearchPotential.TechWebDesigns)
		{
			if (Design.Technology == null)
			{
				Design.Technology = ResearchLaserProjector.researchServer.Techweb.AvailableTech.PickRandom();
				Design.Colour = Design.Technology.ColourPublic;
			}
		}

		Plinth.gameObject.GetComponent<Collider2D>().enabled = false;

		foreach (var Design in ItemResearchPotential.TechWebDesigns)
		{

			foreach (var Beam in Design.Beams)
			{

				// Calculate the incoming angle using the source and target positions.
				Vector2 direction = Plinth.transform.position - Source.transform.position;
				float incomingAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

				// Add the bending angle to the incoming angle to get the final angle.
				int finalAngle =  Mathf.RoundToInt(incomingAngle + Beam);

				// If the final angle is greater than or equal to 360 or less than 0, wrap it around.
				if (finalAngle >= 360)
				{
					finalAngle -= 360;
				}
				else if (finalAngle < 0)
				{
					finalAngle += 360;
				}

				//Angle stuff
				//Spawn new stuff and go down line
				var Angle = VectorExtensions.DegreeToVector2(finalAngle);
				TraverseLaser(Angle, Plinth.gameObject, Design, 0);
			}
		}
		Plinth.gameObject.GetComponent<Collider2D>().enabled = true;

		_ResearchLaserProjector.SynchroniseLaser(LaserLines);

	}



	public void TraverseLaser(Vector2 WorldDirection, GameObject Origin, TechnologyAndBeams TechnologyAndBeams, int Bounces = 0, Vector3? OriginPosition = null)
	{
		if (OriginPosition == null)
		{
			OriginPosition = Origin.transform.position;
		}

		if (Bounces > 5)
		{
			return;
		}


		Collider2D Collider = null;
		if (Origin != null)
		{
			Collider =  Origin.GetComponent<Collider2D>();
			if (Collider != null)
			{
				Collider.enabled = false;
			}
		}

		var hit = MatrixManager.RayCast(OriginPosition.Value, WorldDirection, 15,ProjectionLayerTypeSelection, LayerMask);

		if (Collider != null)
		{
			Collider.enabled = true;
		}

		if (hit.ItHit == false)
		{
			var World = OriginPosition.Value + (Vector3) (WorldDirection.normalized * 15);
			var line = Instantiate(LaserLinePrefab, this.transform);
			line.SetUpLine(Origin, OriginPosition ,null,World, TechnologyAndBeams,this );
			LaserLines.Add(line);

			return;
		}
		else
		{
			if (hit.CollisionHit.GameObject != null)
			{
				var line = Instantiate(LaserLinePrefab, this.transform);
				line.SetUpLine(Origin,  OriginPosition,hit.CollisionHit.GameObject, hit.HitWorld, TechnologyAndBeams, this );
				LaserLines.Add(line);

				var Reflector = hit.CollisionHit.GameObject.GetComponent<Reflector>();
				if (Reflector != null)
				{
					Bounces++;

					if (Reflector.ValidState() == false) return;

					var NewDirection = Reflector.GetReflect(WorldDirection);

					if (float.IsNaN(NewDirection)) return;

					TraverseLaser(	VectorExtensions.DegreeToVector2(NewDirection), Reflector.gameObject, TechnologyAndBeams, Bounces, hit.HitWorld);

					return;
				}

				var ResearchCollector = hit.CollisionHit.GameObject.GetComponent<ResearchCollector>();
				if (ResearchCollector != null)
				{
					return;
				}

			}
			else
			{

				var line = Instantiate(LaserLinePrefab, this.transform);
				line.SetUpLine(Origin,  OriginPosition ,null,hit.HitWorld, TechnologyAndBeams, this );
				LaserLines.Add(line);

				return;
			}
		}
	}

	public void ManualGenerateLine(ResearchLaserProjector.DataSynchronised DataSynchronised)
	{
		var line = Instantiate(LaserLinePrefab, this.transform);
		line.ManualSetup(DataSynchronised.Origin.ToVector3(), DataSynchronised.Target.ToVector3(),
			DataSynchronised.Colour.UncompresseToColour());
	}

	public void CleanupAndDestroy(bool Reshoot = false)
	{
		if (Destroyed) return;
		Destroyed = true;

		foreach (var Line in LaserLines)
		{
			Destroy(Line.gameObject);
		}

		if (CustomNetworkManager.Instance._isServer)
		{
			LaserLines.Clear();
			this.ResearchLaserProjector.SynchroniseLaser(LaserLines);
		}

		Destroy(this.gameObject);

		if (Reshoot && ResearchLaserProjector != null)
		{
			ResearchLaserProjector.TriggerLaser();
		}

	}


}
