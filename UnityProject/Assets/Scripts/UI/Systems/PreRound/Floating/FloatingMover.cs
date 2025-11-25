using UnityEngine;
using UnityEngine.UI;

namespace UI.Systems.PreRound.Floating
{
	public sealed class FloatingMover : MonoBehaviour
	{
		private Vector3 moveDirection;
		private float speed;
		private System.Action onFinished;
		private RectTransform _rectTransform;
		private bool _isUI;
		private float rotationSpeed;
		private float currentAngle;

		private Image _image;
		private FloatingLabel _floatingLabel;
		private float labelOffset = 40f;
		private float showDotThreshold = 0.65f; // cos(angle). ~50-55 degrees
		private float lifetime = 0;

		public void Initialize(Vector3 targetWorldOrLocal, float moveSpeed, System.Action onFinishedCallback, Image img, float rotationSpeedDegPerSec = 90f)
		{
			speed = moveSpeed;
			onFinished = onFinishedCallback;
			_rectTransform = GetComponent<RectTransform>();
			_isUI = _rectTransform != null && _rectTransform.GetComponentInParent<Canvas>() != null;
			rotationSpeed = rotationSpeedDegPerSec;
			currentAngle = 0f;
			Vector3 startPosition = _isUI && _rectTransform != null
				? _rectTransform.anchoredPosition
				: transform.position;
			moveDirection = (targetWorldOrLocal - startPosition).normalized;
			_image = img;
		}

		private void LateUpdate()
		{
			if (speed <= 0f) return;

			var dt = Time.deltaTime;
			lifetime += dt;

			currentAngle += rotationSpeed * dt;
			_image.transform.localEulerAngles = new Vector3(0f, 0f, currentAngle);

			Vector3 nextPos = (_isUI && _rectTransform != null
				? _rectTransform.anchoredPosition
				: transform.position) + moveDirection * speed * dt;

			if (_isUI && _rectTransform != null)
			{
				_rectTransform.anchoredPosition = new Vector2(nextPos.x, nextPos.y);
			}
			else
			{
				transform.position = nextPos;
			}

			if (IsOffScreen())
			{
				Finish();
			}
		}

		private bool IsOffScreen()
		{
			if (lifetime < 30f) return false;
			if (_isUI && _rectTransform != null)
			{
				var canvas = _rectTransform.GetComponentInParent<Canvas>();
				if (canvas == null) return true;

				var canvasRect = canvas.GetComponent<RectTransform>();
				var pos = _rectTransform.anchoredPosition;

				// Add a small margin for off-screen detection
				float margin = 50f;
				return pos.x < -canvasRect.rect.width / 2 - margin ||
					   pos.x > canvasRect.rect.width / 2 + margin ||
					   pos.y < -canvasRect.rect.height / 2 - margin ||
					   pos.y > canvasRect.rect.height / 2 + margin;
			}
			else
			{
				// For world space objects, use camera frustum
				var cam = Camera.main;
				if (cam == null) return true;
				return !cam.isActiveAndEnabled || !GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(cam), new Bounds(transform.position, Vector3.one * 0.1f));
			}
		}

		private void Finish()
		{
			onFinished?.Invoke();
			Destroy(gameObject);
		}
	}
}