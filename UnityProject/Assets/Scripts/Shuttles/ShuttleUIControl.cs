using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShuttleUIControl : MonoBehaviour
{
    public MatrixMove matrixMove;
    private float maxSpeed = 0;
    private bool active = false;

    /// <summary>
    /// Starts or stops the shuttle.
    /// </summary>
    /// <param name="off">Toggle parameter</param>
    public void TurnOnOff(bool off)
    {
        if(matrixMove != null && !off)
        {
            maxSpeed = matrixMove.maxSpeed;
        }
        else
        {
            
            maxSpeed = 0;
        }
        matrixMove.ToggleMovement();
    }

    /// <summary>
    /// Turns the shuttle right.
    /// </summary>
    public void TurnRight()
    {
        if(matrixMove != null)
            matrixMove.TryRotate(true);
    }

    /// <summary>
    /// Turns the shuttle left.
    /// </summary>
    public void TurnLeft()
    {
        if(matrixMove != null)
            matrixMove.TryRotate(false);
    }

    /// <summary>
    /// Sets shuttle speed.
    /// </summary>
    /// <param name="speedMultiplier"></param>
    public void SetSpeed(float speedMultiplier)
    {
        float speed = speedMultiplier*(maxSpeed-1) + 1;
        if(matrixMove != null)
            matrixMove.SetSpeed(speed);
    }
    
}
