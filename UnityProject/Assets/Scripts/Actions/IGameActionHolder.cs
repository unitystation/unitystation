using System;
using System.Collections.Generic;
using Logs;
using Mirror;
using GameActions;
using UI.Core.Action;
using UnityEngine;

namespace GameActions
{
//You should only be implementing these interfaces on GameActionObject unless you have a very good reason not to
	/// <summary>
	/// The interface used for game actions
	/// </summary>
	public interface IGameActionHolder
	{
		GameObject gameObject { get; }

		/// <summary>
		/// The global key used for tracking an action, stored as a string for client communication
		/// 2 ACTIONS SHOULD NEVER EVER SHARE THE SAME KEY
		/// </summary>
		public string ActionGuid { get; }

		/// <summary>
		/// The container this action is inside
		/// </summary>
//	public IGameActionContainer ActionContainer { get; protected set; }
		/// <summary>
		/// The mind that owns this action
		/// </summary>
//	public Mind ActionOwner { get; protected set; }
		/// <summary>
		/// A ref to our UI button
		/// </summary>
		//public UIActionButton ActionButton { get; }
		/// <summary>
		/// The name of this action
		/// </summary>
//	public string ActionName { get; protected set; }
		/// <summary>
		/// The description of this action
		/// </summary>
//	public string ActionDesc { get; protected set; }
		/// <summary>
		/// The list of sprites that go inside our background
		/// </summary>
//	public List<SpriteDataSO> ActionSprites { get; protected set; }
		/// <summary>
		/// Our list of backgrounds
		/// </summary>
//	public List<SpriteDataSO> ActionBackgrounds { get; protected set; }
		ActionData ActionData { get; }

		struct ActionNetworkMessage : NetworkMessage
		{
			public string ReceivingActionGuid { get; set; }
			public string SerializedData { get; set; }
		}

		struct BasicUpdateStruct : IGameActionNetworkMessage
		{
			public string ReceivingActionGuid { get; set; }
		}

		virtual void SetUp()
		{
			if (CustomNetworkManager.IsServer)
			{
				NetworkServer.RegisterHandler<ActionNetworkMessage>(_SeverHandleReceivedMessage);
			}
			else
			{
				NetworkClient.RegisterHandler();
			}
		}

		virtual void CallActionClient() //clientside, this gets called when the UI button gets clicked by a player
		{
			UIActionButton action = UIActionManager.Instance.DicIActionGUI[this][0];
			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdRequestAction(ActionGuid, action.LastClickPosition);
		}

		/// <summary>
		/// Called whenever our UI button is clicked
		/// </summary>
		virtual void OnButtonClicked()
		{
		}

		#region Trigger Chain

		/// <summary>
		/// Generally checked before TriggerAction(), if your calling TriggerAction() you should probably check it too
		/// </summary>
		virtual bool IsAvailable()
		{
			return true;
		}

		/// <summary>
		/// Do the action chain, returns false if PreActivate() or Activate() fail
		/// </summary>
		virtual bool TriggerAction(RequestGameAction attatchedData, bool forced = false)
		{
			if (!PreActivate() && !forced) return false;
			if (!Activate() && !forced) return false;
			PostActivate();
			return true;
		}

		/// <summary>
		/// First in the action chain
		/// </summary>
		virtual bool PreActivate()
		{
			return true;
		}

		/// <summary>
		/// Second in the action chain, this is where you should put most of the logic to execute when your action is triggered
		/// </summary>
		virtual bool Activate()
		{
			return true;
		}

		/// <summary>
		/// Third in the action chain, the action will have counted as succeeded if it reaches this point
		/// generally you put things like starting cooldown here
		/// </summary>
		virtual void PostActivate()
		{
		}

		#endregion Trigger Chain

		#region Networking

		virtual void SendTriggerRequest()
		{
			try
			{
				return;
			}
			catch (Exception e)
			{
				Debug.LogError(e);
			}
		}

