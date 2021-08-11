using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace UI.Core
{
	/// <summary>
	/// Main component for the release pressure adjustment wheel
	/// </summary>
	public class Wheel : Selectable
	{
		[Tooltip("Invoked when wheel is adjusted, on release of the wheel")]
		public FloatEvent OnAdjustmentComplete = new FloatEvent();

		[Tooltip("How many kPa each degree of rotation is equivalent to.")]
		public float KPAPerDegree = 3f;

		public float MaxValue = 1000;
		/// <summary>
		/// Currently selected amount
		/// </summary>
		public float KPA => degrees * KPAPerDegree;

		[Tooltip("Pressure dial this wheel is bound with.")]
		public NumberSpinner ReleasePressureDial;
		// vector pointing from wheel center to the previous position of the mouse
		private Vector2? previousDrag;
		public float RotationSpeed = 0.2f;
		public GameObject[] UprightSprites;
		private WindowDrag windowDrag;
		private Shadow shadow;
		// degrees of rotation
		private float degrees;

		protected override void Awake()
		{
			base.Start();
			windowDrag = GetComponentInParent<WindowDrag>();
			shadow = GetComponent<Shadow>();
		}

		public void RotateToValue(float kPA)
		{
			SetRotation(kPA / KPAPerDegree);
		}

		private void SetRotation(float newRotation)
		{
			newRotation = Mathf.Clamp(newRotation, 0, MaxValue / KPAPerDegree);

			var newQuaternion = Quaternion.Euler(0, 0, newRotation);
			transform.rotation = newQuaternion;
			foreach (var upright in UprightSprites)
			{
				upright.transform.rotation = Quaternion.identity;
			}

			shadow.effectDistance = Quaternion.Euler(0, 0, -newRotation + 215) * Vector2.up * 10f;

			degrees = newRotation;
			ReleasePressureDial.DisplaySpinTo(Mathf.RoundToInt(KPA));
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			base.OnPointerDown(eventData);
			previousDrag = ((eventData.pressPosition - (Vector2)((RectTransform)transform).position) / UIManager.Instance.transform.localScale.x).normalized;
			// client prediction on the dial
			ReleasePressureDial.IgnoreServerUpdates = true;
			// disable window dragging until done with rotation
			windowDrag.disableDrag = true;
		}

		private void Update()
		{
			if (previousDrag != null)
			{
				var newDrag = (((Vector2)CommonInput.mousePosition - (Vector2)((RectTransform)transform).position) / UIManager.Instance.transform.localScale.x).normalized;
				// how far did we rotate from the previous position?
				var degreeDisplacement = Vector2.SignedAngle((Vector2)previousDrag, newDrag);

				// rotate
				SetRotation(degrees + degreeDisplacement);

				previousDrag = newDrag;

				if (CommonInput.GetMouseButtonUp(0))
				{
					previousDrag = null;
					ReleasePressureDial.IgnoreServerUpdates = false;
					OnAdjustmentComplete.Invoke(KPA);
					windowDrag.disableDrag = false;
				}
			}
		}
	}
}
