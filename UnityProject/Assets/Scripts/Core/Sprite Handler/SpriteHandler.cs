using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEditor;
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
	[SerializeField] private bool NetworkThis = true;

	[SerializeField] private List<SpriteDataSO> SubCatalogue = new List<SpriteDataSO>();

	[SerializeField] private SpriteDataSO PresentSpriteSet;
	private SpriteDataSO.Frame PresentFrame = null;

	[Tooltip("If checked, a random sprite SO will be selected during initialization from the catalogue of sprite SOs.")]
	[SerializeField] private bool randomInitialSprite = false;

	private SpriteRenderer spriteRenderer;
	private Image image;

	private int animationIndex = 0;

	[SerializeField]
	private bool pushTextureOnStartUp = true;

	[Range(0, 3)] [SerializeField] private int variantIndex = 0;

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

	private bool Initialised = false;

	private NetworkIdentity NetworkIdentity;

	private bool isSubCatalogueChanged = false;

	[SerializeField]
	private List<SerialisationStanding> Sprites =new List<SerialisationStanding>();

	/// <summary>
	/// The catalogue index representing the current sprite SO.
	/// </summary>
	public int CataloguePage => cataloguePage;

	/// <summary>
	/// Invokes when sprite just changed by animation or other script
	/// Null if sprite became hidden
	/// </summary>
	public event System.Action<Sprite> OnSpriteChanged;

	/// <summary>
	/// Invokes when sprite data scriptable object is changed
	/// Null if sprite became hidden
	/// </summary>
	public event System.Action<SpriteDataSO> OnSpriteDataSOChanged;

	/// <summary>
	/// Invoke when sprite handler has changed color of sprite
	/// </summary>
	public event System.Action<Color> OnColorChanged;

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
	public bool IsHiden
	{
		get
		{
			return CurrentSprite == null || gameObject.activeInHierarchy == false;
		}
	}

	public NetworkIdentity GetMasterNetID()
	{
		return NetworkIdentity;
	}

	public void ChangeSprite(int SubCataloguePage, bool Network = true)
	{
		InternalChangeSprite(SubCataloguePage, Network);
	}

	public void AnimateOnce(int SubCataloguePage, bool Network = true)
	{
		InternalChangeSprite(SubCataloguePage, Network, true);
	}


	private void InternalChangeSprite(int SubCataloguePage, bool Network = true, bool AnimateOnce = false)
	{
		if (cataloguePage > -1 && SubCataloguePage == cataloguePage) return;

		if (SubCataloguePage >= SubCatalogue.Count)
		{
			Logger.LogError("new SubCataloguePage Is out of bounds on " + this.transform.parent.gameObject);
			return;
		}

		cataloguePage = SubCataloguePage;
		if (isSubCatalogueChanged)
		{
			SetSpriteSO(SubCatalogue[SubCataloguePage]);
		}
		else
		{
			SetSpriteSO(SubCatalogue[SubCataloguePage], Network: false);
			if (Network)
			{
				NetUpdate(NewCataloguePage: SubCataloguePage, NewAnimateOnce: AnimateOnce);
			}
		}

		animateOnce = AnimateOnce;
	}

	public void SetSpriteSO(SpriteDataSO NewspriteDataSO, Color? color = null, int NewvariantIndex = -1,
		bool Network = true)
	{
		if (NewspriteDataSO != PresentSpriteSet)
		{
			isPaletteSet = false;
			PresentSpriteSet = NewspriteDataSO;
			// TODO: Network, change to network catalogue message
			// See https://github.com/unitystation/unitystation/pull/5675#pullrequestreview-540239428
			cataloguePage = SubCatalogue.FindIndex(SO => SO == NewspriteDataSO);
			PushTexture(Network);
			if (Network)
			{
				NetUpdate(NewspriteDataSO);
			}
			OnSpriteDataSOChanged?.Invoke(NewspriteDataSO);
		}

		if (color != null)
		{
			SetColor(color.GetValueOrDefault(Color.white), Network);
		}

		if (NewvariantIndex > -1)
		{
			ChangeSpriteVariant(NewvariantIndex, Network);
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

	public void ChangeSpriteVariant(int spriteVariant, bool NetWork = true)
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
				if (NetWork)
				{
					NetUpdate(NewVariantIndex: spriteVariant);
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

	public void SetColor(Color value, bool NetWork = true)
	{
		if (Initialised == false) TryInit();
		if (setColour == value) return;
		setColour = value;
		if (HasImageComponent() == false)
		{
			GetImageComponent();
		}

		SetImageColor(value);
		if (NetWork)
		{
			NetUpdate(NewSetColour: value);
		}
	}

	public void ClearPallet(bool Network = true)
	{
		if (palette == null) return;
		palette = null;
		isPaletteSet = false;

		if (Network)
		{
			NetUpdate(NewClearPallet: true);
		}
	}

	public void Empty(bool ClearSubCatalogue = false, bool Network = true)
	{
		if (ClearSubCatalogue)
		{
			SubCatalogue = new List<SpriteDataSO>();
		}

		if (HasSpriteInImageComponent() == false && PresentSpriteSet == null && cataloguePage == -1) return;
		cataloguePage = -1;
		PushClear(false);
		PresentSpriteSet = null;
		OnSpriteDataSOChanged?.Invoke(null);

		if (Network)
		{
			NetUpdate(NewEmpty: true);
		}
	}

	public void PushClear(bool Network = true)
	{
		if (Initialised == false) TryInit();
		if (HasSpriteInImageComponent() == false) return;

		SetImageSprite(null);
		TryToggleAnimationState(false);
		if (Network)
		{
			NetUpdate(NewPushClear: true);
		}
	}


	/// <summary>
	/// Sets the sprite catalogue for server side only, Any calls to ChangeSprite Will automatically be networked In a different way
	/// </summary>
	/// <param name="NewCatalogue"></param>
	/// <param name="JumpToPage"></param>
	/// <param name="NetWork"></param>
	public void SetCatalogue(List<SpriteDataSO> NewCatalogue, int JumpToPage = -1, bool NetWork = true)
	{
		isSubCatalogueChanged = true;
		SubCatalogue = NewCatalogue;
		if (JumpToPage > -1)
		{
			ChangeSprite(JumpToPage, NetWork);
		}
	}

	public void SetPaletteOfCurrentSprite(List<Color> newPalette, bool Network = true)
	{
		bool paletted = isPaletted();

		Debug.Assert((paletted && newPalette == null) == false, "Paletted sprites should never have palette set to null");

		if (paletted == false)
		{
			newPalette = null;
		}

		isPaletteSet = false;
		palette = newPalette;
		PushTexture(false);
		if (Network)
		{
			NetUpdate(NewPalette: palette);
		}
	}

	public void PushTexture(bool NetWork = true)
	{
		if (Initialised == false) TryInit();
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
				if (NetWork)
				{
					NetUpdate(NewPushTexture: true);
					//NetWork this a poke Basically
				}

				return;
			}
		}


		if (NetWork && HasSpriteInImageComponent())
		{
			NetUpdate(NewPushTexture: true);
		}

		SetImageSprite(null);
		TryToggleAnimationState(false);
	}

	/// <summary>
	/// Toggles the SpriteRenderer texture. Calls PushTexture() if the new state is on, or PushClear() otherwise.
	/// </summary>
	/// <param name="newState">If on, sets the texture (to last known). If off, clears the texture.</param>
	/// <param name="network">Will send update to clients if true (default).</param>
	public void ToggleTexture(bool newState, bool network = true)
	{
		if (newState)
		{
			PushTexture(network);
		}
		else
		{
			PushClear(network);
		}
	}

	private void NetUpdate(
		SpriteDataSO NewSpriteDataSO = null,
		int NewVariantIndex = -1,
		int NewCataloguePage = -1,
		bool NewPushTexture = false,
		bool NewEmpty = false,
		bool NewPushClear = false,
		bool NewClearPallet = false,
		Color? NewSetColour = null,
		List<Color> NewPalette = null,
		bool NewAnimateOnce = false)
	{
		if (NetworkThis == false) return;
		if (SpriteHandlerManager.Instance == null) return;
		if (NetworkIdentity == null)
		{
			if (this?.gameObject == null) return;
			var NetID = SpriteHandlerManager.GetRecursivelyANetworkBehaviour(this.gameObject);
			if (NetID == null)
			{
				Logger.LogError("Was unable to find A NetworkBehaviour for ",
					Category.Sprites);
				return;
			}

			NetworkIdentity = NetID;
			if (NetworkIdentity == null)
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

		if (NewSpriteDataSO != null)
		{
			if (NewSpriteDataSO.setID == -1)
			{
				Logger.Log("NewSpriteDataSO NO ID!" + NewSpriteDataSO.name, Category.Sprites);
			}
			if (spriteChange.Empty) spriteChange.Empty = false;
			spriteChange.PresentSpriteSet = NewSpriteDataSO.setID;
		}

		if (NewVariantIndex != -1)
		{
			spriteChange.VariantIndex = NewVariantIndex;
		}

		if (NewCataloguePage != -1)
		{
			spriteChange.CataloguePage = NewCataloguePage;
		}

		if (NewPushTexture)
		{
			if (spriteChange.PushClear) spriteChange.PushClear = false;
			spriteChange.PushTexture = NewPushTexture;
		}

		if (NewEmpty)
		{
			if (spriteChange.PresentSpriteSet != -1) spriteChange.PresentSpriteSet = -1;
			spriteChange.Empty = NewEmpty;
		}

		if (NewPushClear)
		{
			if (spriteChange.PushTexture) spriteChange.PushTexture = false;
			spriteChange.PushClear = NewPushClear;
		}

		if (NewAnimateOnce)
		{
			if (spriteChange.AnimateOnce) spriteChange.AnimateOnce = false;
			spriteChange.AnimateOnce = NewAnimateOnce;
		}

		if (NewClearPallet)
		{
			if (spriteChange.Pallet != null) spriteChange.Pallet = null;
			spriteChange.ClearPallet = NewClearPallet;
		}

		if (NewSetColour != null)
		{
			spriteChange.SetColour = NewSetColour;
		}

		if (NewPalette != null)
		{
			if (spriteChange.ClearPallet) spriteChange.ClearPallet = false;
			spriteChange.Pallet = NewPalette;
		}

		if (NetworkIdentity.netId == 0)
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
		if (NetworkIdentity.netId == 0)
		{
			Logger.LogError("ID hasn't been set for " + this.transform.parent, Category.Sprites);
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
		TryInit();
	}

	void Start()
	{
		TryInit();
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
		var palette = getPaletteOrNull();
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
		List<Color> paletteOrNull = getPaletteOrNull();

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
		if (Initialised == false) TryInit();
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
		GetImageComponent();
		bool Status = this.GetImageComponentStatus();
		ImageComponentStatus(false);
		Initialised = true;

		if (randomInitialSprite && CatalogueCount > 0)
		{
			ChangeSprite(Random.Range(0, CatalogueCount), NetworkThis);
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

	private void ImageComponentStatus(bool Status)
	{
		if (spriteRenderer != null)
		{
			spriteRenderer.enabled = Status;
		}
		else if (image != null)
		{
			image.enabled = Status;
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
			NetworkIdentity = SpriteHandlerManager.GetRecursivelyANetworkBehaviour(this.gameObject);
			SpriteHandlerManager.RegisterHandler(this.NetworkIdentity, this);
		}

		GetImageComponent();
		OnSpriteChanged?.Invoke(CurrentSprite);

		PushTexture(false); // TODO: animations don't resume when sprite object is disabled and re-enabled, this is a workaround
	}

	private void OnDisable()
	{
		TryToggleAnimationState(false);
		OnSpriteChanged?.Invoke(null);
	}

	private bool isPaletted()
	{
		if (PresentSpriteSet == null)
		{
			return false;
		}

		return PresentSpriteSet.IsPalette;
	}

	private List<Color> getPaletteOrNull()
	{
		if (isPaletted() == false)
			return null;

		return palette;
	}

	public void UpdateMe()
	{
		timeElapsed += UpdateManager.CashedDeltaTime;
		if (timeElapsed >= PresentFrame.secondDelay)
		{
			if (PresentSpriteSet.Variance.Count > variantIndex)
			{
				animationIndex++;
				if (animationIndex >= PresentSpriteSet.Variance[variantIndex].Frames.Count)
				{
					animationIndex = 0;
					if (animateOnce)
					{
						if (CustomNetworkManager.IsServer)
						{
							ChangeSprite(SubCatalogue.Count - 1 >= CataloguePage + 1 ? CataloguePage + 1 : 0);
						}

						isAnimation = false;
						UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
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

	private void SetSprite(SpriteDataSO.Frame Frame)
	{
		timeElapsed = 0;
		PresentFrame = Frame;
		SetImageSprite(Frame.sprite);
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
		EditorAnimating = null;
		if (isAnimation && !(this == null))
		{
			EditorAnimating =
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
	private EditorCoroutine EditorAnimating;

	private void OnValidate()
	{
		if (Application.isPlaying) return;


		if (PresentSpriteSet == null || isAnimation || this == null || this.gameObject == null)
		{
			return;
		}
		if (this.gameObject.scene.path != null && this.gameObject.scene.path.Contains("Scenes") == false &&
		    EditorAnimating == null)
		{
			Initialised = true;
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
				    EditorAnimating == null)
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
	[System.Serializable]
	public class SerialisationStanding
	{
		public Texture2D Texture;
	}
}