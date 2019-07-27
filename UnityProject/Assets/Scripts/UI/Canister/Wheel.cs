
using System;
using Objects;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Main component for the release pressure adjustment wheel
/// </summary>
//TODO: Do I need selectable or is implementing the interfaces sufficient?
public class Wheel : Selectable
{
	[Tooltip("Invoked when wheel is adjusted, on release of the wheel")]
	public IntEvent OnAdjustmentComplete = new IntEvent();

	[Tooltip("How many kPa each degree of rotation is equivalent to.")]
	public float KPAPerDegree = 1.5f;

	[Tooltip("Maximum allowed pressure setting")]
	public float MaxKPA = 1000f;

	/// <summary>
	/// Currently selected amount
	/// </summary>
	public int KPA => Mathf.RoundToInt(degrees * KPAPerDegree);

	[Tooltip("Pressure dial this wheel is bound with.")]
	public NumberSpinner ReleasePressureDial;
	//vector pointing from wheel center to the previous position of the mouse
	private Vector2? previousDrag;
	public float RotationSpeed = 0.2f;
	public GameObject[] UprightSprites;
	private WindowDrag windowDrag;
	private Shadow shadow;
	//degrees of rotation
	private float degrees;

	private void Start()
	{
		base.Start();
		windowDrag = GetComponentInParent<WindowDrag>();
		shadow = GetComponent<Shadow>();
		//TODO: Find a way to allow wheel rotation without disabling drag.
		windowDrag.disableDrag = true;
	}

	public void RotateToValue(int kPA)
	{
		SetRotation(kPA / KPAPerDegree);
	}

	private void SetRotation(float newRotation)
	{
		//can't go below minimum
		if (newRotation < 0) return;
		if (newRotation * KPAPerDegree > MaxKPA) return;

		var newQuaternion = Quaternion.Euler(0, 0, newRotation);
		transform.rotation = newQuaternion;
		foreach (var upright in UprightSprites)
		{
			upright.transform.rotation = Quaternion.identity;
		}

		shadow.effectDistance = Quaternion.Euler(0, 0, -newRotation + 215) * Vector2.up * 10f;

		degrees = newRotation;
		//TODO: Client predict - update display while rotating regardless of server value
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		base.OnPointerDown(eventData);
		previousDrag = ((eventData.pressPosition - (Vector2)((RectTransform) transform).position) / UIManager.Instance.transform.localScale.x).normalized;
	}

	private void Update()
	{
		if (previousDrag != null)
		{
			var newDrag = (((Vector2)CommonInput.mousePosition - (Vector2)((RectTransform) transform).position) / UIManager.Instance.transform.localScale.x).normalized;
			//how far did we rotate from the previous position?
			var degreeDisplacement = Vector2.SignedAngle((Vector2) previousDrag, newDrag);

			//rotate
			SetRotation(degrees + degreeDisplacement);

			previousDrag = newDrag;

			if (CommonInput.GetMouseButtonUp(0))
			{
				previousDrag = null;
				OnAdjustmentComplete.Invoke(KPA);
			}
		}
	}
}
