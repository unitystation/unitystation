using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Logs;
using Managers.Supporters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Core.Networking.AsyncMessageQueue;

namespace UI.Systems.PreRound.Floating
{
	public sealed class FloatingSupporters : MonoBehaviour
	{
		[Header("Spawn Settings")]
		[SerializeField] private GameObject floatingSupporerPrefab;
		[SerializeField] private List<Sprite> supporterSprites = new List<Sprite>();
		[SerializeField] private float spriteScale = 1f;
		[SerializeField, Range(0f, 1f)] private float scaleVariation = 0.1f;
		[SerializeField] private int spriteSortingOrder = 1000;
		[SerializeField] private RectTransform spawnParent;
		[SerializeField] private float spawnInterval = 1.5f;
		[SerializeField] private int maxOnScreen = 8;
		[SerializeField] private float minSpeed = 0.5f;
		[SerializeField] private float maxSpeed = 2.0f;
		[SerializeField, Range(0.0f, 1.0f)] private float minViewportY = 0.1f;
		[SerializeField, Range(0.0f, 1.0f)] private float maxViewportY = 0.9f;
		[SerializeField] private bool spawnContinuously = true;

		private Camera _mainCamera;
		private int _currentOnScreen;
		private CancellationTokenSource _cts;

		// Queues (index-based) to avoid reusing the same supporter or sprite until all have been used
		private Queue<int> _supporterQueue = new Queue<int>();
		private Queue<int> _spriteQueue = new Queue<int>();
		// track the last-known counts so we only rebuild when the underlying list size changes
		private int _lastSupporterCount;
		private int _lastSpriteCount;

		private List<Supporter> _supportersCache = new List<Supporter>();

		private void Awake()
		{
			_mainCamera = Camera.main;
			if (spawnParent != null) return;
			var canvas = GetComponentInParent<Canvas>();
			if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
			if (canvas != null) spawnParent = canvas.transform as RectTransform;
		}

		private void OnEnable()
		{
			_cts = new CancellationTokenSource();
			SpawnLoopAsync(_cts.Token).Forget();
		}

		private void OnDisable()
		{
			_cts?.Cancel();
			_cts?.Dispose();
			_cts = null;

			_supporterQueue.Clear();
			_spriteQueue.Clear();
			_lastSupporterCount = 0;
			_lastSpriteCount = 0;
		}

		private async UniTask AskServerForSupporters()
		{
			await UniTask.WaitForSeconds(2f); // slight delay to allow connection setup
			var status = await RpcMessageQueue.Instance.Queue(RequestHandlerConstants.REQUEST_SUPPORTERS);
			Loggy.Info($"Status: {status.Status}\n data: {status.ValueFromJson}");
			if (status.Status == MessageStatus.Success)
			{
				try
				{
					var supporters = status.DeserializeFromText<List<Supporter>>();
					_supportersCache = supporters;
				}
				catch (Exception e)
				{
					Loggy.Error($"Error deserializing supporters: {e.Message}");
				}
			}
		}

		private async UniTaskVoid SpawnLoopAsync(CancellationToken token)
		{
			if (CustomNetworkManager.IsHeadless) return;
			await AskServerForSupporters();
			if (_supportersCache == null || _supportersCache.Count == 0)
			{
				return;
			}

			while (token.IsCancellationRequested == false && spawnContinuously)
			{
				if (_currentOnScreen < maxOnScreen)
				{
					SpawnOne();
				}

				try
				{
					await UniTask.WaitForSeconds(spawnInterval, cancellationToken: token);
				}
				catch (OperationCanceledException)
				{
					// cancelled - exit loop
					break;
				}
			}
		}

