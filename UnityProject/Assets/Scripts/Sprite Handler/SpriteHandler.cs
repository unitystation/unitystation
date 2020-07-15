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

	[SerializeField] private SpriteDataSO PresentSpriteSet = null;
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

	public void ChangeSprite(int SubCataloguePage, bool Network = true)
	{
		if (SubCataloguePage == cataloguePage) return;

		if ((SubCataloguePage > SubCatalogue.Count ))
		{
			Logger.LogError("new SubCataloguePage Is out of bounds on " + this);
			return;
		}

		cataloguePage = SubCataloguePage;
		SetSpriteSO(SubCatalogue[SubCataloguePage], Network: Network);

		if (Network)
		{
			NetUpdate(NewCataloguePage : SubCataloguePage);
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
			SetColor(color.GetValueOrDefault(Color.white),Network);
		}

		if (NewvariantIndex > -1)
		{
			ChangeSpriteVariant(NewvariantIndex,Network);
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
			NetUpdate(NewSetColour : value);
		}
	}

	public void Empty(bool Network = true)
	{
		if (HasSpriteInImageComponent() == false && SubCatalogue.Count == 0 && PresentSpriteSet == null) return;

		PushClear(false);
		PresentSpriteSet = null;
		SubCatalogue = new List<SpriteDataSO>();
		if (Network)
		{
			NetUpdate(NewEmpty : true);
		}
	}

	public void PushClear(bool Network = true)
	{
		if (HasSpriteInImageComponent() == false) return;

		SetImageSprite(null);
		TryToggleAnimationState(false);
		if (Network)
		{
			NetUpdate( NewPushClear : true );
		}
	}


	public void SetCatalogue(List<SpriteDataSO> NewCatalogue, int JumpToPage = -1, bool NetWork = true)
	{
		Logger.LogError("Not networked yet SetCatalogue");
		SubCatalogue = NewCatalogue;
		cataloguePage = JumpToPage;
		if (cataloguePage > -1)
		{
			ChangeSprite(JumpToPage, false);
		}

		if (NetWork)
		{
			//NetUpdate();
		}
	}

	public void SetPaletteOfCurrentSprite(List<Color> newPalette)
	{
		if (isPaletted())
		{
			palette = newPalette;
			PushTexture();
		}

		Logger.LogError("Not networked yet SetPaletteOfCurrentSprite");
	}

	public void PushTexture(bool NetWork = true)
	{
		if (Initialised == false) return;
		if (PresentSpriteSet != null && PresentSpriteSet.Variance.Count > 0)
		{
			if (variantIndex < PresentSpriteSet.Variance.Count &&
			    animationIndex < PresentSpriteSet.Variance[variantIndex].Frames.Count)
			{
				var Frame = PresentSpriteSet.Variance[variantIndex].Frames[animationIndex];

				SetSprite(Frame);

				TryToggleAnimationState(PresentSpriteSet.Variance[variantIndex].Frames.Count > 1);
				if (NetWork)
				{
					NetUpdate( NewPushTexture : true );
					//NetWork this a poke Basically
				}

				return;
			}
		}


		if (NetWork && HasSpriteInImageComponent())
		{
			NetUpdate( NewPushTexture : true );
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
		Color? NewSetColour = null)
	{

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
			spriteChange.PushTexture = NewPushTexture;
		}

		if (NewEmpty)
		{
			spriteChange.Empty = NewEmpty;
		}

		if (NewPushClear)
		{
			spriteChange.PushClear = NewPushClear;
		}

		if (NewSetColour != null)
		{
			spriteChange.SetColour = NewSetColour;
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
			if (palette != null)
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
		ImageComponentStatus(false);
		Initialised = true;
		if (PresentSpriteSet != null)
		{
			if (HasImageComponent())
			{
				PushTexture();
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
		if (Application.isPlaying)
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

	public void SetColor(Color value)
	{
		if (!HasImageComponent())
		{
			GetImageComponent();
		}

		SetImageColor(value);
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

		Initialised = true;
		GetImageComponent();
		PushTexture();
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
}