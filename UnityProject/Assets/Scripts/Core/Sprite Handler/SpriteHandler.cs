using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif
using UnityEngine.UI;


///	<summary>
///	for Handling sprite animations
///	</summary>
[ExecuteInEditMode]
public class SpriteHandler : MonoBehaviour
{
	[SerializeField] public bool NetworkThis = true;

	[SerializeField] private List<SpriteDataSO> SubCatalogue = new List<SpriteDataSO>();

	[SerializeField] private SpriteDataSO PresentSpriteSet;
	private SpriteDataSO.Frame PresentFrame = null;

	[Tooltip("If checked, a random sprite SO will be selected during initialization from the catalogue of sprite SOs.")]
	[SerializeField] private bool randomInitialSprite = false;

	private SpriteRenderer spriteRenderer;

	public SpriteRenderer SpriteRenderer => spriteRenderer;
	private Image image;

	private int animationIndex = 0;

	[SerializeField]
	private bool pushTextureOnStartUp = true;

	[FormerlySerializedAs("variantIndex"), SerializeField, Range(0, 3)]
	private int initialVariantIndex = 0;

	private int variantIndex = 0;

	private int cataloguePage = -1;

	/// <summary>
	/// Returns the current catalogue page
	/// </summary>
	public int CurrentSpriteIndex => cataloguePage;

	private float timeElapsed = 0;

	private bool isAnimation = false;

	private bool animateOnce;

	private Color? setColour = null;

	[Tooltip("The palette that is applied to the Sprite Renderer, if the Present Sprite Set is paletted.")]
	[SerializeField] private List<Color> palette = new List<Color>();
	public List<Color> Palette => palette;

	/// <summary>
	/// false if the palette has not been configured for the current spriteSO. true otherwise
	/// </summary>
	private bool isPaletteSet = false;

	private bool initialised = false;

	private NetworkIdentity networkIdentity;

	private bool isSubCatalogueChanged = false;

	/// <summary>
	/// The catalogue index representing the current sprite SO.
	/// </summary>
	public int CataloguePage => cataloguePage;

	/// <summary>
	/// Invokes when sprite just changed by animation or other script
	/// Null if sprite became hidden
	/// </summary>
	public event Action<Sprite> OnSpriteChanged;

	/// <summary>
	/// Invokes when sprite data scriptable object is changed
	/// Null if sprite became hidden
	/// </summary>
	public event Action<SpriteDataSO> OnSpriteDataSOChanged;

	/// <summary>
	/// Invoke when sprite handler has changed color of sprite
	/// </summary>
	public event Action<Color> OnColorChanged;

	/// <summary>
	/// The amount of SubCatalogues defined for this SpriteHandler.
	/// </summary>
	public int CatalogueCount => SubCatalogue.Count;

	/// <summary>
	/// Current sprite from SpriteRender or Image
	/// Null if sprite is hidden
	/// </summary>
	public Sprite CurrentSprite
	{
		get
		{
			if (spriteRenderer)
			{
				return spriteRenderer.sprite;
			}
			else if (image)
			{
				return image.sprite;
			}

			return null;
		}
	}

	/// <summary>
	/// Current sprite color from SpriteRender or Image
	/// White means no color modification was added
	/// </summary>
	public Color CurrentColor
	{
		get
		{
			if (spriteRenderer)
			{
				return spriteRenderer.color;
			}
			else if (image)
			{
				return image.color;
			}

			return Color.white;
		}
	}

	/// <summary>
	/// Check if this sprite hander is rendering
	/// </summary>
	public bool IsHidden
	{
		get
		{
			return CurrentSprite == null || gameObject.activeInHierarchy == false;
		}
	}

	public NetworkIdentity GetMasterNetID()
	{
		return networkIdentity;
	}

	/// <summary>
	/// Changes the object's active <see cref="SpriteDataSO"></see>.
	/// </summary>
	/// <param name="cataloguePage">Index as defined via the inspector on the object.</param>
	/// <param name="networked">Whether this change should be sent to clients, if server.</param>
	public void ChangeSprite(int cataloguePage, bool networked = true)
	{
		InternalChangeSprite(cataloguePage, networked);
	}

