using System.Collections;
using InputControl;
using Sprites;
using UnityEngine;

namespace Lighting
{
	internal enum LightState
	{
		On,
		Off,
		Broken
	}

	public class LightSource : ObjectTrigger
	{
		private const int MAX_TARGETS = 400;

		private readonly Collider2D[] lightSpriteColliders = new Collider2D[MAX_TARGETS];

		private int ambientMask;

		/// <summary>
		///     The actual light effect that the light source represents
		/// </summary>
		public GameObject Light;

		/// <summary>
		///     The state of this light
		/// </summary>
		private LightState LightState;

		private int obstacleMask;
		public float radius = 6f;

		/// <summary>
		///     The SpriteRenderer for this light
		/// </summary>
		private SpriteRenderer Renderer;

		/// <summary>
		///     The sprite to show when this light is turned off
		/// </summary>
		public Sprite SpriteLightOff;

		/// <summary>
		///     The sprite to show when this light is turned on
		/// </summary>
		public Sprite SpriteLightOn;

		private bool tempStateCache;

		//For network sync reliability
		private bool waitToCheckState;

		private void Awake()
		{
			Renderer = GetComponentInChildren<SpriteRenderer>();
		}

		private void Start()
		{
			ambientMask = LayerMask.GetMask("LightingAmbience");
			obstacleMask = LayerMask.GetMask("Walls", "Door Open", "Door Closed");
			InitLightSprites();
		}

		private void SetLocalAmbientTiles(bool state)
		{
			int length = Physics2D.OverlapCircleNonAlloc(transform.position, radius, lightSpriteColliders, ambientMask);
			for (int i = 0; i < length; i++)
			{
				Collider2D localCollider = lightSpriteColliders[i];
				GameObject localObject = localCollider.gameObject;
				Vector2 localObjectPos = localObject.transform.position;
				float distance = Vector3.Distance(transform.position, localObjectPos);
				if (IsWithinReach(transform.position, localObjectPos, distance))
				{
					localObject.SendMessage("Trigger", state, SendMessageOptions.DontRequireReceiver);
				}
			}
		}

		private bool IsWithinReach(Vector2 pos, Vector2 targetPos, float distance)
		{
			return distance <= radius
			       &&
			       Physics2D.Raycast(pos, targetPos - pos, distance, obstacleMask).collider == null;
		}

		public override void Trigger(bool state)
		{
			tempStateCache = state;

			if (waitToCheckState)
			{
				return;
			}

			if (Renderer == null)
			{
				waitToCheckState = true;
				StartCoroutine(WaitToTryAgain());
				return;
			}
			Renderer.sprite = state ? SpriteLightOn : SpriteLightOff;
			if (Light != null)
			{
				Light.SetActive(state);
			}
			SetLocalAmbientTiles(state);
		}

		private void InitLightSprites()
		{
			LightState = LightState.On;

			//set the ON sprite to whatever the spriterenderer child has?
			Sprite[] lightSprites = SpriteManager.LightSprites["lights"];
			SpriteLightOn = Renderer.sprite;

			//find the OFF light?
			string[] split = SpriteLightOn.name.Split('_');
			int onPos;
			int.TryParse(split[1], out onPos);
			SpriteLightOff = lightSprites[onPos + 4];
		}

		//Handle sync failure
		private IEnumerator WaitToTryAgain()
		{
			yield return new WaitForSeconds(0.2f);
			if (Renderer == null)
			{
				Renderer = GetComponentInChildren<SpriteRenderer>();
				if (Renderer != null)
				{
					Renderer.sprite = tempStateCache ? SpriteLightOn : SpriteLightOff;
					if (Light != null)
					{
						Light.SetActive(tempStateCache);
					}
				}
				else
				{
					Debug.LogWarning("LightSource still failing Renderer sync");
				}
			}
			else
			{
				Renderer.sprite = tempStateCache ? SpriteLightOn : SpriteLightOff;
				if (Light != null)
				{
					Light.SetActive(tempStateCache);
				}
			}
			waitToCheckState = false;
		}
	}
}