using UnityEngine;
using UnityEngine.UI;

namespace UI.Core.RightClick
{
	public class RadialBranch : MonoBehaviour
	{
		[Tooltip("The size of the lines from the origin point to the radial.")]
		[SerializeField]
		private Vector2 lineSize = Vector2.zero;

		[Tooltip("The angle of the branch towards the radial.")]
		[Range(90, 180)]
		[SerializeField]
		private float angle = default;

		private Vector3 menuPosition;

		public Vector3 MenuPosition
		{
			get => Origin.localPosition - Vector3.Scale(-CurrentQuadrant, menuPosition);
			private set => menuPosition = value;
		}

		private bool FollowWorldPosition { get; set; }

		private Camera Camera { get; set; }

		private Vector3 WorldPosition { get; set; }

		private RectTransform Origin { get; set; }

		private RectTransform LineFromOrigin { get; set; }

		private RectTransform LineToRadial { get; set; }

		private Vector3 CurrentQuadrant { get; set; }

		public void Awake()
		{
			Origin = GetComponent<RectTransform>();
			Camera = Camera.main;
		}

		public void UpdateDirection()
		{
			if (FollowWorldPosition)
			{
				Origin.position = Camera.WorldToScreenPoint(WorldPosition);
			}
			var localPosition = Origin.localPosition;
			var dir = new Vector3(Mathf.Sign(localPosition.x), Mathf.Sign(localPosition.y), 1f);
			if (dir.Equals(CurrentQuadrant))
			{
				return;
			}
			CurrentQuadrant = dir;
			SetScale();
		}

		public void Setup(Vector3 position, int radius, float scale, bool followWorldPosition)
		{
			FollowWorldPosition = followWorldPosition;
			if (followWorldPosition)
			{
				WorldPosition = Camera.ScreenToWorldPoint(position);
			}
			Origin.position = position;
			if (LineFromOrigin == null)
			{
				BuildBranch(radius, scale);
			}
			else
			{
				UpdateDirection();
			}
		}

		private void BuildBranch(int radius, float scale)
		{
			var fromOriginObj = new GameObject("BranchLine", typeof(Image));
			var fromOriginImage = fromOriginObj.GetComponent<Image>();
			fromOriginImage.color = Origin.GetComponent<Image>().color;

			// Build the line that comes from the origin point.
			LineFromOrigin = fromOriginObj.GetComponent<RectTransform>();
			LineFromOrigin.SetParent(Origin);
			LineFromOrigin.localScale = Vector3.one;
			LineFromOrigin.sizeDelta = lineSize;
			LineFromOrigin.pivot = new Vector2(1f, 0.5f);
			var originWidth = Origin.rect.width / 2;
			LineFromOrigin.localPosition = new Vector3(-originWidth, 0, 0);

			// Copy the line previous line and use it as the angled branch.
			LineToRadial = Instantiate(LineFromOrigin, Origin);
			LineToRadial.pivot = Vector2.zero;
			LineToRadial.localPosition = new Vector3(-(originWidth + lineSize.x), lineSize.y / 2, 0);
			LineToRadial.Rotate(Vector3.back, angle);

			var position = Origin.localPosition;
			CurrentQuadrant = new Vector3(Mathf.Sign(position.x), Mathf.Sign(position.y), 1f);

			// Flip the branch as needed based on the clicked quadrant.
			SetScale();
			var pos = new Vector3(-(lineSize.x * 2 + originWidth + radius * scale), lineSize.y / 2, 0f);
			MenuPosition = pos.RotateAroundZ(LineToRadial.localPosition, 180 - angle);
		}

		private void SetScale()
		{
			var scale = Origin.localScale;
			Origin.localScale =
				new Vector3(Mathf.Abs(scale.x) * CurrentQuadrant.x, Mathf.Abs(scale.y) * CurrentQuadrant.y, 1f);
		}
	}
}
