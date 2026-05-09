using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using US13.Core.Highlight;
using US13.Core.Sprite_Handler;
using US13.Items;

namespace US13.UI.Systems.MainHUD.UI_Bottom
{
	/// <summary>
	/// This class manages items sprites rendering for UI Images
	/// It creates new Image instances in root gameobject for each sprite render in item
	/// </summary>
	public class UI_ItemImage
		//TODO how to Handle multiple of the same slot being shown?
		//so, bool To allow repairing?
	{
		public static Dictionary<GameObject,UI_ItemImage> UI_ItemImages  = new Dictionary<GameObject,UI_ItemImage>(); //how to Clear on Round end?

		public static float AnimationSpeed;

		private static readonly int IsPaletted = Shader.PropertyToID("_IsPaletted");
		private static readonly int PaletteSize = Shader.PropertyToID("_PaletteSize");
		private static readonly int ColorPalette = Shader.PropertyToID("_ColorPalette");
		private GameObject CurrentlyOn;

		private GameObject SpriteContainer;
		public readonly GameObject Displaying;
		private bool hidden;

		private Stack<ImageAndHandler> usedImages = new Stack<ImageAndHandler>();
		private Stack<ImageAndHandler> freeImages = new Stack<ImageAndHandler>();
		private Image overlay;

		private Material imgMat;
		private bool Parentless;
		private bool IsCanFitPreview;

		/// <summary>
		/// The first sprite in rendered item
		/// Null if there is no item
		/// </summary>
		public Sprite MainSprite
		{
			get
			{
				if (usedImages.Count != 0)
				{
					var firstImage = usedImages.Peek();
					if (firstImage != null && firstImage.Handler)
					{
						return firstImage.Handler.CurrentSprite;
					}
				}

				return null;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="currentlyOn">Object to be used as parent for new Image instances</param>
		public UI_ItemImage(GameObject currentlyOn, GameObject CopyObject , Material imgMat, bool IsCanFitPreview = false, Color? colour = null)
		{
			this.CurrentlyOn = currentlyOn;
			this.Displaying = CopyObject;
			this.IsCanFitPreview = IsCanFitPreview;
			this.imgMat = imgMat;
			if (IsCanFitPreview == false)
			{
				if (UI_ItemImages.ContainsKey(CopyObject) == false)
				{
					UI_ItemImages[CopyObject] = this;
				}
				else
				{
					Parentless = true;
				}
			}

			// generate and hide overlay image
			overlay = CreateNewImage(imgMat, "uiItemImageOverlay");
			SetOverlay(null);
			ShowItem(colour);
		}


		public static UI_ItemImage RequestItemImage(GameObject Container, GameObject ObjectCopy, bool IsCanFitPreview = false, Color? colour = null, bool MakeNewPreviewNotEmpty = false)
		{
			Material imgMat = CommonMaterials.Instance.ItemSlotMaterial;

			if (UI_ItemImages.ContainsKey(ObjectCopy) && IsCanFitPreview == false && MakeNewPreviewNotEmpty == false)
			{
				UI_ItemImages[ObjectCopy].SetParent(Container, false);
				return UI_ItemImages[ObjectCopy];
			}
			else
			{
				return new UI_ItemImage(Container, ObjectCopy, imgMat, IsCanFitPreview, colour);
			}
		}



		public void SetParent(GameObject parent, bool Force)
		{
			CurrentlyOn = parent;
			//SpriteContainer.transform.SetParent(CurrentlyOn.transform);
			var rt = SpriteContainer.GetComponent<RectTransform>();



			SpriteContainer.transform.SetParent(CurrentlyOn.transform, worldPositionStays: true);
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.sizeDelta = Vector2.zero;
			// rt.anchoredPosition = Vector2.zero;
			rt.localScale = Vector3.one;
			if (Force == false)
			{
				LeanTween.value(SpriteContainer.gameObject,
						rt.anchoredPosition,
						Vector2.zero,
						AnimationSpeed)
					.setEase(LeanTweenType.easeInCirc)
					.setOnUpdate((Vector2 pos) =>
					{
						rt.anchoredPosition = pos;
					})
					.setOnComplete(() =>
					{
						// Snap clean at the end
						rt.anchoredPosition = Vector2.zero;
						rt.anchorMin = Vector2.zero;
						rt.anchorMax = Vector2.one;
						rt.sizeDelta = Vector2.zero;
						rt.localScale = Vector3.one;
					});
			}
			else
			{
				rt.anchorMin = Vector2.zero;
				rt.anchorMax = Vector2.one;
				rt.sizeDelta = Vector2.zero;
				rt.anchoredPosition = Vector2.zero;
				rt.localScale = Vector3.one;
			}
		}

		/// <summary>
		/// Disable all sprites, but not reset their value
		/// </summary>
		public void SetHidden(bool hidden)
		{
			this.hidden = hidden;
			foreach (var pair in usedImages)
			{
				pair.UIImage.enabled = !hidden;
				pair.UIImage.preserveAspect = !hidden;
			}
		}




		/// <summary>
		/// Display item as a composition of Image objects in UI
		/// </summary>
		private void ShowItem(Color? colour = null)
		{
			//determine the sprites to display based on the new item
			var spriteHandlers = Displaying.GetComponentsInChildren<SpriteHandler>(includeInactive: true);
			spriteHandlers = spriteHandlers.Where(x => x != Highlight.instance.spriteRenderer).ToArray();

			foreach (var handler in spriteHandlers)
			{
				// get unused image from stack and subscribe it handler updates
				var image = ConnectFreeImageToHandler(handler, imgMat);

				// check if handler is hidden
				image.gameObject.SetActive(!handler.IsHidden);

				// set sprite
				var sprite = handler.CurrentSprite;
				image.sprite = sprite;

				//set color
				if (colour != null)
				{
					image.color = colour.GetValueOrDefault(Color.white);
				}
				else
				{
					var color = handler.CurrentColor;
					image.color = color;
				}

				// Configure the shader to use palette if item uses it
				var itemAttrs = Displaying.GetComponent<ItemAttributesV2>();
				if (itemAttrs.ItemSprites.IsPaletted)
				{
					image.material.SetInt(IsPaletted, 1);
					image.material.SetInt(PaletteSize, itemAttrs.ItemSprites.Palette.Count);
					image.material.SetColorArray(ColorPalette, itemAttrs.ItemSprites.Palette.ToArray());
				}
				else
				{
					image.material.SetInt(IsPaletted, 0);
				}

				var colorSync = Displaying.GetComponent<SpriteColorSync>();
				if (colorSync != null)
				{   //later find a way to remove this listener when no longer needed
					colorSync.OnColorChange.AddListener(TrackColor);

					void TrackColor(Color newColor)
					{
						if (colorSync.SpriteRenderer != null
						    && colorSync.SpriteRenderer.sprite == image.sprite)
						{
							image.color = newColor;
						}
					}
				}

				image.enabled = !hidden;
				image.preserveAspect = !hidden;
			}
		}

		/// <summary>
		/// Set overlay image for item (like handcufs icon)
		/// Null to clear sprite and hide image
		/// </summary>
		/// <param name="sprite"></param>
		public void SetOverlay(Sprite overlaySprite)
		{
			if (overlaySprite != null)
			{
				overlay.sprite = overlaySprite;
				overlay.enabled = !hidden;
				overlay.preserveAspect = true;
			}
			else
			{
				overlay.sprite = null;
				overlay.enabled = false;
			}
		}

		public void IDontNeedYouAnymore()
		{

		}

		/// <summary>
		/// Disable all images and reset their sprites
		/// </summary>
		public void ClearAll(GameObject HolderRequesting, bool ForceDestroy = false, bool ClearOnlyPreviews = false)
		{
			if (ForceDestroy)
			{
				Object.Destroy(SpriteContainer);
				UI_ItemImages.Remove(Displaying);
				return;
			}

			if (IsCanFitPreview || Parentless)
			{
				Object.Destroy(SpriteContainer);
				return;
			}
			else
			{
				if (HolderRequesting == CurrentlyOn && ClearOnlyPreviews == false)
				{
					UI_ItemImages.Remove(Displaying);
					Object.Destroy(SpriteContainer);
					//SetParent(UIManager.Instance.UI_SlotManager.gameObject,false);
				}
			}
			return; //TODO
			while (usedImages.Count != 0)
			{
				var usedImage = usedImages.Pop();
				usedImage.Clear();

				if (usedImage.UIImage != null)
				{
					freeImages.Push(usedImage);
				}
				else
				{
					usedImage.Clear();
				}

				// reset and hide used image
				//usedImage.Handler = null;
				//usedImage.UIImage.enabled = false;
			}

			SetOverlay(null);
		}

		private Image ConnectFreeImageToHandler(SpriteHandler handler, Material imgMat)
		{
			ImageAndHandler pair;
			if (freeImages.Count > 0)
			{
				pair = freeImages.Pop();
			}
			else
			{
				var img = CreateNewImage(imgMat);
				pair = new ImageAndHandler(img);
			}


			if (SpriteContainer == null)
			{
				SpriteContainer = new GameObject();
				SpriteContainer.AddComponent<RectTransform>(); // This converts the Transform to RectTransform
				SpriteContainer.transform.SetParent(CurrentlyOn.transform);
				var rta = SpriteContainer.GetComponent<RectTransform>();
				rta.anchorMin = Vector2.zero;
				rta.anchorMax = Vector2.one;
				rta.sizeDelta = Vector2.zero;
				rta.anchoredPosition = Vector2.zero;
				rta.localScale = Vector3.one;
			}

			var rt = pair.UIImage.GetComponent<RectTransform>();
			rt.SetParent(SpriteContainer.transform);
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.sizeDelta = Vector2.zero;
			rt.anchoredPosition = Vector2.zero;
			rt.localScale = Vector3.one;

			pair.Handler = handler;
			usedImages.Push(pair);

			return pair.UIImage;
		}

		private Image CreateNewImage(Material imgMat, string name = "uiItemImage")
		{ var go = new GameObject(name, typeof(RectTransform));


			var img = go.AddComponent<Image>();
			img.material = Object.Instantiate(imgMat);
			img.alphaHitTestMinimumThreshold = 0.5f;
			img.raycastTarget = false;

			return img;
		}

		/// <summary>
		/// This class subscribe UIImage to SpriteHandler updates
		/// If SpriteHandler updates sprite this will also update it for UIImage
		/// </summary>
		public class ImageAndHandler
		{
			public static readonly List<System.WeakReference<ImageAndHandler>> ItemList = new();

			System.WeakReference<Image> _img;

			public Image UIImage
			{
				get
				{
					Image trg;
					if (!_img.TryGetTarget(out trg))
					{
						return null;
					}
					else
					{
						return trg;
					}
				}
				private set
				{
					_img = new System.WeakReference<Image>(value);
				}
			}
			private SpriteHandler handler;

			public static void ClearAll()
			{
				foreach (var a in ItemList)
				{
					ImageAndHandler iah;

					if (a.TryGetTarget(out iah))
					{
						try
						{
							iah.Clear();
						}
						catch(System.Exception ee)
						{
							Debug.LogException(ee);
						}
					}
				}

				ItemList.Clear();
			}

			public ImageAndHandler(Image image)
			{
				ItemList.Add(new System.WeakReference<ImageAndHandler>(this));
				UIImage = image;
			}

			public SpriteHandler Handler
			{
				get
				{
					return handler;
				}
				set
				{
					// unsubscribe from old handler changes
					if (handler != null)
					{
						handler.OnSpriteChanged.Remove(OnHandlerSpriteChanged);
						handler.OnColorChanged.Remove(OnHandlerColorChanged);
					}

					handler = value;

					// subscribe to new handler changes
					if (handler)
					{
						OnHandlerSpriteChanged(handler.CurrentSprite);
						OnHandlerColorChanged(handler.CurrentColor);
						handler.OnSpriteChanged.Add(OnHandlerSpriteChanged);
						handler.OnColorChanged.Add(OnHandlerColorChanged);
					}
				}
			}

			private void OnHandlerColorChanged(Color newColor)
			{
				if (!UIImage)
				{
					// looks like image was deleted from scene
					// this happens when item is moved in container
					// and player close this container
					handler.OnSpriteChanged.Remove(OnHandlerSpriteChanged);
					handler.OnColorChanged.Remove(OnHandlerColorChanged);
					return;
				}

				UIImage.color = newColor;
			}

			private void OnHandlerSpriteChanged(Sprite sprite)
			{
				if (UIImage == false)
				{
					// looks like image was deleted from scene
					// this happens when item is moved in container
					// and player close this container
					handler.OnSpriteChanged.Remove(OnHandlerSpriteChanged);
					handler.OnColorChanged.Remove(OnHandlerColorChanged);
					return;
				}

				if (sprite && handler.gameObject.activeInHierarchy)
				{
					UIImage.gameObject.SetActive (true);
					UIImage.sprite = sprite;
				}
				else
				{
					UIImage.gameObject.SetActive(false);
				}

			}

			internal void Clear()
			{
				OnHandlerSpriteChanged(null);
				OnHandlerColorChanged(Color.white);
				handler.OnSpriteChanged.Remove(OnHandlerSpriteChanged);
				handler.OnColorChanged.Remove(OnHandlerColorChanged);
			}
		}
	}
}