using System;
using Light2D;
using TMPro;
using UI.Core.Radial;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Core.RightClick
{
	public class RadialBranch : MonoBehaviour
	{
		private const float RightAngle = Mathf.Deg2Rad * 90;

		private static readonly Quaternion ForwardRotation = Quaternion.Euler(0, 0, 90);

		private static readonly float SineRightAngle = Mathf.Sin(RightAngle);

		[Tooltip("The size of the lines from the origin point to the radial.")]
		[SerializeField]
		private Vector2 lineSize = Vector2.zero;

		[Tooltip("The angle of the branch towards the radial.")]
		[Range(90, 180)]
		[SerializeField]
		private float angle = default;

		[SerializeField]
		private RectTransform origin = default;

		private Canvas canvas;

		private Canvas managerCanvas;

		private Camera mainCamera;

		private RectTransform Origin => origin;

		private Canvas Canvas => this.GetComponentByRef(ref canvas);

		private Canvas ManagerCanvas
		{
			get
			{
				if (managerCanvas == null)
				{
					managerCanvas = UIManager.Instance.GetComponent<Canvas>();
				}

				return managerCanvas;
			}
		}

		private Camera Camera
		{
			get
			{
				if (mainCamera == null)
				{
					mainCamera = Camera.main;
				}

				return mainCamera;
			}
		}

		private IRadialPosition RadialPosition { get; set; }

		private RectTransform Target { get; set; }

		private RectTransform LineFromOrigin { get; set; }

		private RectTransform LineToRadial { get; set; }

		private Vector3 CurrentQuadrant { get; set; }

		/// <summary>
		/// Gets the angle of the branch leading to the target radial.
		/// </summary>
		/// <returns></returns>
		public float GetBranchToTargetAngle()
		{
			var relativePosition = Target.anchoredPosition - LineToRadial.anchoredPosition;
			return Mathf.Rad2Deg * Mathf.Atan2(relativePosition.y, relativePosition.x);
		}

		/// <summary>
		/// Updates the direction of the branch based on the quadrant the origin is currently located in.
		/// </summary>
		private void UpdateDirection()
		{
			if (RadialPosition.IsWorldPosition)
			{
				RadialPosition.BoundsOffset = Origin.rect.size / 2;
				Origin.position = RadialPosition.GetPositionIn(Camera, Canvas);
			}

			var relativePos = Origin.anchoredPosition - Target.anchoredPosition;
			var dir = new Vector3(Mathf.Sign(relativePos.x), Mathf.Sign(relativePos.y), 1f);
			if (dir.Equals(CurrentQuadrant))
			{
				return;
			}
			CurrentQuadrant = dir;
			SetOriginScale();
		}

		private static void SetLineSize(RectTransform line, float size)
		{
			line.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
		}

		private void SetLinesActive(bool active)
		{
			if (LineFromOrigin.gameObject.activeSelf == active) return;

			LineFromOrigin.SetActive(active);
			LineToRadial.SetActive(active);
		}

		public void UpdateLines(IRadial outerRadial, int radius)
		{
			if (RadialPosition == null) return;
			if (RadialPosition.IsWorldPosition == false)
			{
				return;
			}

			UpdateDirection();

			var originPos = Origin.anchoredPosition;
			var targetPos = Target.anchoredPosition;
			var distanceToRadial = lineSize.x + radius;
			var relativePos = originPos - targetPos;

			if (distanceToRadial * distanceToRadial > relativePos.sqrMagnitude)
			{
				SetLinesActive(false);
				return;
			}

			SetLinesActive(true);

			var absDeltaY = Mathf.Abs(relativePos.y);
			float length = 0;

			if (absDeltaY < distanceToRadial)
			{
				var lineAngle = RightAngle - Mathf.Asin(absDeltaY * SineRightAngle / distanceToRadial);
				length = distanceToRadial * Mathf.Sin(lineAngle) / SineRightAngle;
			}

			var xPos = targetPos.x + (length * CurrentQuadrant.x);
			var yPos = originPos.y + (LineToRadial.rect.height / 2) * CurrentQuadrant.y;

			RepositionLineToRadial(new Vector2(xPos, yPos));
			SetLineSize(LineFromOrigin, Math.Abs(originPos.x - (Origin.rect.width / 2 * CurrentQuadrant.x) - LineToRadial.anchoredPosition.x));

			var lineToRadialLength = length <= 0 ? absDeltaY : distanceToRadial;
			lineToRadialLength -= radius + OuterRadialLength(outerRadial, radius);

			SetLineSize(LineToRadial, lineToRadialLength);
		}

		private float OuterRadialLength(IRadial outerRadial, float radius)
		{
			if (outerRadial.IsActive == false)
			{
				return 0;
			}

			var annulusSize = outerRadial.OuterRadius - outerRadial.InnerRadius;
			var linePosition = Vector3.MoveTowards(Target.position, LineToRadial.position, (radius + annulusSize / 2f) * Canvas.scaleFactor);
			return outerRadial.IsPositionWithinRadial(linePosition, false) ? annulusSize : 0;
		}

		public void SetupAndEnable(RectTransform target, int radius, float scale, IRadialPosition radialPosition)
		{
			Target = target;
			RadialPosition = radialPosition;
			Origin.position = radialPosition.GetPositionIn(Camera, Canvas);
			var originPos = Origin.anchoredPosition;
			CurrentQuadrant = new Vector3(Mathf.Sign(originPos.x), Mathf.Sign(originPos.y), 1f);
			if (LineFromOrigin == null)
			{
				BuildBranch();
			}
			else
			{
				SetLineSize(LineFromOrigin, lineSize.x);
				SetLineSize(LineToRadial, lineSize.x);
			}

			// Flip the branch as needed based on the clicked quadrant.
			SetOriginScale();
			CalculateTargetPosition(radius, scale);

			var xPos = originPos.x - (lineSize.x + Origin.rect.width / 2) * CurrentQuadrant.x;
			var yPos = originPos.y + (LineToRadial.rect.height / 2) * CurrentQuadrant.y;

			RepositionLineToRadial(new Vector2(xPos, yPos));
			this.SetActive(true);
		}

		private void OnEnable()
		{
			// Canvas Screen Space - Overlay sort order is based on object hierarchy. It can be changed with an overridden
			// canvas sorting order but it needs to be set at some point after an object is enabled, each time, to work properly.
			var sortingOrder = ManagerCanvas.sortingOrder;
			Canvas.sortingOrder = RadialPosition.IsWorldPosition ? sortingOrder - 1 : sortingOrder + 1;
		}

		private void CalculateTargetPosition(float radius, float scale)
		{
			var originWidth = Origin.rect.width / 2;
			var length = (lineSize.x + radius) * Mathf.Sin(Mathf.Deg2Rad * angle) / SineRightAngle * scale;
			var localPosition = Vector2.Scale(CurrentQuadrant, new Vector2(originWidth + lineSize.x + length, length));
			var targetPosition = Origin.anchoredPosition - localPosition;
			Target.anchoredPosition = targetPosition;
		}

		private void RepositionLineToRadial(Vector2 anchoredPosition)
		{
			LineToRadial.pivot = new Vector2(0, CurrentQuadrant.x * CurrentQuadrant.y > 0 ? 0 : 1);
			LineToRadial.anchoredPosition = anchoredPosition;
			var toTarget = Target.position - LineToRadial.position;
			var overlayUpward= ForwardRotation * toTarget;
			LineToRadial.rotation = Quaternion.LookRotation(Vector3.forward, overlayUpward);
		}

		private void BuildBranch()
		{
			var fromOriginObj = new GameObject("BranchLine", typeof(Image));
			var fromOriginImage = fromOriginObj.GetComponent<Image>();
			fromOriginImage.color = Origin.GetComponent<Image>().color;

			// Build the line that comes from the origin point.
			LineFromOrigin = fromOriginObj.GetComponent<RectTransform>();
			LineFromOrigin.name = nameof(LineFromOrigin);
			LineFromOrigin.SetParent(Origin);
			LineFromOrigin.localScale = Vector3.one;
			LineFromOrigin.sizeDelta = lineSize;
			LineFromOrigin.pivot = new Vector2(1f, 0.5f);
			var originWidth = Origin.rect.width / 2;
			LineFromOrigin.anchoredPosition = new Vector2(-originWidth, 0);

			// Copy the previous line and use it as the angled branch.
			LineToRadial = Instantiate(LineFromOrigin, transform);
			LineToRadial.name = nameof(LineToRadial);
		}

		private void SetOriginScale()
		{
			var scale = Origin.localScale;
			Origin.localScale =
				new Vector3(Mathf.Abs(scale.x) * CurrentQuadrant.x, Mathf.Abs(scale.y) * CurrentQuadrant.y, 1f);
		}
	}
}
