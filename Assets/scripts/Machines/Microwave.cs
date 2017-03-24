using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UI;
using Events;
using Crafting;
using Network;

public class Microwave : NetworkBehaviour
{

	public Sprite onSprite;
	public float cookTime = 10;

	private SpriteRenderer spriteRenderer;
	private Sprite offSprite;
	private AudioSource audioSource;

	public bool Cooking { get; private set; }

	private float cookingTime = 0;
	private GameObject mealPrefab = null;
	private string mealName;

	private NetworkIdentity networkIdentity;

	void Start()
	{
		networkIdentity = GetComponent<NetworkIdentity>();
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		audioSource = GetComponent<AudioSource>();
		offSprite = spriteRenderer.sprite;
	}

	void Update()
	{
		if (Cooking) {
			cookingTime += Time.deltaTime;

			if (cookingTime >= cookTime) {
				StopCooking();
			}
		}
	}

	[Command]
	public void CmdStartCooking(string meal)
	{
		RpcStartCooking(meal);
	}

	[ClientRpc]
	void RpcStartCooking(string meal)
	{
		Cooking = true;
		cookingTime = 0;
		spriteRenderer.sprite = onSprite;
		mealName = meal;
	}

	private void StopCooking()
	{
		Cooking = false;
		spriteRenderer.sprite = offSprite;
		audioSource.Play();
		if (isServer) {
			Debug.Log("FIXME: Needs the actual prefab object instead of a string");
//			NetworkItemDB.Instance.CmdInstantiateItem (mealName, transform.position, Quaternion.identity);

		}
		mealName = null;
	}
}
