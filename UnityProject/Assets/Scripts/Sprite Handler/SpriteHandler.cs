using UnityEngine;
using UnityEngine.Serialization;

///	<summary>
///	Handles sprite syncing between server and clients and contains a custom animator
///	</summary>
public class SpriteHandler : SpriteDataHandler
{
	[SerializeField]
	private SpriteRenderer spriteRenderer;

	[SerializeField]
	private int spriteIndex;

	[SerializeField] [FormerlySerializedAs("VariantIndex")]
	private int variantIndex;

	private SpriteJson spriteJson;

	private int animationIndex = 0;

	private float timeElapsed = 0;

	private float waitTime;

	private bool initialized = false;

	private bool isAnimation = false;

	[SerializeField]
	private int test;

	/// <summary>
	/// Used for stuff like in hands where you dont want any delays / Miss match While it synchronises Requires manual synchronisation
	/// </summary>
	[SerializeField]
	[FormerlySerializedAs("SynchroniseVariant")]
	private bool synchroniseVariant = true;

	private void OnDisable()
	{
		TryToggleAnimationState(false);
	}

	public void SetColor(Color value)
	{
		spriteRenderer.color = value;
	}

	private void TryInit()
	{
		if (!initialized)
		{
			Infos.DeSerializeT();
			initialized = true;
		}
	}

	public void PushClear()
	{
		spriteRenderer.sprite = null;
		TryToggleAnimationState(false);
	}

	public void PushTexture()
	{
		if (Infos != null)
		{
			if (!initialized)
			{
				TryInit();
			}

			if (spriteIndex < Infos.List[spriteIndex].Count &&
			    variantIndex < Infos.List[spriteIndex].Count &&
			    animationIndex < Infos.List[spriteIndex][variantIndex].Count)
			{
				SetSprite(Infos.List[spriteIndex][variantIndex][animationIndex]);
				TryToggleAnimationState(Infos.List[spriteIndex][variantIndex].Count > 1);
				return;
			}
		}
		spriteRenderer.sprite = null;
		TryToggleAnimationState(false);
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
		if (!isAnimation) {
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
}
