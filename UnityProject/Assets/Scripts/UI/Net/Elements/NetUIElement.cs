using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using UnityEngine;


/// <summary>
/// Base class for networked UI element
/// </summary>
[Serializable]
public abstract class NetUIElement<T> : NetUIElementBase
{
	protected bool externalChange;

	/// <summary>
	/// Unique tab that contains this element
	/// </summary>
	public NetTab MasterTab
	{
		get
		{
			if (!masterTab)
			{
				masterTab = GetComponentsInParent<NetTab>(true)[0];
			}

			return masterTab;
		}
	}

	private NetTab masterTab;
	private static readonly JsonSerializer JsonSerializer = new JsonSerializer
	{
		ReferenceLoopHandling = ReferenceLoopHandling.Ignore
	};

	public override ElementValue ElementValue => new ElementValue
	{
		Id = name,
		Value = BinaryValue
	};

	public override ElementMode InteractionMode => ElementMode.Normal;

	public virtual T Value
	{
		get => default;
		set { }
	}

	public override object ValueObject
	{
		get => Value;
		set => Value = (T) value;
	}

	public override byte[] BinaryValue
	{
		set
		{
			var ms = new MemoryStream(value);
			using (var bsonReader = new BsonReader(ms))
			{
				Value = JsonSerializer.Deserialize<Wrapper<T>>(bsonReader).Value;
			}
		}
		get
		{
			var ms = new MemoryStream();
			using (var writer = new BsonWriter(ms))
			{
				JsonSerializer.Serialize(writer, new Wrapper<T>(Value));
			}

			return ms.ToArray();
		}
	}

	/// <summary>
	/// Server-only method for updating element (i.e. changing label text) from server GUI code
	/// </summary>
	public virtual void SetValueServer(T value)
	{
		Value = value;
		UpdatePeepers();
	}

	/// <summary>
	/// Always point to this method in OnValueChanged
	/// <a href="https://camo.githubusercontent.com/e3bbac26b36a01c9df8fbb6a6858bb4a82ba3036/68747470733a2f2f63646e2e646973636f72646170702e636f6d2f6174746163686d656e74732f3339333738373838303431353239373534332f3435333632313031363433343833353435362f7467745f636c69656e742e676966">See GIF</a>
	/// </summary>
	public override void ExecuteClient()
	{
		//Don't send if triggered by external change
		if (!externalChange)
		{
			TabInteractMessage.Send(MasterTab.Provider, MasterTab.Type, name, BinaryValue);
		}
	}

	/// <summary>
	/// Send update to observers.
	/// </summary>
	protected void UpdatePeepers()
	{
		if (gameObject.activeInHierarchy)
		{
			UpdatePeepersLogic();
		}
		else
		{
			MasterTab.ValidatePeepers();
		}
	}


	/// <summary>
	/// Override if you want custom "send update to peepers" logic
	/// i.e. to include more values than just the current one
	/// </summary>
	protected virtual void UpdatePeepersLogic()
	{
		var masterTab = MasterTab;
		TabUpdateMessage.SendToPeepers(masterTab.Provider, masterTab.Type, TabAction.Update, new[] {ElementValue});
	}

	public override void ExecuteServer(ConnectedPlayer subject){}

	public override string ToString() {
		return ElementValue.ToString();
	}

	private class Wrapper<T>
	{
		public T Value;

		public Wrapper(T value)
		{
			Value = value;
		}
	}
}


public abstract class NetUIElementBase : MonoBehaviour
{
	public const char DELIMITER = '~';
	public abstract ElementValue ElementValue { get; }
	public abstract ElementMode InteractionMode { get; }
	public abstract object ValueObject { get; set; }

	/// <summary>
	/// Initialize method before element list is collected. For editor-set values
	/// </summary>
	public virtual void Init() {}

	public abstract byte[] BinaryValue { get; set; }

	/// <summary>
	/// Always point to this method in OnValueChanged
	/// <a href="https://camo.githubusercontent.com/e3bbac26b36a01c9df8fbb6a6858bb4a82ba3036/68747470733a2f2f63646e2e646973636f72646170702e636f6d2f6174746163686d656e74732f3339333738373838303431353239373534332f3435333632313031363433343833353435362f7467745f636c69656e742e676966">See GIF</a>
	/// </summary>
	public abstract void ExecuteClient();

	public abstract void ExecuteServer(ConnectedPlayer subject);

	/// <summary>
	/// Special logic to execute after all tab elements are initialized
	/// </summary>
	public virtual void AfterInit() {}
}

public enum ElementMode
{
	/// Changeable by both client and server
	Normal,

	/// Only server can change value
	ServerWrite,

	/// Only client can change value, and server doesn't store it
	ClientWrite
}