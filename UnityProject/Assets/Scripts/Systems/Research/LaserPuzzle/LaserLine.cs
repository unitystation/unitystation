using System;
using System.Collections;
using System.Collections.Generic;
using Objects.Engineering;
using UnityEngine;

public class LaserLine : MonoBehaviour
{

	private GameObject TOrigin;
	private Reflector TOriginReflector;
	private Integrity TOriginIntegrity;
	private UniversalObjectPhysics TOriginUniversalObjectPhysics;

	private ItemPlinth TOriginPlinth;



	private GameObject TTarget;
	private Reflector TTargetReflector;
	private Integrity TTargetIntegrity;
	private UniversalObjectPhysics TTargetUniversalObjectPhysics;

	private ItemPlinth TTargetPlinth;
	public LaserProjection RelatedLaserProjection;


	public SpriteRenderer Sprite;

	public Vector3 VOrigin;
	public Vector3 VTarget;


	private void HookInto()
	{
		if (TOrigin != null)
		{
			TOriginReflector  = TOrigin.GetComponent<Reflector>();
			if (TOriginReflector != null)
			{
				TOriginReflector.AngleChange += DestroyLine;
			}

			TOriginPlinth  = TOrigin.GetComponent<ItemPlinth>();
			if (TOriginPlinth != null)
			{
				TOriginPlinth.OnItemChange += DestroyLine;
			}



			TOriginIntegrity = TOrigin.GetComponent<Integrity>();
			TOriginIntegrity.BeingDestroyed += DestroyLine;

			TOriginUniversalObjectPhysics = TOrigin.GetComponent<UniversalObjectPhysics>();
			TOriginUniversalObjectPhysics.OnLocalTileReached.AddListener(DestroyLine2);

		}

		if (TTarget != null)
		{
			TTargetReflector  = TTarget.GetComponent<Reflector>();
			if (TTargetReflector != null)
			{
				TTargetReflector.AngleChange += DestroyLine;
			}

			TTargetIntegrity = TTarget.GetComponent<Integrity>();
			if (TTargetIntegrity != null)
			{
				TTargetIntegrity.BeingDestroyed += DestroyLine;
			}

			TTargetPlinth  = TTarget.GetComponent<ItemPlinth>();
			if (TTargetPlinth != null)
			{
				TTargetPlinth.OnItemChange += DestroyLine;
			}



			TTargetUniversalObjectPhysics = TTarget.GetComponent<UniversalObjectPhysics>();
			TTargetUniversalObjectPhysics.OnLocalTileReached.AddListener(DestroyLine2);

		}
	}


	public void SetUpLine(GameObject Origin,  Vector3? OriginTarget, GameObject Target , Vector3? WorldTarget, TechnologyAndBeams TechnologyAndBeams, LaserProjection _RelatedLaserProjection)
	{
		TTarget = Target;
		TOrigin = Origin;


		RelatedLaserProjection = _RelatedLaserProjection;
		var Colour = TechnologyAndBeams.Colour;
		Colour.a = 0.65f;
		Sprite.color = Colour;


		HookInto();
		if (WorldTarget == null)
		{
			WorldTarget = Target.transform.position;
		}

		if (OriginTarget == null)
		{
			OriginTarget = Origin.transform.position;
		}

		PositionLaserBody(OriginTarget.Value, WorldTarget.Value);
	}

	public void ManualSetup(Vector3 OriginTarget, Vector3 WorldTarget, Color Colour)
	{
		Colour.a = 0.65f;
		Sprite.color = Colour;
		PositionLaserBody(OriginTarget, WorldTarget);
	}

	public void PositionLaserBody(Vector3 OriginTarget, Vector3 WorldTarget )
	{
		VOrigin = OriginTarget;
		VTarget = WorldTarget;
		Transform wireBodyRectTransform = this.GetComponent<Transform>();

		Vector2 dif = (WorldTarget - OriginTarget);

		Vector2 norm = dif.normalized;
		float dist = dif.magnitude;
		float angle = -Vector2.SignedAngle(norm, Vector2.up);

		Vector2 wireOrigin = dist * 0.5f * norm + (Vector2) OriginTarget;


		Vector2 oldSize = gameObject.transform.localScale;

		//* (2 - UIManager.Instance.transform.localScale.x)
		gameObject.transform.localScale =
			new Vector2(oldSize.x,
				dist); //Need to add this scaling here, because for some reason, the entire UI is scaled by 0.67? Iunno why.


		wireBodyRectTransform.position = wireOrigin;

		Vector3 rotation = wireBodyRectTransform.transform.eulerAngles;
		rotation.z = angle;
		wireBodyRectTransform.transform.eulerAngles = rotation;
	}

	public void DestroyLine2(Vector3Int move, Vector3Int move2)
	{
		DestroyLine();
	}

	public void DestroyLine()
	{
		RelatedLaserProjection.CleanupAndDestroy(true);
	}

	public void OnDestroy()
	{
		if (TOrigin != null)
		{
			if (TOriginReflector != null)
			{
				TOriginReflector.AngleChange -= DestroyLine;
			}

			if (TOriginPlinth != null)
			{
				TOriginPlinth.OnItemChange -= DestroyLine;
			}


			TOriginIntegrity.BeingDestroyed -= DestroyLine;
			TOriginUniversalObjectPhysics.OnLocalTileReached.RemoveListener(DestroyLine2);
		}

		if (TTarget != null)
		{
			if (TTargetReflector != null)
			{
				TTargetReflector.AngleChange -= DestroyLine;
			}

			if (TTargetIntegrity != null)
			{
				TTargetIntegrity.BeingDestroyed -= DestroyLine;
			}

			if (TTargetPlinth != null)
			{
				TTargetPlinth.OnItemChange -= DestroyLine;
			}

			TTargetUniversalObjectPhysics.OnLocalTileReached.RemoveListener(DestroyLine2);
		}
	}
}
