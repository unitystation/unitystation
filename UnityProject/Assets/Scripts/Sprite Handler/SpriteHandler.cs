using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using System.Linq;
using Newtonsoft.Json;
//using UnityEditor.Experimental.SceneManagement;
//using UnityEditor.SceneManagement;

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
		Infos.DeSerializeT();
		if (Infos.List[spriteIndex][VariantIndex].Count > 1)
		{
			UpdateManager.Instance.Add(UpdateMe);
		}
	}

	public void SetColor(Color value)
	{
		//color = value;
		spriteRenderer.color = value;
	}

	public void PushTexture()
	{
		//Logger.Log("PushTexture > " + this.name);
		//Logger.Log("1");
		if (Infos != null)
		{
			//Logger.Log("2");
			if (!(spriteIndex >= Infos.List.Count))
			{
				//Logger.Log("3");
				if (!(VariantIndex >= Infos.List[spriteIndex].Count))
				{
					//Logger.Log("4");
					//Logger.Log(Infos.List[spriteIndex][VariantIndex][animationIndex].sprite.name);
					SetSprite(Infos.List[spriteIndex][VariantIndex][animationIndex]);
				}
				else {
					spriteRenderer.sprite = null;
				}
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
		if (Infos.List.Count >= spriteIndex)
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
			if (!(Infos.List[spriteIndex][VariantIndex].Count > 1))
			{
				UpdateManager.Instance.Remove(UpdateMe);
			}
		}
		else { 
			UpdateManager.Instance.Remove(UpdateMe);
		}

	}

	public void SyncVariantIndex(int _VariantIndex)
	{

		if (SynchroniseVariant)
		{
			VariantIndex = _VariantIndex;
			animationIndex = 0;
			SetSprite(Infos.List[spriteIndex][VariantIndex][animationIndex]);
			if (Infos.List[spriteIndex][VariantIndex].Count > 1)
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
		if (Infos.List.Count > 0)
		{
			SetSprite(Infos.List[spriteIndex][VariantIndex][animationIndex]);

			if (Infos.List[spriteIndex][VariantIndex].Count > 1)
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
		//Logger.Log("Pushed");
		timeElapsed = 0;
		waitTime = animationStills.waitTime;
		spriteRenderer.sprite = animationStills.sprite;
	}

	public void ChangeSprite(int newSprites)
	{
		if (!(newSprites >= Infos.List.Count))
		{
			if (spriteIndex != newSprites)
			{
				if (!(VariantIndex >= Infos.List[newSprites].Count))
				{
					spriteIndex = newSprites;
					animationIndex = 0;
					SetSprite(Infos.List[spriteIndex][VariantIndex][animationIndex]);
					if (Infos.List[spriteIndex][VariantIndex].Count > 1)
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

	public void ChangeSpriteVariant(int SpriteVariant)
	{
		//Logger.Log("Updating " + SpriteVariant);
		//Logger.Log(spriteIndex + " < > " + Infos.List.Count);
		//Logger.Log(SpriteVariant + " < > " + Infos.List[spriteIndex].Count);
		if (!(spriteIndex >= Infos.List.Count))
		{
			if (!(SpriteVariant >= Infos.List[spriteIndex].Count))
			{
				//Logger.Log("Setting " + spriteIndex + " " +  SpriteVariant + " " + animationIndex);

				if (VariantIndex != SpriteVariant)
				{
					SetSprite(Infos.List[spriteIndex][SpriteVariant][animationIndex]);
					animationIndex = 0;
					VariantIndex = SpriteVariant;

					if (Infos.List[spriteIndex][VariantIndex].Count > 1)
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

