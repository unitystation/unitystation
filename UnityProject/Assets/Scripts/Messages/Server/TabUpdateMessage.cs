using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public class TabUpdateMessage : ServerMessage
{
	//updates which are longer than this will be broken up
	private static readonly short MAX_ELEMENTS_PER_MESSAGE = 10;

	public static short MessageType = (short) MessageTypes.TabUpdateMessage;

	public uint Provider;
	public NetTabType Type;
	public TabAction Action;

	public ElementValue[] ElementValues;

	public bool Touched;

	private static readonly ElementValue[] NoValues = new ElementValue[0];

	public override IEnumerator Process() {
		Logger.LogTraceFormat("Processed {0}", Category.NetUI, this);
		yield return WaitFor( Provider );
		switch ( Action ) {
			case TabAction.Open:
				ControlTabs.ShowTab( Type, NetworkObject, ElementValues );
				break;
			case TabAction.Close:
				ControlTabs.CloseTab( Type, NetworkObject );
				break;
			case TabAction.Update:
				ControlTabs.UpdateTab( Type, NetworkObject, ElementValues, Touched );
				break;
		}
	}

	public override string ToString() {
		return $"[TabUpdateMessage {nameof( Provider )}: {Provider}, {nameof( Type )}: {Type}, " +
		       $"{nameof( Action )}: {Action}, " +
		       $"{nameof( ElementValue )}: {string.Join("; ",ElementValues ?? NoValues)}]";
	}

	public static void SendToPeepers( GameObject provider, NetTabType type, TabAction tabAction,
		ElementValue[] values = null )
	{
		List<ConnectedPlayer> list = NetworkTabManager.Instance.GetPeepers( provider, type );
		foreach ( ConnectedPlayer connectedPlayer in list )
		{
			Send( connectedPlayer.GameObject, provider, type, tabAction, null, values );
		}

	}

	public static void Send(GameObject recipient, GameObject provider, NetTabType type, TabAction tabAction,
		GameObject changedBy = null,
		ElementValue[] values = null)
	{
		if (tabAction == TabAction.Open)
		{
			//register the recipient of this tab and
			//automatically get the values from the nettab instance
			NetworkTabManager.Instance.Add(provider, type, recipient);
			values = NetworkTabManager.Instance.Get( provider, type ).ElementValues;
		}
		//NOTE: Slightly unrobust way of breaking up large messages into smaller chunks to avoid hitting max message size
		//but this should be good for now
		//This limit only gets hit when sending initial values of a large nettab, so it's rarely more than one chunk
		if (values != null && values.Length > MAX_ELEMENTS_PER_MESSAGE)
		{
			foreach (var chunk in values.Chunk(MAX_ELEMENTS_PER_MESSAGE).Select(c => c.ToArray()))
			{
				SendInternal(recipient, provider, type, tabAction, changedBy, chunk);
			}
		}
		else
		{
			SendInternal(recipient, provider, type, tabAction, changedBy, values);
		}
	}

	private static TabUpdateMessage SendInternal( GameObject recipient, GameObject provider, NetTabType type, TabAction tabAction, GameObject changedBy = null,
		ElementValue[] values = null ) {
//		if ( changedBy ) {
//			//body = human_33, hands, uniform, suit
//		}
		var msg = new TabUpdateMessage {
			Provider = provider.NetId(),
			Type = type,
			Action = tabAction,
			ElementValues = values,
			Touched = changedBy != null
		};
		switch ( tabAction ) {
			case TabAction.Close:
				NetworkTabManager.Instance.Remove(provider, type, recipient);
				break;
			case TabAction.Update:
				var playerScript = recipient.Player()?.Script;

				//fixme: duplication of NetTab.ValidatePeepers
				//Not sending updates and closing tab for players that don't pass the validation anymore
				bool validate = playerScript && !playerScript.canNotInteract() && playerScript.IsInReach( provider, true );
				if ( !validate ) {
					SendInternal( recipient, provider, type, TabAction.Close );
					return msg;
				}
				break;
		}
		msg.SendTo( recipient );
		Logger.LogTrace( msg.ToString(), Category.NetUI );
		return msg;
	}
}

public struct ElementValue
{
	public string Id;
	public string Value;

	public override string ToString() {
		return $"[{Id}={Value}]";
	}
}

public enum TabAction {Open, Close, Update}
