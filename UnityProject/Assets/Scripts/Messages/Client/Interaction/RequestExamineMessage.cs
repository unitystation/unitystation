using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mirror;
using UnityEngine;

/// <summary>
/// Requests for the server to perform examine interaction
/// </summary>
public class RequestExamineMessage : ClientMessage
{
	//TODO: Is this constant needed anymore
	public static short MessageType = (short) MessageTypes.RequestExamine;

	//members
	// netid of target
	public uint examineTarget;

	static RequestExamineMessage()
	{
		//constructor

	}

	public override IEnumerator Process()
	{

		string msg = "";
		//TODO: example of break condition
		if (SentByPlayer == null || SentByPlayer.Script == null)
		{
			yield break;
		}

		// Sort of translates one or more netId to gameobjects contained in NetworkObjects[]
		// it's confusing AF.
		yield return WaitFor(examineTarget);

		// Check for ID and display job. Would like to see this move to ItemStorage eventually.
		// string  occupationString  = "";
		// var idComponent = NetworkObject.GetComponent<ItemStorage>()?.GetNamedItemSlot(NamedSlot.id)?.ItemObject?.GetComponent<IDCard>();
		// if (idComponent != null)
		// {
		// 	occupationString = ", " + idComponent.Occupation.DisplayName;
		// }

		// Should this just move to Attributes.cs and other comps? YES + TODO: removeme
		//string msg = $"This is {NetworkObject.name + occupationString}. \n";
		

		// Here we build the message to send, by looking at the target's components. 
		// anyone extending IExaminable gets a say in it.
		// Look for examinables.
		var examinables = NetworkObject.GetComponents<IExaminable>();
		foreach (var examinable in examinables)
		{
			msg += $"{examinable.Examine()}\n";
		}
		
		// Send the message.
		Chat.AddExamineMsgFromServer(SentByPlayer.GameObject, msg);
		
		//TODO: example process body

		// //look up item in active hand slot
		// var clientStorage = SentByPlayer.Script.ItemStorage;
		// var usedSlot = clientStorage.GetActiveHandSlot();
		// var usedObject = clientStorage.GetActiveHandSlot().ItemObject;
		// yield return WaitFor(TargetObject, ProcessorObject);
		// var targetObj = NetworkObjects[0];
		// var processorObj = NetworkObjects[1];
		// var interaction = PositionalHandApply.ByClient(performer, usedObject, targetObj, TargetVector, usedSlot, Intent, TargetBodyPart);
		// ProcessInteraction(interaction, processorObj);
	
	}

	//TODO: Example send()
	public static void Send(uint targetNetId)
	{
		// Log something?
		var msg = new RequestExamineMessage()
		{
			examineTarget = targetNetId
		};
		msg.Send();
	}

	// //TODO: Figure out serial/deserialization
	// public override void Deserialize(NetworkReader reader)
	// {

	// }

	// public override void Serialize(NetworkWriter writer)
	// {
	
	// }

}

///
// //TODO!! notepad for stuff to look at
// 	var lhb = clickedObject.GetComponent<LivingHealthBehaviour>(); 
// 		string pronoun = "It";

// 		if (lhb)
// 		{
// 			var healthFraction = lhb.OverallHealth/lhb.maxHealth;
// 			var healthString  = "";
// 			if (healthFraction < 0.2f)
// 			{
// 				healthString = "heavily wounded.";
// 			}			
// 			else if (healthFraction < 0.6f)
// 			{
// 				healthString = "wounded.";
// 			}
// 			else
// 			{
// 				healthString = "in good shape.";
// 			}

// 			// On fire?
// 			if (lhb.FireStacks > 0)
// 			{
// 				healthString = "on fire!";
// 			}

// 			// Check if clicked Object has inventory and try and read ID card for profession.
// 			var idComponent = clickedObject.GetComponent<ItemStorage>()?.GetNamedItemSlot(NamedSlot.id)?.ItemObject?.GetComponent<IDCard>();
// 			string occupationString = "They have no ID.";
// 			if (idComponent)
// 			{
// 				pronoun = PlayerList.Instance.Get(lhb.gameObject).Script.characterSettings.PersonalPronoun();
// 				pronoun = pronoun.First().ToString().ToUpper() + pronoun.Substring(1);
// 				occupationString = pronoun + " is a " + idComponent.Occupation.DisplayName + "\n";
// 			}
			

// 			// Finally append message; shamelessly stolen from Health Scanner
// 			string examineResult = (pronoun + " is " + lhb.ConsciousState.ToString().ToLower() + ".\n"
// 		                 + pronoun + " is " + healthString + "\n"
// 		                 + occupationString);
// 			Chat.AddExamineMsgToClient(examineResult);

// 			return true;
// 		}


// 		        // Check if Attributes component is present, and call SendExamine on it 
//         var attributes = clickedObject.GetComponent<Attributes>();

//         if (attributes)
//         {
//             attributes.SendExamine();
//         	return true;
//         }