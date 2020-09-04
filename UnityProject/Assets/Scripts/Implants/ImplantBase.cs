using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ItemAttributesV2))]
public class ImplantBase : MonoBehaviour
{
	private ItemAttributesV2 attributes;
	private void Awake()
	{
		attributes = GetComponent<ItemAttributesV2>();
	}
}
