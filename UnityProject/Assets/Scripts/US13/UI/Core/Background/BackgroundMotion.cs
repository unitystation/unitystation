using UnityEngine;

namespace US13.UI.Core.Background
{
	/// <summary>
	/// Slowly pans and zooms a background image for a subtle Ken Burns drift, choosing a new random direction each pass.
	/// </summary>
	public class BackgroundMotion : MonoBehaviour
	{
		[SerializeField] private RectTransform target = null;
		[SerializeField] private bool panEnabled = true;
		[SerializeField] private bool zoomEnabled = true;
		[SerializeField] private float driftDuration = 18f;
		[SerializeField, Range(0f, 0.2f)] private float overscan = 0.12f; // extra zoom kept in reserve so panning never shows the edge
		[SerializeField, Range(0f, 0.1f)] private float panDistance = 0.04f; // how far it slides, as a fraction of the screen
		[SerializeField, Range(0f, 0.2f)] private float zoomAmount = 0.06f; // how much extra it zooms in over a pass

		private Vector2 fromPosition;
		private Vector2 toPosition;
		private float fromScale;
		private float toScale;
		private float elapsed;

		private void Awake()
		{
			if (target == null) target = transform as RectTransform;
		}

		private void OnEnable()
		{
			if (target == null) return;
			float baseScale = 1f + overscan;
			target.localScale = new Vector3(baseScale, baseScale, 1f);
			StartNewDrift();
		}

		private void Update()
		{
			if (target == null) return;
			elapsed += Time.deltaTime;
			float t = Mathf.SmoothStep(0f, 1f, driftDuration > 0f ? elapsed / driftDuration : 1f);
			if (panEnabled) target.anchoredPosition = Vector2.Lerp(fromPosition, toPosition, t);
			float scale = Mathf.Lerp(fromScale, toScale, t);
			target.localScale = new Vector3(scale, scale, 1f);
			if (elapsed >= driftDuration) StartNewDrift();
		}

		private void StartNewDrift()
		{
			float baseScale = 1f + overscan;
			fromPosition = target.anchoredPosition;
			toPosition = panEnabled ? PickPanTarget() : Vector2.zero;
			fromScale = target.localScale.x;
			toScale = zoomEnabled ? baseScale + Random.Range(0f, zoomAmount) : baseScale;
			elapsed = 0f;
		}

		private Vector2 PickPanTarget()
		{
			if ((target.parent is RectTransform parent) == false) return Vector2.zero;
			Vector2 range = new Vector2(parent.rect.width, parent.rect.height) * panDistance;
			return new Vector2(Random.Range(-range.x, range.x), Random.Range(-range.y, range.y));
		}
	}
}
