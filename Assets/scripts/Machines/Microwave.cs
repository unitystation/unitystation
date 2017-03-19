using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI;
using Events;
using Crafting;
using Network;

public class Microwave : Photon.PunBehaviour {

	public Sprite onSprite;
	public float cookTime = 10;

	private SpriteRenderer spriteRenderer;
	private Sprite offSprite;
	private AudioSource audioSource;

	public bool Cooking { get; private set; }
	private float cookingTime = 0;
	private GameObject mealPrefab = null;
	private string mealName;

	void Start ()
	{
		spriteRenderer = GetComponentInChildren<SpriteRenderer> ();
		audioSource = GetComponent<AudioSource> ();
		offSprite = spriteRenderer.sprite;
	}

	void Update ()
	{
		if (Cooking) {
			cookingTime += Time.deltaTime;

			if (cookingTime >= cookTime) {
				StopCooking ();
			}
		}
	}

	[PunRPC]
	void StartCookingRPC (string meal)
	{
		Cooking = true;
		cookingTime = 0;
		spriteRenderer.sprite = onSprite;
		mealName = meal;

	}

	private void StopCooking ()
	{
		Cooking = false;
		spriteRenderer.sprite = offSprite;
		audioSource.Play ();
		if (PhotonNetwork.connectedAndReady) {
			if (PhotonNetwork.isMasterClient) {
				NetworkItemDB.Instance.MasterClientCreateItem (mealName, transform.position, Quaternion.identity, 0, null);

			}
			mealName = null;
		}
	}
}
