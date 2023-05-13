using System.Collections;
using System.Collections.Generic;
using Objects.Engineering;
using UnityEngine;

public class LaserProjection : MonoBehaviour
{
	public LayerTypeSelection ProjectionLayerTypeSelection;
	public LayerMask LayerMask;

	public LaserLine LaserLinePrefab;

	public List<LaserLine> LaserLines = new List<LaserLine>();


	public void Initialise(GameObject Source, ItemPlinth Target, ResearchLaserProjector ResearchLaserProjector )
	{

		var hits = MatrixManager.Linecast(Source.transform.position, ProjectionLayerTypeSelection, LayerMask,
			Target.transform.position);

		if (hits.ItHit)
		{
			//TODO Destroy
			//return;
		}

		var line = Instantiate(LaserLinePrefab, this.transform);
		line.SetUpLine(Source, Target.gameObject,Target.transform.position, new TechnologyAndBeams() );
		LaserLines.Add(line);


		if (Target.HasItem == false)
		{
			//TODO Destroy
			//return;
		}


		var ItemResearchPotential = Target.DisplayedItem.GetComponent<ItemResearchPotential>();

		//TODO Initialise technologies on ItemResearchPotential

		foreach (var Design in ItemResearchPotential.TechWebDesigns)
		{
			foreach (var Beam in Design.Beams)
			{

				// Calculate the incoming angle using the source and target positions.
				Vector2 direction = Target.transform.position - Source.transform.position;
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

				TraverseLaser(VectorExtensions.DegreeToVector2(finalAngle), Target.gameObject, Design);
			}
		}

	}

	public void TraverseLaser(Vector2 WorldDirection, GameObject Origin, TechnologyAndBeams TechnologyAndBeams, int Bounces = 0)
	{
		if (Bounces > 5)
		{
			return;
		}

		var hit = MatrixManager.RayCast(Origin.transform.position + WorldDirection.To3(), WorldDirection, 15,ProjectionLayerTypeSelection, LayerMask);

		if (hit.ItHit == false)
		{
			var World = Origin.transform.position + (Vector3) (WorldDirection.normalized * 15);
			var line = Instantiate(LaserLinePrefab, this.transform);
			line.SetUpLine(Origin, null,World, TechnologyAndBeams );
			LaserLines.Add(line);

			return;
		}
		else
		{
			if (hit.CollisionHit.GameObject != null)
			{
				var line = Instantiate(LaserLinePrefab, this.transform);
				line.SetUpLine(Origin,  hit.CollisionHit.GameObject, hit.CollisionHit.GameObject.transform.position, TechnologyAndBeams );
				LaserLines.Add(line);

				var Reflector = hit.CollisionHit.GameObject.GetComponent<Reflector>();
				if (Reflector != null)
				{
					Bounces++;

					if (Reflector.ValidState() == false) return;

					var NewDirection = Reflector.GetReflect(WorldDirection);

					if (float.IsNaN(NewDirection)) return;

					TraverseLaser(	VectorExtensions.DegreeToVector2(NewDirection), Reflector.gameObject, TechnologyAndBeams, Bounces);

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
				line.SetUpLine(Origin, null,hit.HitWorld, TechnologyAndBeams );
				LaserLines.Add(line);

				return;
			}
		}



	}


}
