
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Main component for the release pressure adjustment wheel
/// </summary>
//TODO: Do I need selectable or is implementing the interfaces sufficient?
public class Wheel : Selectable
{
	private Vector2? dragOrigin;
	public float RotationSpeed = 0.2f;
	public GameObject[] UprightSprites;
	private WindowDrag windowDrag;

	private void Start()
	{
		base.Start();
		windowDrag = GetComponentInParent<WindowDrag>();
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		base.OnPointerDown(eventData);
		dragOrigin = (eventData.pressPosition - (Vector2)((RectTransform) transform).position) / UIManager.Instance.transform.localScale.x;
		windowDrag.disableDrag = true;
		Logger.Log("DragOrigin " + dragOrigin);
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		base.OnPointerUp(eventData);
		dragOrigin = null;
		windowDrag.disableDrag = false;
	}

	private void Update()
	{
		if (dragOrigin != null)
		{
			var direction = ((Vector2)CommonInput.mousePosition - (Vector2)((RectTransform) transform).position) / UIManager.Instance.transform.localScale.x;

			//TODO: Lerp for a bit smoother of animation
			transform.rotation = Quaternion.Euler(0, 0,
				(float) Math.Atan2(direction.x, direction.y) * Mathf.Rad2Deg - 90);


			foreach (var upright in UprightSprites)
			{
				upright.transform.rotation = Quaternion.identity;
			}
		}
	}
}