		virtual IGameActionRequestMessage GetRequestData(IGameActionRequestMessage passedMessage)
		{
			var request = Convert.ToBoolean(passedMessage) ? passedMessage : new BasicRequestStruct();
			request.ReceivingActionGuid = ActionGuid;
			request.AttemptTrigger = true;
			return request;
		}

		/// <summary>
		/// Action wrapper for SeverHandleReceivedMessage
		/// </summary>
		void _SeverHandleReceivedMessage(NetworkConnectionToClient conn, NetworkMessage msg)
		{
			SeverHandleReceivedMessage(conn, msg);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="allowTypeFailure">Should only be set to true when being used in overrides.
		/// Set to true to silence the output error upon not having a handler method for msg.</param>
		virtual bool SeverHandleReceivedMessage(NetworkConnectionToClient conn, NetworkMessage msg,
			bool allowTypeFailure = false)
		{
			if (msg is IGameActionRequestMessage)
			{
				HandleTriggerRequest((IGameActionRequestMessage)msg);
				return true;
			}
			else if (allowTypeFailure) return false;

			Loggy.Error(
				$":IGameActionHolder::HandleReceivedMessage: was passed a NetworkMessage[{msg}] type it does not have a handler for.",
				Category.Actions);
			return false;
		}



		virtual bool HandleTriggerRequest(IGameActionRequestMessage msg)
		{
			return true;
		}

		#endregion Networking
	}

	/// <summary>
	/// Actions with this interface have some kind of cooldown associated with their use
	/// </summary>
	public interface ICooldownGameActionHolder<S, C> : ICooldown, IGameActionHolder
		where S : struct, IGameActionRequestMessage
		where C : struct, IGameActionRequestMessage
	{
		/// <summary>
		/// How long is the cooldown of this action
		/// </summary>
		int CooldownTime { get; set; }

		virtual bool StartCooldown()
		{
			//Cooldowns.TryStartServer(sentByPlayer.Script, this, CooldownTime);
			return true;
		}
	}

	/// <summary>
	/// Actions with this interface have an on and an off state they can swap between
	/// </summary>
	public interface IToggleGameActionHolder<S, C> : IGameActionHolder
		where S : struct, IGameActionRequestMessage
		where C : struct, IGameActionRequestMessage
	{
		/// <summary>
		/// The current toggle state of this action
		/// </summary>
		bool Active { get; set; }

		/// <summary>
		/// Does our owner select a target for us by clicking on something
		/// </summary>
		bool Targeted { get; set; }

		/// <summary>
		/// What happens when we are toggled on
		/// </summary>
		virtual void ToggleOn()
		{
		}

		/// <summary>
		/// What happens when we are toggled off
		/// </summary>
		virtual void ToggleOff()
		{
		}
	}

	/// <summary>
	/// Actions with this interface have a limited number of charges
	/// </summary>
	public interface IChargeGameActionHolder<S, C> : IGameActionHolder
		where S : struct, IGameActionRequestMessage
		where C : struct, IGameActionRequestMessage
	{
		/// <summary>
		/// How many charges do we currently have
		/// </summary>
		int Charges { get; set; }

		/// <summary>
		/// What is the maximum amount of charges we can have
		/// if you don't have a way to regenerate charges you likely don't care about this
		/// </summary>
		int MaxCharges { get; set; }
	}
}
//some example classes
/*
public class __ExampleIActionGUI__ : IGameActionHolder
{
	[SerializeField]
	private ActionData actionData = null;
	public ActionData ActionData => actionData;
	public int ActionKey => UIActionManager.RegisterAction(this);

	public void CallActionClient()
	{
		Do whatever you want
	}
}

public class __ExampleIServerActionGUI__ : IGameActionHolder
{
	[SerializeField]
	private ActionData actionData = null;
	public ActionData ActionData => actionData;

	public void CallActionClient()
	{
		Do whatever you want
		Remember if its networked do validation
	}

	public void CallActionServer(PlayerInfo playerInfo)
	{
		Validation
		do Action
	}
}*/
