using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class FireAlarm : InputTrigger
{	
	private const int MAX_TARGETS = 44;

	private int lightingMask;
	private int obstacleMask;
	
	private bool switchCoolDown;
	
	private SpriteRenderer spriteRenderer;
	
	private readonly Collider2D[] lightSpriteColliders = new Collider2D[MAX_TARGETS];
	
	public Vector2 startPos;
	public Vector2 stopPos;
	
	private void Start()
	{
		
		gameObject.layer = LayerMask.NameToLayer("WallMounts");
		lightingMask = LayerMask.GetMask("EmergencyLighting");
		obstacleMask = LayerMask.GetMask("Walls", "Door Open", "Door Closed");
		DetectLightsAndAction (true);
	}
	
	public override void OnStartClient()
	{
		StartCoroutine(WaitForLoad());
	}
	
	private IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(3f);
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
		ToggleFire(gameObject);
	}
	
	private void ToggleFire(GameObject switchObj)
	{
		FireAlarm s = switchObj.GetComponent<FireAlarm>();
		
	}
	
	private IEnumerator CoolDown()
	{
		switchCoolDown = true;
		yield return new WaitForSeconds(0.2f);
		switchCoolDown = false;
	}
	
	private void DetectLightsAndAction(bool state)
	{
		Vector2 myPos = GetCastPos();
		int length = Physics2D.OverlapAreaNonAlloc(startPos, stopPos, lightSpriteColliders, lightingMask);
		for (int i = 0; i < length; i++)
		{
			Collider2D localCollider = lightSpriteColliders[i];
			GameObject localObject = localCollider.gameObject;
			Vector2 localObjectPos = localObject.transform.position;
			float distance = Vector3.Distance(myPos, localObjectPos);
			localObject.SendMessage("FireTrigger", state , SendMessageOptions.DontRequireReceiver);
		}
	}
	
	private Vector2 GetCastPos()
	{
		Vector2 newPos = transform.position + ((spriteRenderer.transform.position - transform.position).normalized);
		return newPos;
	}
}	