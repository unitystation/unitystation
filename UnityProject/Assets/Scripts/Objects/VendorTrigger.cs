using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class VendorTrigger : InputTrigger
{
	public GameObject[] vendorcontent;

	public bool allowSell = true;
	public float cooldownTimer = 2f;
	public int stock = 5;
	public string interactionMessage;
	public string deniedMessage;
	public DispenseDirection DispenseDirection = DispenseDirection.None;

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (!allowSell && deniedMessage != null && !GameData.Instance.testServer && !GameData.IsHeadlessServer)
		{
			ChatRelay.Instance.AddToChatLogClient(deniedMessage, ChatChannel.Examine);
		}
		// Client pre-approval
		else if (!isServer && allowSell)
		{
			allowSell = false;
			UI_ItemSlot slot = UIManager.Hands.CurrentSlot;
			ChatRelay.Instance.AddToChatLogClient(interactionMessage, ChatChannel.Examine);
			//Client informs server of interaction attempt
			InteractMessage.Send(gameObject, position, slot.eventName);
			StartCoroutine(VendorInputCoolDown());
		}
		else if(allowSell)
		{
			allowSell = false;
			if (!GameData.Instance.testServer && !GameData.IsHeadlessServer)
			{
				ChatRelay.Instance.AddToChatLogClient(interactionMessage, ChatChannel.Examine);
			}

			ServerVendorInteraction(originator, position, hand);
			StartCoroutine(VendorInputCoolDown());
		}

		return true;
	}

	[Server]
	private bool ServerVendorInteraction(GameObject originator, Vector3 position, string hand)
	{
//		Debug.Log("status" + allowSell);
		PlayerScript ps = originator.GetComponent<PlayerScript>();
		if (ps.canNotInteract() || !ps.IsInReach(position) || vendorcontent.Length == 0)
		{
			return false;
		}

		int randIndex = Random.Range(0, vendorcontent.Length);

		var spawnedItem = ItemFactory.SpawnItem(vendorcontent[randIndex], transform.position);

		//Dispensing in direction
		if ( DispenseDirection != DispenseDirection.None ) {
			Vector3 offset = Vector3.zero;
			switch ( DispenseDirection ) {
				case DispenseDirection.Up:
					offset = transform.rotation * Vector3.up / Random.Range(4,12);
					break;
				case DispenseDirection.Down:
					offset = transform.rotation * Vector3.down / Random.Range(4,12);
					break;
				case DispenseDirection.Random:
					offset = new Vector3( Random.Range( -0.15f, 0.15f ), Random.Range( -0.15f, 0.15f ), 0 );
					break;
			}
			spawnedItem.GetComponent<CustomNetTransform>()?.Throw( new ThrowInfo {
				ThrownBy = gameObject,
				Aim = BodyPartType.Chest,
				OriginPos = transform.position,
				TargetPos = transform.position + offset,
				SpinMode = DispenseDirection == DispenseDirection.Random ? SpinMode.Clockwise : SpinMode.None
			} );
		}
		stock--;

		return true;
	}

	private IEnumerator VendorInputCoolDown()
	{
		yield return new WaitForSeconds(cooldownTimer);
		if ( stock > 0 )
		{
			allowSell = true;
		}
	}

}

public enum DispenseDirection { None, Up, Down, Random }
