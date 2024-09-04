using System.Collections;
using Core;
using Mirror;
using UI.Action;
using UnityEngine;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

namespace Items.Others
{
	public class JumpBoots : NetworkBehaviour, IServerInventoryMove
	{
		private ItemActionButton actionButton;
		private PlayerScript playerScript;
		
		[SerializeField] private float jumpCooldown = 6;
		[SerializeField] private float jumpSpeed = 12;
		[SerializeField] private float jumpAirTime = 0.25f;
		
		private bool isJumping;

		private void Awake()
		{
			actionButton = GetComponent<ItemActionButton>();
		}

		private void OnEnable()
		{
			actionButton.ServerActionClicked += Jump;
		}

		private void OnDisable()
		{
			actionButton.ServerActionClicked -= Jump;
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (info.ToPlayer != null)
			{
				playerScript = info.ToPlayer.PlayerScript;
			}
			else if (info.FromPlayer != null)
			{
				playerScript = info.FromPlayer.PlayerScript;
			}
		}
		
		private IEnumerator JumpCooldown()
		{
			isJumping = true;
			yield return WaitFor.Seconds(jumpCooldown);
			isJumping = false;
		}
		
		//If the player is moving in the air at a speed greater then default human runspeed they get stuned when they land as OnImpact is called
		//So to make this actually usable at reasonable speeds this awful hacky bullshit is required
		public void OnLanding(UniversalObjectPhysics obj, Vector2 newtonian)
		{
			obj.OnImpact.RemoveListener(OnLanding);
			StartCoroutine(EndOfFrameDoImpactVomit(obj));
		}
		
		private IEnumerator EndOfFrameDoImpactVomit(UniversalObjectPhysics obj)
		{
			yield return WaitFor.EndOfFrame;
			obj.DoImpactVomit = true;
		}
		
		private void Jump()
		{
			if (isJumping)
			{
				Chat.AddExamineMsgFromServer(playerScript.PlayerInfo, $"The {gameObject.ExpensiveName()}'s internal propulsion needs to recharge still!");
			}
			else
			{
				var dir = playerScript.CurrentDirection;
				StartCoroutine(JumpCooldown());
				var obj = playerScript.ObjectPhysics;
				obj.DoImpactVomit = false;
				obj.OnImpact.AddListener(OnLanding);
				obj.NewtonianPush(dir.ToLocalVector2Int(), jumpSpeed, jumpAirTime, 0);
				Chat.AddActionMsgToChat(playerScript.GameObject, "You dash forwards into the air!", $"{playerScript.GameObject.ExpensiveName()} dashes forwards into the air!");
			}
		}
	}
}
