using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;

///	<summary>
///	Handles sprite syncing between server and clients and contains a custom animator
///	</summary>
public class SpriteHandler : SpriteHandlerData
{
	public SpriteRenderer spriteRenderer;

	[SyncVar(hook = nameof(SyncIndexSprite))]
	public int spriteIndex;

	[SyncVar(hook = nameof(SyncVariantIndex))]
	public int VariantIndex;

	private SpriteJson spriteJson;
	private int animationIndex = 0;

	private float timeElapsed = 0;
	private float waitTime;

	[SyncVar(hook = nameof(setSynchroniseVariant))]
	public bool SynchroniseVariant = true; //Used for stuff like in hands where you dont want any delays / Miss match While it synchronises Requires manual synchronisation

	public override void OnStartClient()
	{
		Start();
		setSynchroniseVariant(SynchroniseVariant);
		SyncIndexSprite(spriteIndex);
		SyncVariantIndex(VariantIndex);
	}

	void Start()
	{
		SpriteInfos.DeSerializeT();
		if (SpriteInfos.spriteList[spriteIndex][VariantIndex].Count > 1)
		{
			UpdateManager.Instance.Add(UpdateMe);
		}
	}

	public void PushTexture()
	{
		if (!(spriteIndex >= SpriteInfos.spriteList.Count))
		{
			if (!(VariantIndex >= SpriteInfos.spriteList[spriteIndex].Count))
			{
				SetSprite(SpriteInfos.spriteList[spriteIndex][VariantIndex][animationIndex]);
			}
			else {
				spriteRenderer.sprite = null;
			}
		}
		else {
			spriteRenderer.sprite = null;
		}
	}

	public void UpdateMe()
	{
		timeElapsed += Time.deltaTime;
		if (timeElapsed >= waitTime)
		{
			animationIndex++;
			if (animationIndex >= SpriteInfos.spriteList[spriteIndex][VariantIndex].Count)
			{
				animationIndex = 0;
			}
			SetSprite(SpriteInfos.spriteList[spriteIndex][VariantIndex][animationIndex]);
		}
	}

	public void SyncVariantIndex(int _VariantIndex)
	{

		if (SynchroniseVariant)
		{
			VariantIndex = _VariantIndex;
			animationIndex = 0;
			SetSprite(SpriteInfos.spriteList[spriteIndex][VariantIndex][animationIndex]);
			if (SpriteInfos.spriteList[spriteIndex][VariantIndex].Count > 1)
			{
				UpdateManager.Instance.Add(UpdateMe);
			}
			else {

				UpdateManager.Instance.Remove(UpdateMe);
			}
		}
	}

	public void setSynchroniseVariant(bool Sync)
	{
		SynchroniseVariant = Sync;
	}

	public void SyncIndexSprite(int _spriteIndex)
	{
		spriteIndex = _spriteIndex;
		animationIndex = 0;
		if (SpriteInfos.spriteList.Count > 0)
		{
			SetSprite(SpriteInfos.spriteList[spriteIndex][VariantIndex][animationIndex]);

			if (SpriteInfos.spriteList[spriteIndex][VariantIndex].Count > 1)
			{
				UpdateManager.Instance.Add(UpdateMe);
			}
			else {

				UpdateManager.Instance.Remove(UpdateMe);
			}
		}
	}

	void SetSprite(SpriteInfo animationStills)
	{
		timeElapsed = 0;
		waitTime = animationStills.waitTime;
		spriteRenderer.sprite = animationStills.sprite;
	}

	public void ChangeSprite(int newSprites)
	{
		if (!(newSprites >= SpriteInfos.spriteList.Count))
		{
			if (spriteIndex != newSprites)
			{
				spriteIndex = newSprites;
				animationIndex = 0;
				SetSprite(SpriteInfos.spriteList[spriteIndex][VariantIndex][animationIndex]);
				if (SpriteInfos.spriteList[spriteIndex][VariantIndex].Count > 1)
				{
					UpdateManager.Instance.Add(UpdateMe);
				}
				else {
					UpdateManager.Instance.Remove(UpdateMe);
				}
			}
		}
		else
		{

		}
	}

	public void ChangeSpriteVariant(int SpriteVariant)
	{
		//Logger.Log(spriteIndex + " < > " + SpriteInfos.spriteList.Count);
		//Logger.Log(SpriteVariant + " < > " + SpriteInfos.spriteList[spriteIndex].Count);
		if (!(spriteIndex >= SpriteInfos.spriteList.Count))
		{
			if (!(SpriteVariant >= SpriteInfos.spriteList[spriteIndex].Count))
			{
				SetSprite(SpriteInfos.spriteList[spriteIndex][VariantIndex][animationIndex]);
				if (VariantIndex != SpriteVariant)
				{
					animationIndex = 0;
					VariantIndex = SpriteVariant;
					if (SpriteInfos.spriteList[spriteIndex][VariantIndex].Count > 1)
					{
						UpdateManager.Instance.Add(UpdateMe);
					}
					else {
						UpdateManager.Instance.Remove(UpdateMe);
					}
				}
			}
		}
	}
}

