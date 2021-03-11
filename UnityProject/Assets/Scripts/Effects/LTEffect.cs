using UnityEngine;
using Mirror;

public class LTEffect : NetworkBehaviour 
{
    /// <summary>
    /// The base class for all animated effects that requires LeanTween to sync between all clients.
    /// </summary>

    [HideInInspector]
    public Vector3 originalPosition;

    [Tooltip("Which Axis will the animation play on?")]
    public NetworkedLeanTween.Axis axisMode = NetworkedLeanTween.Axis.X;

    [SerializeField, Tooltip("Do you want to animate the entire gameObject or just the sprite?")]
    public AnimMode animType = AnimMode.SPRITE;

    [Tooltip("The sprite gameObject that will be used for the animation.")]
    public Transform spriteReference;

    public NetworkedLeanTween tween;

    [HideInInspector]
    public enum AnimMode
    {
        SPRITE,
        GAMEOBJECT
    }


    private void Awake()
    {
       getOriginalPosition();
    }

	public virtual void StopAnimation()
    {
        tween.RpcCancelObject(this.gameObject, false);
    }

	[ClientRpc]
	public virtual void RpcStopAnim()
	{
		tween.RpcCancelObject(this.gameObject, false);
	}

    public void getOriginalPosition()
    {
        originalPosition = transform.position;
    }
}