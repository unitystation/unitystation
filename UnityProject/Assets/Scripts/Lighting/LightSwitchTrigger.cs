using System.Collections;
using PlayGroup;
using PlayGroups.Input;
using UnityEngine;
using UnityEngine.Networking;

namespace Lighting
{
	public class LightSwitchTrigger : InputTrigger
	{
		private const int MAX_TARGETS = 44;

		private readonly Collider2D[] lightSpriteColliders = new Collider2D[MAX_TARGETS];
		private AudioSource clickSFX;

		[SyncVar(hook = "SyncLightSwitch")] public bool isOn = true;

		private int lightingMask;
		public Sprite lightOff;
		public Sprite lightOn;
		private int obstacleMask;
		public float radius = 10f;
		private bool soundAllowed;
		private SpriteRenderer spriteRenderer;
		private bool switchCoolDown;

		private void Awake()
		{
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			clickSFX = GetComponent<AudioSource>();
		}

		private void Start()
		{
			//This is needed because you can no longer apply lightSwitch prefabs (it will move all of the child sprite positions)
			gameObject.layer = LayerMask.NameToLayer("WallMounts");
			//and the rest of the mask caches:
			lightingMask = LayerMask.GetMask("Lighting");
			obstacleMask = LayerMask.GetMask("Walls", "Door Open", "Door Closed");
		}

		public override void OnStartClient()
		{
			StartCoroutine(WaitForLoad());
		}

		private IEnumerator WaitForLoad()
		{
			yield return new WaitForSeconds(3f);
			SyncLightSwitch(isOn);
		}

		public override void Interact(GameObject originator, Vector3 position, string hand)
		{
			if (!PlayerManager.LocalPlayerScript.IsInReach(position))
			{
				return;
			}

			if (switchCoolDown)
			{
				return;
			}

			StartCoroutine(CoolDown());
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleLightSwitch(gameObject);
		}

		private IEnumerator CoolDown()
		{
			switchCoolDown = true;
			yield return new WaitForSeconds(0.2f);
			switchCoolDown = false;
		}

		private void DetectLightsAndAction(bool state)
		{
			Vector2 startPos = GetCastPos();
			int length = Physics2D.OverlapCircleNonAlloc(startPos, radius, lightSpriteColliders, lightingMask);
			for (int i = 0; i < length; i++)
			{
				Collider2D localCollider = lightSpriteColliders[i];
				GameObject localObject = localCollider.gameObject;
				Vector2 localObjectPos = localObject.transform.position;
				float distance = Vector3.Distance(startPos, localObjectPos);
				if (IsWithinReach(startPos, localObjectPos, distance))
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

		private Vector2 GetCastPos()
		{
			Vector2 newPos = transform.position + (transform.position - spriteRenderer.transform.position);
			return newPos;
		}

		private void SyncLightSwitch(bool state)
		{
			DetectLightsAndAction(state);

			if (clickSFX != null && soundAllowed)
			{
				clickSFX.Play();
			}

			if (spriteRenderer != null)
			{
				spriteRenderer.sprite = state ? lightOn : lightOff;
			}
			soundAllowed = true;
		}
	}
}