using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LightSwitchTrigger : InputTrigger
{
	private const int MAX_TARGETS = 44;

	private readonly Collider2D[] lightSpriteColliders = new Collider2D[MAX_TARGETS];
	private AudioSource clickSFX;

	[SyncVar(hook = "SyncLightSwitch")] public bool isOn = true;

	public float AtShutOffVoltage = 50;

	public Sprite lightOff;
	private int lightingMask;
	public APC RelatedAPC;
	public Sprite lightOn;
	public bool PowerCut = false;
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
		DetectLightsAndAction(true);
		if (RelatedAPC != null)
		{
			RelatedAPC.ConnectedSwitchesAndLights[this] = new List<LightSource>();
		}
	}
	public void PowerNetworkUpdate(float Voltage)
	{
		if (Voltage < AtShutOffVoltage && isOn == true)
		{
			isOn = false;
			PowerCut = true;
		}
		else if (PowerCut == true && Voltage > AtShutOffVoltage)
		{
			isOn = true;
			PowerCut = false;
		}

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

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (!PlayerManager.LocalPlayerScript.IsInReach(position))
		{
			return true;
		}

		if (RelatedAPC == null)
		{
			return true;
		}

		if (RelatedAPC.Voltage == 0f)
		{
			return true;
		}

		if (switchCoolDown)
		{
			return true;
		}

		StartCoroutine(CoolDown());
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleLightSwitch(gameObject);

		return true;
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
				if (localObject.tag != "EmergencyLight")
				{
					LightSwitchData Send = new LightSwitchData ( ){state = state,LightSwitchTrigger = this,  RelatedAPC = RelatedAPC };
					localObject.SendMessage("Received", Send,  SendMessageOptions.DontRequireReceiver);
				}
				if (RelatedAPC != null)
				{
					LightSwitchData Send = new LightSwitchData (){ LightSwitchTrigger = this,  RelatedAPC = RelatedAPC };
					localObject.SendMessage("EmergencyLight",Send, SendMessageOptions.DontRequireReceiver);
				}
			}
		}
	}

	private bool IsWithinReach(Vector2 pos, Vector2 targetPos, float distance)
	{
		return distance <= radius &&
			Physics2D.Raycast(pos, targetPos - pos, distance, obstacleMask).collider == null;
	}

	private Vector2 GetCastPos()
	{
		Vector2 newPos = transform.position + ((spriteRenderer.transform.position - transform.position).normalized);
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

public class LightSwitchData {
	public bool state;
	public LightSwitchTrigger LightSwitchTrigger;
	public APC RelatedAPC;
}