	/// <summary>
	/// When the animation for the given SO is complete,
	/// the current SO index is incremented (looping to 0 if needed) instead of looping the SO's animation.
	/// </summary>
	public void AnimateOnce(int cataloguePage, bool networked = true)
	{
		InternalChangeSprite(cataloguePage, networked, true);
	}

	private void InternalChangeSprite(int cataloguePage, bool networked = true, bool animateOnce = false)
	{
		if (animateOnce == false)
		{
			if ((this.cataloguePage > -1 && cataloguePage == this.cataloguePage) || cataloguePage < 0) return;
		}

		if (cataloguePage >= SubCatalogue.Count)
		{
			Logger.LogError($"Sprite catalogue index '{cataloguePage}' is out of bounds on {transform.parent.gameObject}.");
			return;
		}

		this.cataloguePage = cataloguePage;
		if (isSubCatalogueChanged)
		{
			SetSpriteSO(SubCatalogue[cataloguePage]);
		}
		else
		{
			SetSpriteSO(SubCatalogue[cataloguePage], networked: false);
			if (networked)
			{
				NetUpdate(newCataloguePage: cataloguePage, newAnimateOnce: animateOnce);
			}
		}

		this.animateOnce = animateOnce;
	}

	public void SetSpriteSO(SpriteDataSO newSpriteSO, Color? color = null, int newVariantIndex = -1,
		bool networked = true)
	{
		if (newSpriteSO == null) return;
		if (newSpriteSO != PresentSpriteSet)
		{
			isPaletteSet = false;
			PresentSpriteSet = newSpriteSO;
			// TODO: Network, change to network catalogue message
			// See https://github.com/unitystation/unitystation/pull/5675#pullrequestreview-540239428
			cataloguePage = SubCatalogue.FindIndex(SO => SO == newSpriteSO);
			PushTexture(networked);
			if (networked)
			{
				NetUpdate(newSpriteSO);
			}
			OnSpriteDataSOChanged?.Invoke(newSpriteSO);
		}

		if (color != null)
		{
			SetColor(color.GetValueOrDefault(Color.white), networked);
		}

		if (newSpriteSO.Variance.Count - 1 < variantIndex)
		{
			newVariantIndex = 0;
		}
		if (newVariantIndex > -1)
		{
			ChangeSpriteVariant(newVariantIndex, networked);
		}
	}


	//TODO
	//Player?
	//customisation
	//console animators

	/// <summary>
	/// Used to set a singular sprite NOTE: This will not be networked
	/// </summary>
	/// <param name="_sprite">Sprite.</param>
	public void SetSprite(Sprite _sprite)
	{
		SetImageSprite(_sprite);
		TryToggleAnimationState(false);
	}

	public void ChangeSpriteVariant(int spriteVariant, bool networked = true)
	{
		if (PresentSpriteSet != null)
		{
			if (spriteVariant < PresentSpriteSet.Variance.Count &&
			    variantIndex != spriteVariant)
			{
				if (PresentSpriteSet.Variance[spriteVariant].Frames.Count <= animationIndex)
				{
					animationIndex = 0;
				}

				variantIndex = spriteVariant;
				var Frame = PresentSpriteSet.Variance[variantIndex].Frames[animationIndex];
				SetSprite(Frame);

				TryToggleAnimationState(PresentSpriteSet.Variance[variantIndex].Frames.Count > 1);
				if (networked)
				{
					NetUpdate(newVariantIndex: spriteVariant);
				}
			}
		}
	}

	public Color GetColor()
	{
		if (setColour == null)
		{
			UpdateImageColor();
		}

		return setColour.Value;
	}

	public void SetColor(Color value, bool networked = true)
	{
		if (initialised == false) TryInit();
		if (setColour == value) return;
		setColour = value;
		if (HasImageComponent() == false)
		{
			GetImageComponent();
		}

		SetImageColor(value);
		if (networked)
		{
			NetUpdate(newSetColor: value);
		}
	}

	public void ClearPalette(bool networked = true)
	{
		if (palette == null) return;
		palette = null;
		isPaletteSet = false;

		if (networked)
		{
			NetUpdate(newClearPalette: true);
		}
	}

