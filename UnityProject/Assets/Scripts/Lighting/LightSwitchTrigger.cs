using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class LightSwitchTrigger : InputTrigger
{

	public enum States { 
		Off,
		On,
		PowerCut,
	}
	private const int MAX_TARGETS = 44;

	private readonly Collider2D[] lightSpriteColliders = new Collider2D[MAX_TARGETS];
	private AudioSource clickSFX;

	[SyncVar(hook = "SyncLightSwitch")] public States isOn = States.On;

	public float AtShutOffVoltage = 50;

	public Sprite lightOff;
	private int lightingMask;
	public APC RelatedAPC;
	public Sprite lightOn;
	public Sprite lightPowerCut;
	public bool PowerCut = false;
	private int obstacleMask;
	public float radius = 10f;
	private bool soundAllowed;
	private SpriteRenderer spriteRenderer;
	private bool switchCoolDown;
	private RegisterTile registerTile;
	public bool SelfPowered = false;
	public List<LightSource> SelfPowerLights = new List<LightSource>();

	private void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
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
		WaitForLoad();
		DetectAPC();
		DetectLightsAndAction(true);
		if (RelatedAPC != null)
		{
			RelatedAPC.ConnectedSwitchesAndLights[this] = new List<LightSource>();
		}
		if (SelfPowered) {
			for (int i = 0; i < SelfPowerLights.Count; i++)
			{
				SelfPowerLights[i].PowerLightIntensityUpdate(240);
			}
		}
	}
	public void PowerNetworkUpdate(float Voltage)
	{
		if (Voltage < AtShutOffVoltage && isOn ==  States.On)
		{
			isOn =  States.PowerCut;
			PowerCut = true;
			if (PowerCut)
			{

				spriteRenderer.sprite = lightPowerCut;
			}
		}
		else if (PowerCut == true && Voltage > AtShutOffVoltage)
		{
			isOn = States.On;
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
		if (!PlayerManager.LocalPlayerScript.IsInReach(position, false))
		{
			return true;
		}
		if (!SelfPowered) {
			if (RelatedAPC == null)
			{
				return true;
			}
			if (RelatedAPC.Voltage == 0f)
			{
				return true;
			}
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

	//Find the APC in the same room as the light switch and assign it to RelatedAPC
	private void DetectAPC()
	{
		if (RelatedAPC != null)
		{
			return;
		}

		int layerMask = LayerMask.GetMask("WallMounts");
		var possibleApcs = Physics2D.OverlapCircleAll(GetCastPos(), radius, layerMask);

		int thisRoomNum = MatrixManager.GetMetaDataAt(Vector3Int.RoundToInt(transform.position-transform.up)).RoomNumber;

		foreach (Collider2D col in possibleApcs)
		{
			if (col.tag == "APC")
			{
				//Light switch has no room number, assign it to the first APC it finds so it still functions
				if (thisRoomNum == -1)
				{
					RelatedAPC = col.gameObject.GetComponent<APC>();
					break;
				}

				if (MatrixManager.GetMetaDataAt(Vector3Int.RoundToInt(col.transform.position-col.transform.up )).RoomNumber == thisRoomNum)
				{
					RelatedAPC = col.gameObject.GetComponent<APC>();
					break;
				}
			}
		}
		if (RelatedAPC == null && !SelfPowered) {
			isOn =  States.PowerCut;
		}
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
					LightSwitchData Send = new LightSwitchData() { state = state, LightSwitchTrigger = this, RelatedAPC = RelatedAPC };
					localObject.SendMessage("Received", Send, SendMessageOptions.DontRequireReceiver);
				}
				if (RelatedAPC != null ) //|| SelfPowered
				{
					LightSwitchData Send = new LightSwitchData() { LightSwitchTrigger = this, RelatedAPC = RelatedAPC, SelfPowered = SelfPowered };
					localObject.SendMessage("EmergencyLight", Send, SendMessageOptions.DontRequireReceiver);
				}
			}
		}
	}

	private bool IsWithinReach(Vector2 pos, Vector2 targetPos, float distance)
	{
		return distance <= radius &&
			Physics2D.Raycast(Abs(pos), Abs(targetPos) - Abs(pos), distance, obstacleMask).collider == null;
	

	}

	public Vector2 Abs (Vector2 v2) {
		return (new Vector2(Mathf.Abs(v2.x), Mathf.Abs(v2.y)));
    }


	private Vector2 GetCastPos()
	{
		Vector2 newPos = transform.position + ((spriteRenderer.transform.position - transform.position).normalized);
		return newPos;
	}

	private void SyncLightSwitch(States state)
	{
		if (state == States.On)
		{
			DetectLightsAndAction(true);
		}
		else if (state == States.Off || state == States.PowerCut){ 
			DetectLightsAndAction(false);
		}


		if (clickSFX != null && soundAllowed)
		{
			clickSFX.Play();
		}

		if (spriteRenderer != null)
		{
			switch (state)
			{
				case States.Off:
					spriteRenderer.sprite = lightOff;
					break;
				case States.On:
					spriteRenderer.sprite = lightOn;
					break;
				case States.PowerCut:
					spriteRenderer.sprite = lightPowerCut;
					break;
			}
		}
		soundAllowed = true;
	}
}

public class LightSwitchData
{
	public bool state;
	public LightSwitchTrigger LightSwitchTrigger;
	public APC RelatedAPC;
	public bool SelfPowered;
}