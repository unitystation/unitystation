using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Main component for shuttle console
/// </summary>
public class ShuttleConsole : NBHandApplyInteractable
{
    public string interactionMessage;
    public MatrixMove ShuttleMatrixMove;

    public TabStateEvent OnStateChange;
    private TabState state = TabState.None;
    public TabState State
    {
	    get { return state; }
	    set
	    {
		    if ( state != value )
		    {
			    state = value;
			    OnStateChange.Invoke( value );
		    }
	    }
    }

    private void OnEnable()
    {
	    if ( ShuttleMatrixMove == null )
	    {
		    ShuttleMatrixMove = GetComponentInParent<MatrixMove>();

		    if ( ShuttleMatrixMove == null )
		    {
			    Logger.LogError( $"{this} has no reference to MatrixMove, didn't find any in parents either", Category.Matrix );
		    }
		    else
		    {
			    Logger.Log( $"No MatrixMove reference set to {this}, found {ShuttleMatrixMove} automatically", Category.Matrix );
		    }
	    }
    }


    protected override InteractionValidationChain<HandApply> InteractionValidationChain()
    {
	    return CommonValidationChains.CAN_APPLY_HAND_CONSCIOUS
		    .WithValidation(IsToolUsed.OfType(ToolType.Emag));
    }

    protected override void ServerPerformInteraction(HandApply interaction)
    {
	    //apply emag
	    if ( State == TabState.None )
	    {
		    State = TabState.Normal;
	    }
	    switch ( State )
	    {
		    case TabState.Normal:
			    State = TabState.Emagged;
				break;
		    case TabState.Emagged:
			    State = TabState.Off;
				break;
		    case TabState.Off:
			    State = TabState.Normal;
			    break;
	    }
    }

}
public enum TabState
{
	None, Normal, Emagged, Off
}
/// <inheritdoc />
/// "If you wish to use a generic UnityEvent type you must override the class type."
[Serializable]
public class TabStateEvent : UnityEvent<TabState>{}