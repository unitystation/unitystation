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
	public bool EjectObjects = false;
	public EjectDirection EjectDirection = EjectDirection.None;

	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (!CanUse(originator, hand, position, false))
		{
			return false;
		}
		if (!isServer)
		{
			//ask server to perform the interaction
			InteractMessage.Send(gameObject, position, hand);
			return true;
		}

		if (!allowSell && deniedMessage != null && !GameData.Instance.testServer && !GameData.IsHeadlessServer)
		{
			UpdateChatMessage.Send(originator, ChatChannel.Examine, deniedMessage);
		}
		else if (allowSell)
		{
			allowSell = false;
			if (!GameData.Instance.testServer && !GameData.IsHeadlessServer)
			{
				UpdateChatMessage.Send(originator, ChatChannel.Examine, interactionMessage);
			}
			ServerVendorInteraction(position);
			StartCoroutine(VendorInputCoolDown());
		}

		return true;
	}

	[Server]
	private bool ServerVendorInteraction(Vector3 position)
	{
		//		Debug.Log("status" + allowSell);
		if (vendorcontent.Length == 0)
		{
			return false;
		}

		int randIndex = Random.Range(0, vendorcontent.Length);

		var spawnedItem = PoolManager.PoolNetworkInstantiate(vendorcontent[randIndex], transform.position, transform.parent);

		//Ejecting in direction
		if (EjectObjects && EjectDirection != EjectDirection.None)
		{
			Vector3 offset = Vector3.zero;
			switch (EjectDirection)
			{
				case EjectDirection.Up:
					offset = transform.rotation * Vector3.up / Random.Range(4, 12);
					break;
				case EjectDirection.Down:
					offset = transform.rotation * Vector3.down / Random.Range(4, 12);
					break;
				case EjectDirection.Random:
					offset = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.15f, 0.15f), 0);
					break;
			}
			spawnedItem.GetComponent<CustomNetTransform>()?.Throw(new ThrowInfo
			{
				ThrownBy = gameObject,
				Aim = BodyPartType.Chest,
				OriginPos = transform.position,
				TargetPos = transform.position + offset,
				SpinMode = EjectDirection == EjectDirection.Random ? SpinMode.Clockwise : SpinMode.None
			});
		}
		stock--;

		return true;
	}

	private IEnumerator VendorInputCoolDown()
	{
		yield return new WaitForSeconds(cooldownTimer);
		if (stock > 0)
		{
			allowSell = true;
		}
	}

}

public enum EjectDirection { None, Up, Down, Random }
