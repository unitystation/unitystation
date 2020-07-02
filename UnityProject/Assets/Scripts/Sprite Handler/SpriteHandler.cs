using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
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
	public SpriteDataSO spriteDataSO = null;
	private SpriteDataSO.Frame PresentFrame = null;

	private SpriteRenderer spriteRenderer;
	private Image image;

	private int animationIndex = 0;

	[SerializeField]
	private int variantIndex = 0;

	private float timeElapsed = 0;

	private bool isAnimation = false;

	public List<Color> palette = new List<Color>();

	private bool Initialised;

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

		//network it
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

	private void GetImageComponent()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		image = GetComponent<Image>();
	}

	private void TryInit()
	{
		ImageComponentStatus(false);
		Initialised = true;
		if (spriteDataSO != null)
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
			SpriteHandlerManager.RegisterHandler(
				this.transform.parent.GetComponent<NetworkBehaviour>()?.netIdentity, this);
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

	/// <summary>
	/// Sets the sprite to null and stops any animations.
	/// </summary>
	public void PushClear()
	{
		SetImageSprite(null);
		TryToggleAnimationState(false);
	}

	private bool isPaletted()
	{
		if (spriteDataSO == null || spriteDataSO.Variance[variantIndex].Frames == null)
			return false;
		return spriteDataSO.IsPalette;
	}

	private List<Color> getPaletteOrNull()
	{
		if (!isPaletted())
			return null;

		return palette;
	}

	public void SetPaletteOfCurrentSprite(List<Color> newPalette)
	{
		if (isPaletted())
		{
			palette = newPalette;
			PushTexture();
		}
	}

	public void PushTexture()
	{
		if (Initialised)
		{
			if (spriteDataSO != null && spriteDataSO.Variance.Count > 0)
			{
				if (variantIndex < spriteDataSO.Variance.Count &&
				    animationIndex < spriteDataSO.Variance[variantIndex].Frames.Count)
				{
					var Frame = spriteDataSO.Variance[variantIndex].Frames[animationIndex];

					SetSprite(Frame);

					TryToggleAnimationState(spriteDataSO.Variance[variantIndex].Frames.Count > 1);
					return;
				}
			}

			SetImageSprite(null);
			TryToggleAnimationState(false);
		}
	}

	public void UpdateMe()
	{
		timeElapsed += Time.deltaTime;
		if (spriteDataSO.Variance.Count > variantIndex &&
		    timeElapsed >= PresentFrame.secondDelay)
		{
			animationIndex++;
			if (animationIndex >= spriteDataSO.Variance[variantIndex].Frames.Count)
			{
				animationIndex = 0;
			}

			var frame = spriteDataSO.Variance[variantIndex].Frames[animationIndex];
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

	public void ChangeSprite(int newSprites)
	{
		//net work
	}

	public void ChangeSpriteVariant(int spriteVariant)
	{
		if (spriteDataSO != null)
		{
			if (spriteVariant < spriteDataSO.Variance.Count &&
			    variantIndex != spriteVariant)
			{
				if (spriteDataSO.Variance[spriteVariant].Frames.Count <= animationIndex)
				{
					animationIndex = 0;
				}

				variantIndex = spriteVariant;

				var Frame = spriteDataSO.Variance[variantIndex].Frames[animationIndex];
				SetSprite(Frame);

				TryToggleAnimationState(spriteDataSO.Variance[variantIndex].Frames.Count > 1);
			}
		}
	}

#if UNITY_EDITOR
	IEnumerator EditorAnimations()
	{
		yield return new Unity.EditorCoroutines.Editor.EditorWaitForSeconds(PresentFrame.secondDelay);
		UpdateMe();
		EditorAnimating = null;
		if (isAnimation && !(this == null))
		{
			EditorAnimating = Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutine(EditorAnimations(), this);
		}
	}

#endif

	private void TryToggleAnimationState(bool turnOn)
	{
#if UNITY_EDITOR
		if (Application.isEditor && !Application.isPlaying)
		{
			if (turnOn && !isAnimation)
			{
				if ( this.gameObject.scene.path != null &&  this.gameObject.scene.path.Contains("Scenes") == false && EditorAnimating == null)
				{
					Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutine(EditorAnimations(), this);
					isAnimation = true;
				}
				else
				{
					return;
				}

			}
			else if (!turnOn && isAnimation)
			{
				isAnimation = false;
			}
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

	/// <summary>
	/// Used to set a singular sprite NOTE: This will not be networked
	/// </summary>
	/// <param name="_sprite">Sprite.</param>
	public void SetSprite(Sprite _sprite)
	{
		SetImageSprite(_sprite);
		TryToggleAnimationState(false);
	}


	public void SetSprite(SpriteDataSO _spriteDataSO, int _variantIndex = 0)
	{
		spriteDataSO = _spriteDataSO;
		variantIndex = _variantIndex;
		if (Initialised)
		{
			PushTexture();
		}
	}

#if UNITY_EDITOR
	private EditorCoroutine EditorAnimating;
	private void OnValidate()
	{
		if (Application.isPlaying) return;

		if (spriteDataSO == null || isAnimation || this == null || this.gameObject == null)
		{
			return;
		}
		Initialised = true;
		GetImageComponent();
		PushTexture();
	}
#endif
}


