using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Informs server of interaction with some object's tab element
/// </summary>
public class TabInteractMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.TabInteractMessage;
	public NetworkInstanceId TabProvider;
	public NetTabType NetTabType;
	public string ElementId;
	public string ElementValue;
	//Serverside
	public override IEnumerator Process()
	{
//		Logger.Log("Processed " + ToString());
		yield return WaitFor(TabProvider);
		ProcessFurther(SentByPlayer, NetworkObject);
	}

	private void ProcessFurther(ConnectedPlayer player, GameObject tabProvider)
	{
		var playerScript = player.Script;
		bool validate = !playerScript.canNotInteract() && playerScript.IsInReach( tabProvider );
		if ( !validate ) {
			FailValidation( player, tabProvider, "Can't interact/reach" );
			return;
		}
		var tabInfo = NetworkTabManager.Instance.Get( tabProvider, NetTabType );
		if ( !tabInfo /* == NetworkTabInfo.Invalid*/ ) {
			//No such tab exists on server!
			FailValidation( player, tabProvider, $"No such tab: {tabProvider}/{NetTabType}" );
			return;
		}
		var updatedElement = tabInfo[ElementId];
		if ( updatedElement == null ) {
			//No such element exists on server!
			FailValidation( player, tabProvider, $"No such element: {tabInfo}[{ElementId}]" );
			return;
		}
		if ( updatedElement.InteractionMode == ElementMode.ServerWrite ) {
			//Don't change labels and other non-interactable elements. If this is triggered, someone's tampering with client
			FailValidation( player, tabProvider, $"Non-interactable {updatedElement}" );
			return;
		}

		var valueBeforeUpdate = updatedElement.Value;
		updatedElement.Value = ElementValue;
		updatedElement.ExecuteServer();

		if ( updatedElement.InteractionMode == ElementMode.ClientWrite ) {
			//Don't rememeber value provided by client and restore to the initial one
			updatedElement.Value = valueBeforeUpdate;
		}

		//Notify all peeping players of the change
		List<ConnectedPlayer> list = NetworkTabManager.Instance.GetPeepers( tabProvider, NetTabType );
		for ( var i = 0; i < list.Count; i++ ) {
			var connectedPlayer = list[i];
//Not sending that update to the same player
			if ( connectedPlayer.GameObject != player.GameObject ) {
				TabUpdateMessage.Send( connectedPlayer.GameObject, tabProvider, NetTabType, TabAction.Update, player.GameObject,
					new[] {new ElementValue {Id = ElementId, Value = updatedElement.Value}} );
			}
		}
	}

	private TabUpdateMessage FailValidation( ConnectedPlayer player, GameObject tabProvider, string reason="" ) {

		Logger.LogWarning( $"{player.Name}: Tab interaction w/{tabProvider} denied: {reason}",Category.NetUI );
		return TabUpdateMessage.Send( player.GameObject, tabProvider, NetTabType, TabAction.Close );
	}

	public static TabInteractMessage Send( GameObject tabProvider, NetTabType netTabType, string elementId, string elementValue = "-1" )
	{
		TabInteractMessage msg = new TabInteractMessage {
			TabProvider = tabProvider.NetId(),
			NetTabType = netTabType,
			ElementId = elementId,
			ElementValue = elementValue
		};
		msg.Send();
		return msg;
	}

	public override string ToString() {
		return $"[TabInteractMessage {nameof( TabProvider )}: {TabProvider}, {nameof( NetTabType )}: {NetTabType}, " +
									$"{nameof( ElementId )}: {ElementId}, {nameof( ElementValue )}: {ElementValue}, " +
									$"MsgType={MessageType} SentBy={SentByPlayer}]";
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		TabProvider = reader.ReadNetworkId();
		NetTabType = (NetTabType) reader.ReadInt32();
		ElementId = reader.ReadString();
		ElementValue = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(TabProvider);
		writer.Write( (int)NetTabType );
		writer.Write( ElementId );
		writer.Write( ElementValue );
	}
}