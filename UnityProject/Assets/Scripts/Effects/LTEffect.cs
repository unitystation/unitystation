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

	[Command(requiresAuthority = false)]
	public virtual void CmdStartAnimation()
    {
        tween.isAnim = true;
    }

	[ClientRpc]
	public virtual void RpcStartAnim()
	{
		tween.isAnim = true;
	}

	[Command(requiresAuthority = false)]
	public virtual void CmdStopAnimation()
    {
        tween.CmdCancelObject(this.gameObject, false);
        tween.isAnim = false;
    }

	[ClientRpc]
	public virtual void RpcStopAnim()
	{
		tween.CmdCancelObject(this.gameObject, false);
		tween.isAnim = false;
	}

    public void getOriginalPosition()
    {
        originalPosition = transform.position;
    }
}