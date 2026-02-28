using System.Collections.Generic;
using System.Linq;
using Logs;
using UnityEngine;
using US13.Core.Cooldowns;
using US13.Core.Initialisation;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interactions.Internal;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Sprite_Handler;
using US13.Player;
using US13.PlayerPrefs;
using US13.UI.Systems.AdminTools.DevTools;

namespace US13.Core.Highlight
{
	public class Highlight : MonoBehaviour, IInitialise
	{
		private const int HighlightPadding = 3;
		public static bool HighlightEnabled;
		public static Highlight instance;

		public GameObject TargetObject;

		public SpriteRenderer prefabSpriteRenderer;
		public SpriteRenderer spriteRenderer;
		public Material material;

		private static List<SpriteHandler> subscribeSpriteHandlers = new();
		private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
		private static GameObject cachedTarget;
		private static Vector2Int cachedMaxSpriteSize;
		private static bool cachedSizeValid;


		public InitialisationSystems Subsystem => InitialisationSystems.Highlight;

		void IInitialise.Initialise()
		{
			if (UnityEngine.PlayerPrefs.HasKey(PlayerPrefKeys.EnableHighlights))
			{
				if (UnityEngine.PlayerPrefs.GetInt(PlayerPrefKeys.EnableHighlights) == 1)
				{
					HighlightEnabled = true;
				}
				else
				{
					HighlightEnabled = false;
				}
			}
			else
			{
				UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.EnableHighlights, 1);
				UnityEngine.PlayerPrefs.Save();
			}
		}

		public static void SetPreference(bool preference)
		{
			if (preference)
			{
				UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.EnableHighlights, 1);
				HighlightEnabled = true;
			}
			else
			{
				UnityEngine.PlayerPrefs.SetInt(PlayerPrefKeys.EnableHighlights, 0);
				HighlightEnabled = false;
			}

