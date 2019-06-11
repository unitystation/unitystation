using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_Vendor : NetTab
{
	[SerializeField]
	private bool allowSell = true;
	[SerializeField]
	private float cooldownTimer = 2f;
	[SerializeField]
	private string interactionMessage;
	[SerializeField]
	private string deniedMessage;
	public bool EjectObjects = false;
	[SerializeField]
	private EjectDirection ejectDirection = EjectDirection.None;

	private VendorTrigger vendor;
	private List<VendorItem> vendorContent = new List<VendorItem>();
	

	protected override void InitServer()
	{
		vendor = Provider.GetComponent<VendorTrigger>();
		vendorContent = vendor.VendorContent;
	}

	private void GenerateList()
	{
		
	}

	private void SpawnItem(VendorItem item)
	{
		if (CanSell() == false)
			return;

		var spawnedItem = PoolManager.PoolNetworkInstantiate(item.item, transform.position, transform.parent);

		//Ejecting in direction
		if (EjectObjects && ejectDirection != EjectDirection.None)
		{
			Vector3 offset = Vector3.zero;
			switch (ejectDirection)
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
				SpinMode = ejectDirection == EjectDirection.Random ? SpinMode.Clockwise : SpinMode.None
			});
		}

		allowSell = false;
		StartCoroutine(VendorInputCoolDown());
	}

	public bool CanSell()
	{
		if (!allowSell && deniedMessage != null && !GameData.Instance.testServer && !GameData.IsHeadlessServer)
		{
			UpdateChatMessage.Send(vendor.Originator, ChatChannel.Examine, deniedMessage);
		}
		else if (allowSell && !GameData.Instance.testServer && !GameData.IsHeadlessServer)
		{
			UpdateChatMessage.Send(vendor.Originator, ChatChannel.Examine, interactionMessage);
			return true;
		}
		return false;
	}

	private IEnumerator VendorInputCoolDown()
	{
		yield return WaitFor.Seconds(cooldownTimer);
		allowSell = true;
	}
}