	public void Empty(bool clearCatalogue = false, bool networked = true)
	{
		if (clearCatalogue)
		{
			SubCatalogue = new List<SpriteDataSO>();
		}

		if (HasSpriteInImageComponent() == false && PresentSpriteSet == null && cataloguePage == -1) return;
		cataloguePage = -1;
		PushClear(false);
		PresentSpriteSet = null;
		OnSpriteDataSOChanged?.Invoke(null);

		if (networked)
		{
			NetUpdate(newEmpty: true);
		}
	}

	public void PushClear(bool networked = true)
	{
		if (initialised == false) TryInit();
		if (HasSpriteInImageComponent() == false) return;

		SetImageSprite(null);
		TryToggleAnimationState(false);
		if (networked)
		{
			NetUpdate(newPushClear: true);
		}
	}

	/// <summary>
	/// Sets the sprite catalogue for server side only, Any calls to ChangeSprite Will automatically be networked In a different way
	/// </summary>
	public void SetCatalogue(List<SpriteDataSO> newCatalogue, int initialPage = -1, bool networked = true)
	{
		isSubCatalogueChanged = true;
		SubCatalogue = newCatalogue;
		if (initialPage > -1)
		{
			ChangeSprite(initialPage, networked);
		}
	}

	public void SetPaletteOfCurrentSprite(List<Color> newPalette, bool networked = true)
	{
		bool paletted = IsPaletted();

		Debug.Assert((paletted && newPalette == null) == false, "Paletted sprites should never have palette set to null");

		if (paletted == false)
		{
			newPalette = null;
		}

		isPaletteSet = false;
		palette = newPalette;
		PushTexture(false);
		if (networked)
		{
			NetUpdate(newPalette: palette);
		}
	}

	public void PushTexture(bool networked = true)
	{
		if (initialised == false) TryInit();
		if (PresentSpriteSet != null && PresentSpriteSet.Variance.Count > 0)
		{
			if (variantIndex < PresentSpriteSet.Variance.Count)
			{
				if (animationIndex >= PresentSpriteSet.Variance[variantIndex].Frames.Count)
				{
					animationIndex = 0;
				}

				var Frame = PresentSpriteSet.Variance[variantIndex].Frames[animationIndex];

				SetSprite(Frame);

				TryToggleAnimationState(PresentSpriteSet.Variance[variantIndex].Frames.Count > 1);
				if (networked)
				{
					NetUpdate(newPushTexture: true);
					//NetWork this a poke Basically
				}

				return;
			}
		}


		if (networked && HasSpriteInImageComponent())
		{
			NetUpdate(newPushTexture: true);
		}

		SetImageSprite(null);
		TryToggleAnimationState(false);
	}

	/// <summary>
	/// Toggles the SpriteRenderer texture. Calls PushTexture() if the new state is on, or PushClear() otherwise.
	/// </summary>
	/// <param name="newState">If on, sets the texture (to last known). If off, clears the texture.</param>
	/// <param name="networked">Will send update to clients if true (default).</param>
	public void ToggleTexture(bool newState, bool networked = true)
	{
		if (newState)
		{
			PushTexture(networked);
		}
		else
		{
			PushClear(networked);
		}
	}