			UnityEngine.PlayerPrefs.Save();
		}

		private void Awake()
		{
			if (instance == null)
			{
				instance = this;
			}
		}

		public static void UpdateCurrentHighlight()
		{
			if (instance == null) return;
			if (HighlightEnabled && instance.TargetObject != null)
			{
				HighlightThis(instance.TargetObject);
			}
			else
			{
				foreach (var handler in subscribeSpriteHandlers)
				{
					if (handler == null) continue;
					handler.OnSpriteUpdated -= (UpdateCurrentHighlight);
				}

				subscribeSpriteHandlers.Clear();
			}
		}


		public static void DeHighlight()
		{
			if (HighlightEnabled)
			{
				if (instance.spriteRenderer == null)
				{
					instance.spriteRenderer = Instantiate(instance.prefabSpriteRenderer);
				}

				foreach (var handler in subscribeSpriteHandlers)
				{
					if (handler == null) continue;
					handler.OnSpriteUpdated -= (UpdateCurrentHighlight);
				}

				subscribeSpriteHandlers.Clear();

				Texture2D mainTex = instance.spriteRenderer.sprite.texture;
				var data = mainTex.GetPixels();
				for (int xy = 0; xy < data.Length; xy++)
				{
					data[xy] = new Color32(0, 0, 0, 0);
				}

				mainTex.SetPixels(data);
				mainTex.Apply();
				instance.TargetObject = null;
				cachedTarget = null;
				cachedSizeValid = false;
			}
		}

		public static void HighlightThis(GameObject highlightobject)
		{
			if (PlayerManager.LocalPlayerScript.IsNormal && HighlightEnabled)
			{
				if (highlightobject.TryGetComponent<Items.Attributes>(out var attributes))
				{
					if (attributes.NoMouseHighlight) return;
				}

				ShowHighlight(highlightobject);
			}
		}



		public static void ShowHighlight(GameObject highlightobject, bool ignoreHandApply = false)
		{
			if (instance.spriteRenderer == null)
			{
				instance.spriteRenderer = Instantiate(instance.prefabSpriteRenderer);
			}

			instance.TargetObject = highlightobject;
			instance.spriteRenderer.gameObject.SetActive(true);
			instance.spriteRenderer.enabled = true;
			var spriteRenderers = highlightobject.GetComponentsInChildren<SpriteRenderer>();
			SpriteRenderer rootRenderer = spriteRenderers.FirstOrDefault(x => x.sprite != null);
			if (rootRenderer == null)
			{
				return;
			}
			UnityEngine.Transform trans = instance.spriteRenderer.transform;

			trans.SetParent(rootRenderer.transform, false);
			trans.localPosition = Vector3.zero;
			trans.transform.localRotation = Quaternion.Euler(0, 0, 0);
			trans.localScale = Vector3.one;
			instance.spriteRenderer.sortingLayerID = rootRenderer.sortingLayerID;

			foreach (var handler in subscribeSpriteHandlers)
			{
				if (handler == null) continue;
				handler.OnSpriteUpdated -= (UpdateCurrentHighlight);
				handler.OnSpriteUpdated -= (InvalidateCachedSize);
			}

			subscribeSpriteHandlers = highlightobject.GetComponentsInChildren<SpriteHandler>().ToList();
			foreach (var handler in subscribeSpriteHandlers)
			{
				if (handler == null) continue;
				handler.OnSpriteUpdated += (UpdateCurrentHighlight);
				handler.OnSpriteUpdated += (InvalidateCachedSize);
			}

			spriteRenderers = spriteRenderers.Where(x => x.sprite != null && x != instance.spriteRenderer && x.CompareTag("DontHighlightSpecial") == false).ToArray();
			if (cachedTarget != highlightobject)
			{
				cachedTarget = highlightobject;
				cachedSizeValid = false;
			}

			if (!cachedSizeValid)
			{
				cachedMaxSpriteSize = GetMaxSpriteRectSize(spriteRenderers);
				cachedSizeValid = true;
			}

			var maxSpriteSize = cachedMaxSpriteSize;
			var mainTex = EnsureHighlightTexture(instance.spriteRenderer, maxSpriteSize.x, maxSpriteSize.y, HighlightPadding);
			ClearTexture(mainTex);

			bool canHighlight = ignoreHandApply || CheckHandApply(highlightobject);
			if (!canHighlight)
			{
				mainTex.Apply();
				instance.spriteRenderer.enabled = false;
				return;
			}

			if (ignoreHandApply)
			{
				instance.material.SetColor(OutlineColor, Color.green);
			}

			foreach (var T in spriteRenderers)
			{
				if (DevCameraControls.ObjecIsVisible(T.gameObject) == false) continue;
				if (T.sortingLayerName == "Preview") continue;
				RecursiveTextureStack(mainTex, T, HighlightPadding);
			}

			mainTex.Apply();
			instance.spriteRenderer.enabled = true;
			instance.spriteRenderer.sprite = Sprite.Create(mainTex, new Rect(0, 0, mainTex.width, mainTex.height),
				new Vector2(0.5f, 0.5f), instance.spriteRenderer.sprite.pixelsPerUnit, 1, SpriteMeshType.FullRect, Vector4.zero);
		}


		static void RecursiveTextureStack(Texture2D mainTex, SpriteRenderer spriteRenderers, int padding)
		{
			if (spriteRenderers.gameObject.activeInHierarchy == false) return;
			Sprite sprite = spriteRenderers.sprite;
			Texture2D texture = sprite.texture;
			if (texture == null) return;

			int width = Mathf.RoundToInt(sprite.rect.width);
			int height = Mathf.RoundToInt(sprite.rect.height);
			if (width <= 0 || height <= 0) return;

			int maxX = mainTex.width - padding;
			int maxY = mainTex.height - padding;

			bool hasUvCorners = TryGetSpriteUvCorners(sprite, out var uvCorners);
			Rect rect = sprite.textureRect;

			for (int x = 0; x < width; x++)
			{
				int xx = padding + x;
				if (xx >= maxX) break;

				for (int y = 0; y < height; y++)
				{
					int yy = padding + y;
					if (yy >= maxY) break;

					Color color;
					if (hasUvCorners)
					{
						float u = (x + 0.5f) / width;
						float v = (y + 0.5f) / height;
						Vector2 uv = Vector2.Lerp(
							Vector2.Lerp(uvCorners.BottomLeft, uvCorners.BottomRight, u),
							Vector2.Lerp(uvCorners.TopLeft, uvCorners.TopRight, u),
							v);
						color = texture.GetPixelBilinear(uv.x, uv.y);
					}
					else
					{
						int texX = Mathf.FloorToInt(rect.x) + x;
						int texY = Mathf.FloorToInt(rect.y) + y;
						color = texture.GetPixel(texX, texY);
					}

					if (color.a != 0)
					{
						mainTex.SetPixel(xx, yy, color);
					}
				}
			}
		}

		private static Vector2Int GetMaxSpriteRectSize(SpriteRenderer[] renderers)
		{
			int maxW = 0;
			int maxH = 0;

			foreach (SpriteRenderer renderer in renderers)
			{
				if (renderer == null || renderer.sprite == null) continue;
				maxW = Mathf.Max(maxW, Mathf.RoundToInt(renderer.sprite.rect.width));
				maxH = Mathf.Max(maxH, Mathf.RoundToInt(renderer.sprite.rect.height));
			}

			return new Vector2Int(maxW, maxH);
		}

		private static Texture2D EnsureHighlightTexture(SpriteRenderer renderer, int spriteWidth, int spriteHeight, int padding)
		{
			Sprite currentSprite = renderer.sprite;
			Texture2D currentTexture = currentSprite != null ? currentSprite.texture : null;
			int targetWidth = Mathf.Max(1, spriteWidth + padding * 2);
			int targetHeight = Mathf.Max(1, spriteHeight + padding * 2);

			if (currentTexture != null && currentTexture.width == targetWidth && currentTexture.height == targetHeight)
			{
				return currentTexture;
			}

			TextureFormat format = currentTexture != null ? currentTexture.format : TextureFormat.RGBA32;
			var newTexture = new Texture2D(targetWidth, targetHeight, format, false)
			{
				filterMode = currentTexture != null ? currentTexture.filterMode : FilterMode.Point,
				wrapMode = currentTexture != null ? currentTexture.wrapMode : TextureWrapMode.Clamp,
				name = "HighlightTexture"
			};

			float pixelsPerUnit = currentSprite != null ? currentSprite.pixelsPerUnit : 32f;
			renderer.sprite = Sprite.Create(newTexture, new Rect(0, 0, targetWidth, targetHeight),
				new Vector2(0.5f, 0.5f), pixelsPerUnit, 1, SpriteMeshType.FullRect, Vector4.zero);
			return newTexture;
		}

		private static void ClearTexture(Texture2D texture)
		{
			var data = new Color32[texture.width * texture.height];
			texture.SetPixels32(data);
		}

		private struct SpriteUvCorners
		{
			public Vector2 BottomLeft;
			public Vector2 TopLeft;
			public Vector2 BottomRight;
			public Vector2 TopRight;
		}

		private static bool TryGetSpriteUvCorners(Sprite sprite, out SpriteUvCorners corners)
		{
			corners = default;
			var verts = sprite.vertices;
			var uvs = sprite.uv;
			if (verts == null || uvs == null || verts.Length != uvs.Length || verts.Length < 4)
			{
				return false;
			}

			float minX = float.PositiveInfinity;
			float maxX = float.NegativeInfinity;
			float minY = float.PositiveInfinity;
			float maxY = float.NegativeInfinity;

			for (int i = 0; i < verts.Length; i++)
			{
				var v = verts[i];
				minX = Mathf.Min(minX, v.x);
				maxX = Mathf.Max(maxX, v.x);
				minY = Mathf.Min(minY, v.y);
				maxY = Mathf.Max(maxY, v.y);
			}

			const float epsilon = 0.0001f;
			bool foundBL = false;
			bool foundTL = false;
			bool foundBR = false;
			bool foundTR = false;

			for (int i = 0; i < verts.Length; i++)
			{
				Vector2 v = verts[i];
				Vector2 uv = uvs[i];
				if (Mathf.Abs(v.x - minX) < epsilon && Mathf.Abs(v.y - minY) < epsilon)
				{
					corners.BottomLeft = uv;
					foundBL = true;
				}
				else if (Mathf.Abs(v.x - minX) < epsilon && Mathf.Abs(v.y - maxY) < epsilon)
				{
					corners.TopLeft = uv;
					foundTL = true;
				}
				else if (Mathf.Abs(v.x - maxX) < epsilon && Mathf.Abs(v.y - minY) < epsilon)
				{
					corners.BottomRight = uv;
					foundBR = true;
				}
				else if (Mathf.Abs(v.x - maxX) < epsilon && Mathf.Abs(v.y - maxY) < epsilon)
				{
					corners.TopRight = uv;
					foundTR = true;
				}
			}

			return foundBL && foundTL && foundBR && foundTR;
		}

		private static void InvalidateCachedSize()
		{
			cachedSizeValid = false;
		}

		public void OnDestroy()
		{
			foreach (SpriteHandler handler in subscribeSpriteHandlers)
			{
				if (handler == null) continue;
				handler.OnSpriteUpdated -= (UpdateCurrentHighlight);
			}

			subscribeSpriteHandlers.Clear();
		}


		public static bool CheckHandApply(GameObject target)
		{
			//call the used object's handapply interaction methods if it has any, for each object we are applying to
			HandApply handApply = HandApply.ByLocalPlayer(target);
			PositionalHandApply posHandApply = PositionalHandApply.ByLocalPlayer(target);

			handApply.IsHighlight = true;
			posHandApply.IsHighlight = true;

			//if handobj is null, then its an empty hand apply so we only need to check the receiving object
			if (handApply.HandObject != null)
			{
				//get all components that can handapply or PositionalHandApply
				var handAppliables = handApply.HandObject.GetComponents<MonoBehaviour>()
					.Where(c => c != null && c.enabled &&
								c is IBaseInteractable<HandApply> or IBaseInteractable<PositionalHandApply>);
				Loggy.Trace().Format("Checking HandApply / PositionalHandApply interactions from {0} targeting {1}",
					Category.Interaction, handApply.HandObject.name, target.name);

				foreach (var handAppliable in handAppliables.Reverse())
				{
					if (handAppliable is IBaseInteractable<HandApply>)
					{
						var hap = handAppliable as IBaseInteractable<HandApply>;
						if (CheckInteractInternal(hap, handApply, NetworkSide.Client))
						{
							instance.material.SetColor(OutlineColor, Color.cyan);
							return true;
						}
					}
					else
					{
						var hap = handAppliable as IBaseInteractable<PositionalHandApply>;
						if (CheckInteractInternal(hap, posHandApply, NetworkSide.Client))
						{
							instance.material.SetColor(OutlineColor, Color.magenta);
							return true;
						}
					}
				}
			}


			//call the hand apply interaction methods on the target object if it has any
			var targetHandAppliables = handApply.TargetObject.GetComponents<MonoBehaviour>()
				.Where(c => c != null && c.enabled &&
							c is IBaseInteractable<HandApply> or IBaseInteractable<PositionalHandApply>);
			foreach (MonoBehaviour targetHandAppliable in targetHandAppliables.Reverse())
			{
				if (targetHandAppliable is IBaseInteractable<HandApply> interactable)
				{
					//var hap = targetHandAppliable as IBaseInteractable<HandApply>;
					if (CheckInteractInternal(interactable, handApply, NetworkSide.Client))
					{
						instance.material.SetColor(OutlineColor, Color.green);
						return true;
					}
				}
				else
				{
					var hap = targetHandAppliable as IBaseInteractable<PositionalHandApply>;
					if (CheckInteractInternal(hap, posHandApply, NetworkSide.Client))
					{
						instance.material.SetColor(OutlineColor, new Color(1, 0.647f, 0));
						return true;
					}
				}
			}

			return false;
		}


		private static bool CheckInteractInternal<T>(IBaseInteractable<T> interactable, T interaction,
			NetworkSide side)
			where T : Interaction
		{
			if (Cooldowns.Cooldowns.IsOn(interaction, CooldownID.Asset(CommonCooldowns.Instance.Interaction, side))) return false;
			var result = false;
			//check if client side interaction should be triggered
			if (side == NetworkSide.Client && interactable is IClientInteractable<T> clientInteractable)
			{
				result = clientInteractable.Interact(interaction);
				if (result)
				{
					Loggy.Trace().Format("ClientInteractable triggered from {0} on {1} for object {2}",
						Category.Interaction, typeof(T).Name, clientInteractable.GetType().Name,
						(clientInteractable as Component)?.gameObject.name);
					Cooldowns.Cooldowns.TryStartClient(interaction, CommonCooldowns.Instance.Interaction);
					return true;
				}
			}

			//check other kinds of interactions
			if (interactable is ICheckable<T> checkable)
			{
				result = checkable.WillInteract(interaction, side);
				if (result)
				{
					Loggy.Trace().Format("WillInteract triggered from {0} on {1} for object {2}", Category.Interaction,
						typeof(T).Name, checkable.GetType().Name,
						(checkable as Component)?.gameObject.name);
					return true;
				}
			}
			else if (interactable is IInteractable<T>)
			{
				//use default logic
				result = DefaultWillInteract.Default(interaction, side);
				if (result)
				{
					Loggy.Trace().Format("WillInteract triggered from {0} on {1} for object {2}", Category.Interaction,
						typeof(T).Name, interactable.GetType().Name,
						(interactable as Component)?.gameObject.name);

					return true;
				}
			}

			Loggy.Trace().Format("No interaction triggered from {0} on {1} for object {2}", Category.Interaction,
				typeof(T).Name, interactable.GetType().Name,
				(interactable as Component)?.gameObject.name);

			return false;
		}
	}
}
