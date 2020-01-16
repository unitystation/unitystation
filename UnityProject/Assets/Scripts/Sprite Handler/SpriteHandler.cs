using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;


///	<summary>
///	Handles sprite syncing between server and clients and contains a custom animator
///	</summary>
public class SpriteHandler : SpriteDataHandler
{
	//[SerializeField]
	private SpriteRenderer spriteRenderer;

	[SerializeField]
	private int spriteIndex;

	[SerializeField]
	[FormerlySerializedAs("VariantIndex")]
	private int variantIndex;

	private int animationIndex = 0;

	private float timeElapsed = 0;

	private float waitTime;

	private bool isAnimation = false;

	[SerializeField]
	private int test;

	[SerializeField]
	private bool SetSpriteOnStartUp = true;


	private bool Initialised;

	private IEnumerator WaitForInitialisation()
	{
		yield return WaitFor.EndOfFrame;
		Initialised = true;
		spriteRenderer = this.GetComponent<SpriteRenderer>();
		if (spriteRenderer != null && SetSpriteOnStartUp)
		{
			//Logger.LogError("GO!2 " + transform.parent.name);
			PushTexture();
		}
	}

	void Start()
	{
		AddSprites();
		spriteRenderer = this.GetComponent<SpriteRenderer>();
		//Logger.LogError("GO!1 " + transform.parent.name);
		StartCoroutine(WaitForInitialisation());

	}

	private void OnDisable()
	{
		TryToggleAnimationState(false);
	}

	public void SetColor(Color value)
	{
		if (spriteRenderer == null) { 
			spriteRenderer = this.GetComponent<SpriteRenderer>();
		}
		spriteRenderer.color = value;
	}

	public void PushClear()
	{
		spriteRenderer.sprite = null;
		TryToggleAnimationState(false);
	}

	public void PushTexture()
	{
		if (Initialised)
		{
			if (Infos != null)
			{
				if (spriteIndex < Infos.List.Count &&
					variantIndex < Infos.List[spriteIndex].Count &&
					animationIndex < Infos.List[spriteIndex][variantIndex].Count)
				{
					SetSprite(Infos.List[spriteIndex][variantIndex][animationIndex]);
					TryToggleAnimationState(Infos.List[spriteIndex][variantIndex].Count > 1);
					return;
				}
				else if (spriteIndex < Infos.List.Count &&
						 variantIndex < Infos.List[spriteIndex].Count)
				{
					animationIndex = 0;
					SetSprite(Infos.List[spriteIndex][variantIndex][animationIndex]);
					TryToggleAnimationState(Infos.List[spriteIndex][variantIndex].Count > 1);
					return;
				}
			}
			spriteRenderer.sprite = null;
			TryToggleAnimationState(false);
		}
	}

	public void UpdateMe()
	{

		timeElapsed += Time.deltaTime;
		if (Infos.List.Count > spriteIndex &&
			timeElapsed >= waitTime)
		{
			animationIndex++;
			if (animationIndex >= Infos.List[spriteIndex][variantIndex].Count)
			{
				animationIndex = 0;
			}
			SetSprite(Infos.List[spriteIndex][variantIndex][animationIndex]);
		}
		if (!isAnimation)
		{
			UpdateManager.Instance.Remove(UpdateMe);
			spriteRenderer.sprite = null;
		}
	}

	private void SetSprite(SpriteInfo animationStills)
	{
		timeElapsed = 0;
		waitTime = animationStills.waitTime;
		spriteRenderer.sprite = animationStills.sprite;
	}

	[ContextMenu("Test Change Sprite")]
	private void ChangeIt()
	{
		ChangeSprite(test);
	}

	public void ChangeSprite(int newSprites)
	{
		if (newSprites < Infos.List.Count &&
			spriteIndex != newSprites &&
			variantIndex < Infos.List[newSprites].Count)
		{
			spriteIndex = newSprites;
			animationIndex = 0;
			SetSprite(Infos.List[spriteIndex][variantIndex][animationIndex]);
			TryToggleAnimationState(Infos.List[spriteIndex][variantIndex].Count > 1);
		}
	}

	public void ChangeSpriteVariant(int spriteVariant)
	{
		if (spriteIndex < Infos.List.Count &&
			spriteVariant < Infos.List[spriteIndex].Count &&
			variantIndex != spriteVariant)
		{
			if (Infos.List[spriteIndex][spriteVariant].Count <= animationIndex)
			{
				animationIndex = 0;
			}
			SetSprite(Infos.List[spriteIndex][spriteVariant][animationIndex]);
			variantIndex = spriteVariant;
			TryToggleAnimationState(Infos.List[spriteIndex][variantIndex].Count > 1);
		}
	}

	private void TryToggleAnimationState(bool turnOn)
	{
		//UpdateManager.Instance.Remove(UpdateMe);
		if (turnOn && !isAnimation)
		{
			UpdateManager.Instance.Add(UpdateMe);
			isAnimation = true;
		}
		else if (!turnOn && isAnimation)
		{
			UpdateManager.Instance.Remove(UpdateMe);
			isAnimation = false;
		}
	}

	/// <summary>
	/// Used to Set sprite handlers internal buffer to the single Texture specified and set Sprite
	/// </summary>
	/// <param name="_SpriteSheetAndData">specified Texture.</param>
	/// <param name="_variantIndex">Variant index.</param>
	public void SetSprite(SpriteSheetAndData _SpriteSheetAndData, int _variantIndex = 0)
	{
		Infos.List.Clear();
		Infos.List.Add(StaticSpriteHandler.CompleteSpriteSetup(_SpriteSheetAndData));
		variantIndex = _variantIndex;
		PushTexture();
	}

	/// <summary>
	/// Used to Set sprite handlers internal buffer to To a different internal buffer
	/// </summary>
	/// <param name="_Info">internal buffer.</param>
	/// <param name="_spriteIndex">Sprite index.</param>
	/// <param name="_variantIndex">Variant index.</param>
	public void SetInfo(SpriteData _Info, int _spriteIndex = 0, int _variantIndex = 0)
	{
		spriteIndex = _spriteIndex;
		variantIndex = _variantIndex;
		Infos = _Info;
		if (Initialised)
		{
			PushTexture();
		}
	}

}
