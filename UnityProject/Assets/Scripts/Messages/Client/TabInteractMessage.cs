using System.Collections;
using PlayGroup;
using UnityEngine;
using UnityEngine.Networking;
using Util;

/// <summary>
///     Informs server of interaction with some object's tab element
/// </summary>
public class TabInteractMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.TabInteractMessage;
	public NetworkInstanceId TabProvider;
	public TabType TabType;
	public string ElementId;
	public string ElementValue;
	//Serverside
	public override IEnumerator Process()
	{
		Debug.Log("Processed " + ToString());
		yield return WaitFor(SentBy, TabProvider);
		ProcessFurther(NetworkObjects[0], NetworkObjects[1]);
	}

	private void ProcessFurther(GameObject player, GameObject tabProvider)
	{
		var playerScript = player.Player().Script; 
		bool validate = !playerScript.canNotInteract() && playerScript.IsInReach( tabProvider );
		if ( !validate ) {
			FailValidation( player, tabProvider, "Can't interact/reach" );
			return;
		}
		var tabInfo = NetworkTabManager.Instance.Get( tabProvider, TabType );
		if ( tabInfo == NetworkTabInfo.Invalid ) {
			//No such tab exists on server!
			FailValidation( player, tabProvider, $"No such tab: {tabProvider}/{TabType}" );
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
		foreach ( var connectedPlayer in NetworkTabManager.Instance.GetPeepers( tabProvider, TabType ) ) {
			//Not sending that update to the same player
			if ( connectedPlayer.GameObject != player ) {
				TabUpdateMessage.Send( connectedPlayer.GameObject, tabProvider, TabType, TabAction.Update, player,
					new[]{new ElementValue{ Id = ElementId, Value = updatedElement.Value}}  );
			}
		}
	}

	private TabUpdateMessage FailValidation( GameObject player, GameObject tabProvider, string reason="" ) {
		Debug.LogWarning( $"{player}: Tab interaction w/{tabProvider} denied: {reason}" );
		return TabUpdateMessage.Send( player, tabProvider, TabType, TabAction.Close );
	}

	public static TabInteractMessage Send( GameObject tabProvider, TabType tabType, string elementId, string elementValue = "-1" )
	{
		TabInteractMessage msg = new TabInteractMessage {
			TabProvider = tabProvider.NetId(),
			TabType = tabType,
			ElementId = elementId,
			ElementValue = elementValue
		};
		msg.Send();
		return msg;
	}

	public override string ToString() {
		return $"[TabInteractMessage {nameof( TabProvider )}: {TabProvider}, {nameof( TabType )}: {TabType}, " +
									$"{nameof( ElementId )}: {ElementId}, {nameof( ElementValue )}: {ElementValue}, " +
									$"MsgType={MessageType} SentBy={SentBy}]";
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		TabProvider = reader.ReadNetworkId();
		TabType = (TabType) reader.ReadInt32();
		ElementId = reader.ReadString();
		ElementValue = reader.ReadString();
//		ElementValue = reader.ReadInt32();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(TabProvider);
		writer.Write( (int)TabType );
		writer.Write( ElementId );
		writer.Write( ElementValue );
	}
}