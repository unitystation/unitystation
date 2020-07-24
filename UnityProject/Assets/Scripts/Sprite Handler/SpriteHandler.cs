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

	private SpriteRenderer spriteRenderer;
	private Image image;

	private int animationIndex = 0;

	[Range(0, 3)] [SerializeField] private int variantIndex = 0;

	private int cataloguePage = -1;

	private float timeElapsed = 0;

	private bool isAnimation = false;

	private Color? setColour = null;

	[SerializeField] private List<Color> palette = new List<Color>();

	private bool Initialised;

	private NetworkIdentity NetworkIdentity;

	private bool isSubCatalogueChanged = false;

	[SerializeField]
	private List<SerialisationStanding> Sprites =new List<SerialisationStanding>();

	public NetworkIdentity GetMasterNetID()
	{
		return NetworkIdentity;
	}

	public void ChangeSprite(int SubCataloguePage, bool Network = true)
	{
		if (SubCataloguePage == cataloguePage) return;

		if ((SubCataloguePage >= SubCatalogue.Count))
		{
			Logger.LogError("new SubCataloguePage Is out of bounds on " + this.transform.parent.gameObject);
			return;
		}

		cataloguePage = SubCataloguePage;
		if (isSubCatalogueChanged)
		{
			SetSpriteSO(SubCatalogue[SubCataloguePage], Network: true);
		}
		else
		{
			SetSpriteSO(SubCatalogue[SubCataloguePage], Network: false);
			if (Network)
			{
				NetUpdate(NewCataloguePage: SubCataloguePage);
			}
		}
	}


	public void SetSpriteSO(SpriteDataSO NewspriteDataSO, Color? color = null, int NewvariantIndex = -1,
		bool Network = true)
	{
		if (NewspriteDataSO != PresentSpriteSet)
		{
			PresentSpriteSet = NewspriteDataSO;
			PushTexture(Network);
			if (Network)
			{
				NetUpdate(NewspriteDataSO);
			}
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

	public void SetColor(Color value, bool NetWork = true)
	{
		if (setColour == value) return;

		setColour = value;
		if (!HasImageComponent())
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

		if (HasSpriteInImageComponent() == false && PresentSpriteSet == null) return;

		PushClear(false);
		PresentSpriteSet = null;

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
		cataloguePage = JumpToPage;
		if (cataloguePage > -1)
		{
			ChangeSprite(JumpToPage, NetWork);
		}
	}

	public void SetPaletteOfCurrentSprite(List<Color> newPalette, bool Network = true)
	{
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

	private void NetUpdate(
		SpriteDataSO NewSpriteDataSO = null,
		int NewVariantIndex = -1,
		int NewCataloguePage = -1,
		bool NewPushTexture = false,
		bool NewEmpty = false,
		bool NewPushClear = false,
		bool NewClearPallet = false,
		Color? NewSetColour = null,
		List<Color> NewPalette = null)
	{
		if (NetworkThis == false) return;
		if (SpriteHandlerManager.Instance == null) return;
		if (NetworkIdentity == null)
		{
			NetworkIdentity = SpriteHandlerManager.GetRecursivelyANetworkBehaviour(this.gameObject)?.netIdentity;
			if (NetworkIdentity == null)
			{
				Logger.LogError("Was unable to find A NetworkBehaviour for " + gameObject.name,
					Category.SpriteHandler);
			}
		}

		if (NetworkIdentity.netId == 0)
		{
			//Logger.Log("ID hasn't been set for " + this.transform.parent);
			return;
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
				Logger.Log("NewSpriteDataSO NO ID!" + NewSpriteDataSO.name);
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

		SpriteHandlerManager.Instance.QueueChanges[this] = spriteChange;
	}


	void Awake()
	{
		GetImageComponent();
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
	}

	private void SetImageSprite(Sprite value)
	{
		if (spriteRenderer != null)
		{
			spriteRenderer.sprite = value;
			MaterialPropertyBlock block = new MaterialPropertyBlock();
			spriteRenderer.GetPropertyBlock(block);
			var palette = getPaletteOrNull();
			if (palette != null && palette.Count == 8)
			{
				List<Vector4> pal = palette.ConvertAll<Vector4>((Color c) => new Vector4(c.r, c.g, c.b, c.a));
				block.SetVectorArray("_ColorPalette", pal);
				block.SetInt("_IsPaletted", 1);
			}
			else
			{
				block.SetInt("_IsPaletted", 0);
			}

			spriteRenderer.SetPropertyBlock(block);
		}
		else if (image != null)
		{
			image.sprite = value;
			if (value == null)
			{
				image.enabled = false;
			}

		}
	}

	private bool HasImageComponent()
	{
		if (spriteRenderer != null) return (true);
		if (image != null) return (true);
		return (false);
	}

	private bool HasSpriteInImageComponent()
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
			if (image.sprite != null)
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
	}

	private void TryInit()
	{
		GetImageComponent();
		ImageComponentStatus(false);
		Initialised = true;
		if (PresentSpriteSet != null)
		{
			if (HasImageComponent())
			{
				PushTexture(false);
			}
		}

		ImageComponentStatus(true);
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

	private void OnEnable()
	{
		if (Application.isPlaying && NetworkThis)
		{
			NetworkIdentity = SpriteHandlerManager.GetRecursivelyANetworkBehaviour(this.gameObject)?.netIdentity;
			SpriteHandlerManager.RegisterHandler(this.NetworkIdentity, this);
		}

		GetImageComponent();
	}

	private void OnDisable()
	{
		TryToggleAnimationState(false);
	}

	private bool isPaletted()
	{
		if (PresentSpriteSet == null) return false;

		return PresentSpriteSet.IsPalette;
	}

	private List<Color> getPaletteOrNull()
	{
		if (!isPaletted())
			return null;

		return palette;
	}

	public void UpdateMe()
	{
		timeElapsed += Time.deltaTime;
		if (PresentSpriteSet.Variance.Count > variantIndex &&
		    timeElapsed >= PresentFrame.secondDelay)
		{
			animationIndex++;
			if (animationIndex >= PresentSpriteSet.Variance[variantIndex].Frames.Count)
			{
				animationIndex = 0;
			}

			var frame = PresentSpriteSet.Variance[variantIndex].Frames[animationIndex];
			SetSprite(frame);
		}

		if (!isAnimation)
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

		if (turnOn && !isAnimation)
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			isAnimation = true;
		}
		else if (!turnOn && isAnimation)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
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
		if (Application.isEditor && !Application.isPlaying)
		{
			if (turnOn && !isAnimation)
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
			else if (!turnOn && isAnimation)
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