	private void NetUpdate(
		SpriteDataSO newSpriteSO = null,
		int newVariantIndex = -1,
		int newCataloguePage = -1,
		bool newPushTexture = false,
		bool newEmpty = false,
		bool newPushClear = false,
		bool newClearPalette = false,
		Color? newSetColor = null,
		List<Color> newPalette = null,
		bool newAnimateOnce = false)
	{
		if (NetworkThis == false) return;
		if (SpriteHandlerManager.Instance == null) return;
		if (networkIdentity == null)
		{
			if (this?.gameObject == null) return;
			var NetID = SpriteHandlerManager.GetRecursivelyANetworkBehaviour(this.gameObject);
			if (NetID == null)
			{
				Logger.LogError("Was unable to find A NetworkBehaviour for ",
					Category.Sprites);
				return;
			}

			networkIdentity = NetID;
			if (networkIdentity == null)
			{
				var gamename = "";
				if (this?.gameObject != null)
				{
					gamename = gameObject.name;
				}
				Logger.LogError("Was unable to find A NetworkBehaviour for " + gamename,
					Category.Sprites);
			}
		}

		if (CustomNetworkManager.Instance._isServer == false) return;

		SpriteHandlerManager.SpriteChange spriteChange = null;

		if (SpriteHandlerManager.Instance.QueueChanges.ContainsKey(this))
		{
			spriteChange = SpriteHandlerManager.Instance.QueueChanges[this];
		}
		else
		{
			spriteChange = SpriteHandlerManager.GetSpriteChange();
		}

		if (newSpriteSO != null)
		{
			if (newSpriteSO.setID == -1)
			{
				Logger.Log("NewSpriteDataSO NO ID!" + newSpriteSO.name, Category.Sprites);
			}
			if (spriteChange.Empty) spriteChange.Empty = false;
			spriteChange.PresentSpriteSet = newSpriteSO.setID;
		}

		if (newVariantIndex != -1)
		{
			spriteChange.VariantIndex = newVariantIndex;
		}

		if (newCataloguePage != -1)
		{
			spriteChange.CataloguePage = newCataloguePage;
		}

		if (newPushTexture)
		{
			if (spriteChange.PushClear) spriteChange.PushClear = false;
			spriteChange.PushTexture = newPushTexture;
		}

		if (newEmpty)
		{
			if (spriteChange.PresentSpriteSet != -1) spriteChange.PresentSpriteSet = -1;
			spriteChange.Empty = newEmpty;
		}

		if (newPushClear)
		{
			if (spriteChange.PushTexture) spriteChange.PushTexture = false;
			spriteChange.PushClear = newPushClear;
		}

		if (newAnimateOnce)
		{
			if (spriteChange.AnimateOnce) spriteChange.AnimateOnce = false;
			spriteChange.AnimateOnce = newAnimateOnce;
		}

		if (newClearPalette)
		{
			if (spriteChange.Palette != null) spriteChange.Palette = null;
			spriteChange.ClearPalette = newClearPalette;
		}

		if (newSetColor != null)
		{
			spriteChange.SetColour = newSetColor;
		}

		if (newPalette != null)
		{
			if (spriteChange.ClearPalette) spriteChange.ClearPalette = false;
			spriteChange.Palette = newPalette;
		}

		if (networkIdentity.netId == 0)
		{
			//Logger.Log("ID hasn't been set for " + this.transform.parent);
			StartCoroutine(WaitForNetInitialisation(spriteChange));
		}
		else
		{
			SpriteHandlerManager.Instance.QueueChanges[this] = spriteChange;
		}

	}

	private IEnumerator WaitForNetInitialisation(SpriteHandlerManager.SpriteChange spriteChange)
	{
		yield return null;
		if (networkIdentity.netId == 0)
		{
			Logger.LogError($"ID hasn't been set for ${this.transform.parent}.", Category.Sprites);
			yield break;
		}

		if (SpriteHandlerManager.Instance.QueueChanges.ContainsKey(this))
		{
			spriteChange.MergeInto(SpriteHandlerManager.Instance.QueueChanges[this]);

		}
		SpriteHandlerManager.Instance.QueueChanges[this] = spriteChange;

	}


	void Awake()
	{
		if (Application.isPlaying)
		{
			TryInit();
		}
	}

	void Start()
	{
		if (Application.isPlaying)
		{
			TryInit();
		}
	}

	private void OnDestroy()
	{
		if (SpriteHandlerManager.Instance)
		{
			SpriteHandlerManager.Instance.QueueChanges.Remove(this);
			SpriteHandlerManager.Instance.NewClientChanges.Remove(this);
		}
	}

	private void SetImageColor(Color value)
	{
		if (spriteRenderer != null)
		{
			spriteRenderer.color = value;
		}
		else if (image != null)
		{
			image.color = value;
		}

		OnColorChanged?.Invoke(value);
	}

	private void UpdateImageColor()
	{
		if (spriteRenderer != null)
		{
			setColour = spriteRenderer.color;
		}
		else if (image != null)
		{
			setColour = image.color;
		}
	}

