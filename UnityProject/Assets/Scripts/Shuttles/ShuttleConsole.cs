using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Main component for shuttle console
/// </summary>
public class ShuttleConsole : NBHandApplyInteractable
{
	public MatrixMove ShuttleMatrixMove;
	private RegisterTile registerTile;

    public TabStateEvent OnStateChange;
    private TabState state = TabState.Normal;
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

    private void Awake()
    {
	    if ( !registerTile )
	    {
		    registerTile = GetComponent<RegisterTile>();
	    }
    }

    private void OnEnable()
    {
	    if ( ShuttleMatrixMove == null )
	    {
		    StartCoroutine( InitMatrixMove() );
	    }
    }

    private IEnumerator InitMatrixMove()
    {
	    ShuttleMatrixMove = GetComponentInParent<MatrixMove>();

        if ( ShuttleMatrixMove == null )
        {
            while ( !registerTile.Matrix )
            {
                yield return WaitFor.EndOfFrame;
            }
            ShuttleMatrixMove = MatrixManager.Get( registerTile.Matrix ).MatrixMove;
        }

        if ( ShuttleMatrixMove == null )
        {
            Logger.LogError( $"{this} has no reference to MatrixMove, current matrix doesn't seem to have it either", Category.Matrix );
        }
        else
        {
            Logger.Log( $"No MatrixMove reference set to {this}, found {ShuttleMatrixMove} automatically", Category.Matrix );
        }
    }


    protected override bool WillInteract(HandApply interaction, NetworkSide side)
    {
	    if (!base.WillInteract(interaction, side)) return false;
	    //can only be interacted with an emag (normal click behavior is in HasNetTab)
	    if (!Validations.IsTool(interaction.UsedObject, ToolType.Emag)) return false;
	    return true;
    }

    protected override void ServerPerformInteraction(HandApply interaction)
    {
	    //apply emag
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
	Normal, Emagged, Off
}
/// <inheritdoc />
/// "If you wish to use a generic UnityEvent type you must override the class type."
[Serializable]
public class TabStateEvent : UnityEvent<TabState>{}