using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserLine : MonoBehaviour
{

	//TODO Check objects for updates, move, Reflect angle change

	//TODO https://www.youtube.com/watch?v=DwGcKFMxrmI

	public void SetUpLine(GameObject Origin, GameObject Target , Vector3 WorldTarget, TechnologyAndBeams TechnologyAndBeams)
	{
		if (Target != null)
		{
			WorldTarget = Target.transform.position;
		}

		PositionLaserBody(Origin, WorldTarget);


	}

	public void PositionLaserBody(GameObject Origin, Vector3 WorldTarget )
	{
		Transform wireBodyRectTransform = this.GetComponent<Transform>();

		Vector2 dif = (WorldTarget - Origin.transform.position);

		Vector2 norm = dif.normalized;
		float dist = dif.magnitude;
		float angle = -Vector2.SignedAngle(norm, Vector2.up);

		Vector2 wireOrigin = dist * 0.5f * norm + (Vector2) Origin.transform.position;


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

}