	private void SetPaletteOnSpriteRenderer()
	{
		isPaletteSet = true;
		var palette = GetPaletteOrNull();
		if (palette != null && palette.Count > 0 && palette.Count <= 256)
		{
			MaterialPropertyBlock block = new MaterialPropertyBlock();
			spriteRenderer.GetPropertyBlock(block);
			List<Vector4> pal = palette.ConvertAll<Vector4>((Color c) => new Vector4(c.r, c.g, c.b, c.a));
			block.SetVectorArray("_ColorPalette", pal);
			block.SetInt("_IsPaletted", 1);
			block.SetInt("_PaletteSize", pal.Count);
			spriteRenderer.SetPropertyBlock(block);
		}
		else
		{
			MaterialPropertyBlock block = new MaterialPropertyBlock();
			spriteRenderer.GetPropertyBlock(block);
			block.SetInt("_IsPaletted", 0);
			spriteRenderer.SetPropertyBlock(block);
		}
	}

	private void SetPaletteOnImage()
	{
		List<Color> paletteOrNull = GetPaletteOrNull();

		if (paletteOrNull != null && palette.Count > 0 && palette.Count <= 256)
		{
			List<Vector4> pal = paletteOrNull.ConvertAll((c) => new Vector4(c.r, c.g, c.b, c.a));
			image.material.SetVectorArray("_ColorPalette", pal);
			image.material.SetInt("_IsPaletted", 1);
			image.material.SetInt("_PaletteSize", pal.Count);
		}
		else
		{
			image.material.SetInt("_IsPaletted", 0);
		}
	}

	private void SetImageSprite(Sprite value)
	{
		if (spriteRenderer != null)
		{
			spriteRenderer.enabled = true;
			spriteRenderer.sprite = value;

			if (isPaletteSet == false)
			{
				SetPaletteOnSpriteRenderer();
			}
		}
		else if (image != null)
		{
			image.sprite = value;

			if (isPaletteSet == false)
			{
				SetPaletteOnImage();
			}

			if (value == null)
			{
				image.enabled = false;
			}
			else
			{
				image.enabled = true;
			}
		}

		OnSpriteChanged?.Invoke(value);
	}

	private bool HasImageComponent()
	{
		if (spriteRenderer != null) return (true);
		if (image != null) return (true);
		return (false);
	}

	public bool HasSpriteInImageComponent()
	{
		if (initialised == false) TryInit();
		if (spriteRenderer != null)
		{
			if (spriteRenderer.sprite != null)
			{
				return true;
			}
		}

		if (image != null)
		{
			if (image.sprite != null || image.enabled)
			{
				return true;
			}
		}

		return (false);
	}

