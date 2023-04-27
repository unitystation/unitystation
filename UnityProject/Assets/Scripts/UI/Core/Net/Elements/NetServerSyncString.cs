using System.Collections;
using System.Collections.Generic;
using UI.Core.NetUI;
using UnityEditor;
using UnityEngine;

public class NetServerSyncString : NetUIStringElement
{

	public override ElementMode InteractionMode => ElementMode.ServerWrite;

	private string CurrentString;

	public StringEvent OnChange;

	public override string Value
	{
		get
		{
			return CurrentString;
		}
		protected set
		{
			CurrentString = value;
			OnChange.Invoke(CurrentString);
		}
	}
}
