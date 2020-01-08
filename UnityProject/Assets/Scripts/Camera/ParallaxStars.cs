using UnityEngine;

public class ParallaxStars : MonoBehaviour
{
	/// <summary>
	/// The space background objects for this parallax area.
	/// Specify a target position for the object to be visible (a world space co-ord)
	/// and if it is within a certain distance it will turn on, otherwise it will be off
	/// </summary>
	public SpaceBgObjects[] spaceBgObjects;

	public void CheckSpaceObjects()
	{
		for (int i = 0; i < spaceBgObjects.Length; i++)
		{
			var dist = Vector3.Distance(spaceBgObjects[i].gameObject.transform.position, spaceBgObjects[i].targetPos);
			if (dist < 50f && !spaceBgObjects[i].gameObject.activeInHierarchy)
			{
				spaceBgObjects[i].gameObject.SetActive(true);
			}
			else if (dist > 50f && spaceBgObjects[i].gameObject.activeInHierarchy)
			{
				spaceBgObjects[i].gameObject.SetActive(false);
			}
		}
	}
}

[System.Serializable]
public class SpaceBgObjects
{
	public Vector3 targetPos;
	public GameObject gameObject;
}