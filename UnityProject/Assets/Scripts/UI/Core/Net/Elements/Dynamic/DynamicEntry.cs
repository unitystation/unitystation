﻿using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Dynamic list entry
/// </summary>
public class DynamicEntry : NetUIElement<Vector2> {
	public NetUIElementBase[] Elements => GetComponentsInChildren<NetUIElementBase>(false);
	public override ElementMode InteractionMode => ElementMode.ServerWrite;

	public override Vector2 Value {
		get {
			return transform.localPosition;
		}
		set {
			externalChange = true;
			transform.localPosition = value;
			externalChange = false;
		}
	}

	public override byte[] BinaryValue
	{
		get
		{
			var bytes = BitConverter.GetBytes(transform.localPosition.x) .ToList();
			bytes.AddRange(BitConverter.GetBytes(transform.localPosition.y));

			return bytes.ToArray();
		}
		set
		{
			Value = new Vector2(
				BitConverter.ToSingle(value, 0),
				BitConverter.ToSingle(value, sizeof(float)));
		}
	}

	public Vector3 Position {
		get { return transform.localPosition; }
		set { transform.localPosition = value; }
	}

	public override void ExecuteServer(ConnectedPlayer subject)
	{
	}
}