		private void SpawnOne()
		{
			if (_supportersCache == null || _supportersCache.Count == 0) return;
			if (floatingSupporerPrefab == null)
			{
				Loggy.Error("template not assigned. Cannot spawn.");
				return;
			}

			// pick next supporter index from queue (refill+shuffle when exhausted)
			int supIndex = GetNextSupporterIndex();
			if (supIndex < 0) return;
			var sup = _supportersCache[supIndex];
			bool fromLeft = UnityEngine.Random.value > 0.5f;

			Vector3 spawnViewport = new Vector3(fromLeft ? 0f : 1f, UnityEngine.Random.Range(minViewportY, maxViewportY), 0f);
			Vector3 moveDirectionViewport = fromLeft
				? new Vector3(-1f, UnityEngine.Random.Range(-0.4f, 0.1f), 0f).normalized
				: new Vector3(1f, UnityEngine.Random.Range(-0.4f, 0.1f), 0f).normalized;

			// Apply a small offset to spawn position so floaters smoothly enter the view
			// Offset is in the opposite direction of movement (slightly off-screen)
			Vector3 entryOffsetViewport = spawnViewport;
			float entryOffset = UnityEngine.Random.Range(0.08f, 0.18f); // percentage of screen to offset
			if (fromLeft)
			{
				entryOffsetViewport.x = -entryOffset;
			}
			else
			{
				entryOffsetViewport.x = 1f + entryOffset;
			}

			float rotSpeed = UnityEngine.Random.Range(-180f, 180f);
			if (spawnParent == null)
			{
				var canvas = GetComponentInParent<Canvas>();
				if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
				if (canvas != null) spawnParent = canvas.transform as RectTransform;
			}

			if (spawnParent == null)
			{
				Debug.LogWarning("FloatingSupporters: no Canvas/spawnParent found - disabling spawner.");
				enabled = false;
				return;
			}

			GameObject go = Instantiate(floatingSupporerPrefab, spawnParent);
			go.name = $"FloatingSupporter_{sup.Identifier}";
			go.SetActive(true);

			var rt = go.GetComponent<RectTransform>();
			var spawnPos = ViewportToCanvasLocal(entryOffsetViewport);
			rt.anchoredPosition = spawnPos;

			float scaleMultiplier = 1f + UnityEngine.Random.Range(-scaleVariation, scaleVariation);
			float actualScale = spriteScale * scaleMultiplier;
			rt.localScale = Vector3.one * actualScale;
			rt.SetAsLastSibling();

			var img = go.GetComponentInChildren<Image>();
			if (supporterSprites != null && supporterSprites.Count > 0)
			{
				if (img != null)
				{
					int spriteIndex = GetNextSpriteIndex();
					if (spriteIndex >= 0 && spriteIndex < supporterSprites.Count)
					{
						img.sprite = supporterSprites[spriteIndex];
					}
					img.preserveAspect = true;
					img.raycastTarget = false;
				}
			}

			var tmpText = go.GetComponentInChildren<TMP_Text>();
			if (tmpText != null)
			{
				tmpText.text = sup.Identifier ?? "Supporter";
			}

			// Convert movement direction from viewport space to canvas local space
			Vector3 moveDirectionCanvasStart = ViewportToCanvasLocal(Vector3.zero);
			Vector3 moveDirectionCanvasEnd = ViewportToCanvasLocal(moveDirectionViewport);
			Vector3 moveDirectionCanvas = (moveDirectionCanvasEnd - moveDirectionCanvasStart).normalized;

			var mover = go.GetComponent<FloatingMover>();
			if (mover != null)
			{
				mover.Initialize(
					moveDirectionCanvas,
					UnityEngine.Random.Range(minSpeed, maxSpeed),
					() => { _currentOnScreen--; },
					img,
					rotSpeed
				);
			}

			_currentOnScreen++;
		}

		private int GetNextSupporterIndex()
		{
			var list = _supportersCache;
			if (list == null || list.Count == 0) return -1;

			// refill and shuffle if queue exhausted or if the underlying supporter list size changed
			if (_supporterQueue == null) _supporterQueue = new Queue<int>();
			if (_supporterQueue.Count == 0 || _lastSupporterCount != list.Count)
			{
				var temp = Enumerable.Range(0, list.Count).ToList();
				ShuffleList(temp);
				_supporterQueue = new Queue<int>(temp);
				_lastSupporterCount = list.Count;
			}

			if (_supporterQueue.Count == 0) return -1;
			return _supporterQueue.Dequeue();
		}

		private int GetNextSpriteIndex()
		{
			if (supporterSprites == null || supporterSprites.Count == 0) return -1;
			if (_spriteQueue == null) _spriteQueue = new Queue<int>();
			if (_spriteQueue.Count == 0 || _lastSpriteCount != supporterSprites.Count)
			{
				var temp = Enumerable.Range(0, supporterSprites.Count).ToList();
				ShuffleList(temp);
				_spriteQueue = new Queue<int>(temp);
				_lastSpriteCount = supporterSprites.Count;
			}
			if (_spriteQueue.Count == 0) return -1;
			return _spriteQueue.Dequeue();
		}

		private static void ShuffleList<T>(List<T> list)
		{
			for (int i = list.Count - 1; i > 0; i--)
			{
				int j = UnityEngine.Random.Range(0, i + 1);
				(list[i], list[j]) = (list[j], list[i]);
			}
		}

		private Vector2 ViewportToCanvasLocal(Vector3 viewport)
		{
			var screenPoint = new Vector2(viewport.x * Screen.width, viewport.y * Screen.height);
			if (spawnParent == null)
			{
				return new Vector2((viewport.x - 0.5f) * 10f, (viewport.y - 0.5f) * 6f);
			}

			Camera camForCanvas = _mainCamera;
			var canvas = spawnParent.GetComponentInParent<Canvas>();
			if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) camForCanvas = null;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(spawnParent, screenPoint, camForCanvas, out Vector2 localPoint);
			return localPoint;
		}
	}
}
