using UnityEngine;

///	<summary>
///	Handles sprite syncing between server and clients and contains a custom animator
///	</summary>
public class SpriteHandler : SpriteDataHandler
{
	public SpriteRenderer spriteRenderer;

	public int spriteIndex;

	public int VariantIndex;

	private SpriteJson spriteJson;
	private int animationIndex = 0;

	private float timeElapsed = 0;
	private float waitTime;
	private bool initialized = false;
	private bool isAnimation = false;

	public bool
		SynchroniseVariant =
			true; //Used for stuff like in hands where you dont want any delays / Miss match While it synchronises Requires manual synchronisation

	void OnDisable()
	{
		TryToggleAnimationState(false);
	}

	public void SetColor(Color value)
	{
		//color = value;
		spriteRenderer.color = value;
	}

	void TryInit()
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

			if (spriteIndex < Infos.List.Count)
			{
				if (VariantIndex < Infos.List[spriteIndex].Count)
				{
					if (animationIndex < Infos.List[spriteIndex][VariantIndex].Count)
					{
						SetSprite(Infos.List[spriteIndex][VariantIndex][animationIndex]);
						TryToggleAnimationState(Infos.List[spriteIndex][VariantIndex].Count > 1);
					}
					else
					{
						spriteRenderer.sprite = null;
						TryToggleAnimationState(false);
					}
				}
				else
				{
					spriteRenderer.sprite = null;
					TryToggleAnimationState(false);
				}
			}
			else
			{
				spriteRenderer.sprite = null;
				TryToggleAnimationState(false);
			}
		}
		else
		{
			spriteRenderer.sprite = null;
			TryToggleAnimationState(false);
		}
	}

	public void UpdateMe()
	{
		timeElapsed += Time.deltaTime;
		if (Infos.List.Count > spriteIndex)
		{
			if (timeElapsed >= waitTime)
			{
				animationIndex++;
				if (animationIndex >= Infos.List[spriteIndex][VariantIndex].Count)
				{
					animationIndex = 0;
				}

				SetSprite(Infos.List[spriteIndex][VariantIndex][animationIndex]);
			}
		}
	}

	void SetSprite(SpriteInfo animationStills)
	{
		timeElapsed = 0;
		waitTime = animationStills.waitTime;
		spriteRenderer.sprite = animationStills.sprite;
	}

	public int test;

	[ContextMenu("Test Change Sprite")]
	void ChangeIt()
	{
		ChangeSprite(test);
	}

	public void ChangeSprite(int newSprites)
	{
		if ((newSprites < Infos.List.Count))
		{
			if (spriteIndex != newSprites)
			{
				if ((VariantIndex < Infos.List[newSprites].Count))
				{
					spriteIndex = newSprites;
					animationIndex = 0;
					SetSprite(Infos.List[spriteIndex][VariantIndex][animationIndex]);
					TryToggleAnimationState(Infos.List[spriteIndex][VariantIndex].Count > 1);
				}
			}
		}
	}

	public void ChangeSpriteVariant(int SpriteVariant)
	{
		if ((spriteIndex < Infos.List.Count))
		{
			if ((SpriteVariant < Infos.List[spriteIndex].Count))
			{
				if (VariantIndex != SpriteVariant)
				{
					if (Infos.List[spriteIndex][SpriteVariant].Count <= animationIndex)
					{
						animationIndex = 0;
					}
					SetSprite(Infos.List[spriteIndex][SpriteVariant][animationIndex]);

					VariantIndex = SpriteVariant;
					TryToggleAnimationState(Infos.List[spriteIndex][VariantIndex].Count > 1);
				}
			}
		}
	}

	void TryToggleAnimationState(bool turnOn)
	{
		if (turnOn)
		{
			if (!isAnimation)
			{
				UpdateManager.Instance.Add(UpdateMe);
				isAnimation = true;
			}
		}
		else
		{
			if (isAnimation)
			{
				UpdateManager.Instance.Remove(UpdateMe);
				isAnimation = false;
			}
		}
	}

#if UNITY_EDITOR
	public override void SetUpSheet()
	{
		base.SetUpSheet();
	}
#endif
}