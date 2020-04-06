using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class TabUpdateMessage : ServerMessage {
	public uint Provider;
	public NetTabType Type;
	public TabAction Action;

	public ElementValue[] ElementValues;

	public bool Touched;

	private static readonly ElementValue[] NoValues = new ElementValue[0];

	public override void Process() {
		Logger.LogTraceFormat("Processed {0}", Category.NetUI, this);
		LoadNetworkObject(Provider);
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
		//Notify all peeping players of the change
		List<ConnectedPlayer> list = NetworkTabManager.Instance.GetPeepers( provider, type );
		foreach ( ConnectedPlayer connectedPlayer in list )
		{
			Send( connectedPlayer.GameObject, provider, type, tabAction, null, values );
		}
	}

	public static TabUpdateMessage Send( GameObject recipient, GameObject provider, NetTabType type, TabAction tabAction, GameObject changedBy = null,
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
			case TabAction.Open:
				NetworkTabManager.Instance.Add(provider, type, recipient);
				//!! resetting ElementValues
				msg.ElementValues = NetworkTabManager.Instance.Get( provider, type ).ElementValues;
				//!!
				break;
			case TabAction.Close:
				NetworkTabManager.Instance.Remove(provider, type, recipient);
				break;
			case TabAction.Update:
				var playerScript = recipient.Player()?.Script;

				//fixme: duplication of NetTab.ValidatePeepers
				//Not sending updates and closing tab for players that don't pass the validation anymore
				bool validate = Validations.CanApply(recipient, provider, NetworkSide.Server);
				if ( !validate ) {
					Send( recipient, provider, type, TabAction.Close );
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