	private void GetImageComponent()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		image = GetComponent<Image>();
		if (image != null)
		{
			// unity doesn't support property blocks on ui renderers, so this is a workaround
			image.material = Instantiate(image.material);
		}
	}

	private void TryInit()
	{
		variantIndex = initialVariantIndex;
		GetImageComponent();
		bool Status = this.GetImageComponentStatus();
		ImageComponentStatus(false);
		initialised = true;

		if (randomInitialSprite && CatalogueCount > 0)
		{
			ChangeSprite(UnityEngine.Random.Range(0, CatalogueCount), NetworkThis);
		}
		else if (PresentSpriteSet != null)
		{
			if (HasImageComponent() && pushTextureOnStartUp)
			{
				PushTexture(false);
			}
		}

		ImageComponentStatus(Status);
	}

	private void ImageComponentStatus(bool newStatus)
	{
		if (spriteRenderer != null)
		{
			spriteRenderer.enabled = newStatus;
		}
		else if (image != null)
		{
			image.enabled = newStatus;
		}
	}

	private bool GetImageComponentStatus()
	{
		if (spriteRenderer != null)
		{
			return spriteRenderer.enabled;
		}
		else if (image != null)
		{
			return image.enabled;
		}

		return false;
	}

	private void OnEnable()
	{
		if (Application.isPlaying && NetworkThis)
		{
			networkIdentity = SpriteHandlerManager.GetRecursivelyANetworkBehaviour(this.gameObject);
			SpriteHandlerManager.RegisterHandler(this.networkIdentity, this);
		}

		GetImageComponent();
		OnSpriteChanged?.Invoke(CurrentSprite);
		if (Application.isPlaying)
		{
			PushTexture(false); // TODO: animations don't resume when sprite object is disabled and re-enabled, this is a workaround
		}
	}

	private void OnDisable()
	{
		TryToggleAnimationState(false);
		OnSpriteChanged?.Invoke(null);
	}

	private bool IsPaletted()
	{
		if (PresentSpriteSet == null)
		{
			return false;
		}

		return PresentSpriteSet.IsPalette;
	}

	private List<Color> GetPaletteOrNull()
	{
		if (IsPaletted() == false)
			return null;

		return palette;
	}

	public void UpdateMe()
	{
		timeElapsed += UpdateManager.CashedDeltaTime;
		if (timeElapsed >= PresentFrame.secondDelay)
		{
			if (variantIndex < PresentSpriteSet.Variance.Count)
			{
				animationIndex++;
				if (animationIndex >= PresentSpriteSet.Variance[variantIndex].Frames.Count)
				{
					animationIndex = 0;
					if (animateOnce)
					{
						InternalChangeSprite(CataloguePage + 1 < SubCatalogue.Count ? CataloguePage + 1 : 0, false);
						return;
					}
				}
				var frame = PresentSpriteSet.Variance[variantIndex].Frames[animationIndex];
				SetSprite(frame);
			}

		}

		if (isAnimation == false)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}
	}

	private void SetSprite(SpriteDataSO.Frame frame)
	{
		timeElapsed = 0;
		PresentFrame = frame;
		SetImageSprite(frame.sprite);
	}

	/// <summary>
	/// Gets the current sprite SO, if it exists.
	/// </summary>
	public SpriteDataSO GetCurrentSpriteSO()
	{
		return PresentSpriteSet;
	}

	/// <summary>
	/// Gets the sprite SO from the SO catalogue of the given index, if it exists.
	/// </summary>
	public SpriteDataSO GetSpriteSO(int index)
	{
		if (index < CatalogueCount)
		{
			return SubCatalogue[index];
		}

		return default;
	}

#if UNITY_EDITOR
	IEnumerator EditorAnimations()
	{
		yield return new Unity.EditorCoroutines.Editor.EditorWaitForSeconds(PresentFrame.secondDelay);
		UpdateMe();
		editorAnimating = null;
		if (isAnimation && !(this == null))
		{
			editorAnimating =
				Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutine(EditorAnimations(), this);
		}
	}

#endif

	private void TryToggleAnimationState(bool turnOn)
	{
#if UNITY_EDITOR
		if (EditorTryToggleAnimationState(turnOn))
		{
			return;
		}
#endif

		if (turnOn && isAnimation == false)
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			isAnimation = true;
		}
		else if (turnOn == false && isAnimation)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			animationIndex = 0;
			isAnimation = false;
		}
	}

#if UNITY_EDITOR
	private EditorCoroutine editorAnimating;

	private void OnValidate()
	{
		if (Application.isPlaying) return;


		if (PresentSpriteSet == null || isAnimation || this == null || this.gameObject == null)
		{
			return;
		}
		if (this.gameObject.scene.path != null && this.gameObject.scene.path.Contains("Scenes") == false &&
		    editorAnimating == null)
		{
			initialised = true;
			GetImageComponent();
			PushTexture();
		}
	}


	private bool EditorTryToggleAnimationState(bool turnOn)
	{
		if (Application.isEditor && Application.isPlaying == false)
		{
			if (turnOn && isAnimation == false)
			{
				if (this.gameObject.scene.path != null && this.gameObject.scene.path.Contains("Scenes") == false &&
				    editorAnimating == null)
				{
					Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutine(EditorAnimations(), this);
					isAnimation = true;
				}
				else
				{
					return true;
				}
			}
			else if (turnOn == false && isAnimation)
			{
				isAnimation = false;
			}

			return true;
		}

		return false;
	}
#endif
	[Serializable]
	public class SerialisationStanding
	{
		public Texture2D Texture;
	}
}