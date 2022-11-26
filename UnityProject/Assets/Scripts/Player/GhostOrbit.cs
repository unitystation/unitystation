using System.Threading.Tasks;
using Objects;
using UnityEngine;
using Mirror;


namespace Player
{
	public class GhostOrbit : NetworkBehaviour
	{

		public static GhostOrbit Instance;

		[SyncVar(hook = nameof(SyncOrbitObject))]
		private NetworkIdentity idtarget;

		private GameObject Target
		{
			get => idtarget.OrNull()?.gameObject;
			set => SyncOrbitObject(idtarget, value.NetWorkIdentity());
		}


		[SerializeField] private GhostMove ghostMove;
		[SerializeField] private RotateAroundTransform rotateTransform;

		/// <summary>
		/// Time in milliseconds! The time between mouse clicks where we can orbit an object
		/// </summary>
		private readonly int doubleClickTime = 500;
		private bool hasClicked = false;

		private Mind mind;
		private void Start()
		{
			if (ghostMove == null) ghostMove = GetComponent<GhostMove>();
			if (rotateTransform == null) rotateTransform = GetComponent<RotateAroundTransform>();
			mind =  GetComponent<Mind>();

			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			Instance = this;
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);

			if(CustomNetworkManager.IsServer == false) return;
			StopOrbiting();
		}

		private void SyncOrbitObject(NetworkIdentity oldObject, NetworkIdentity newObject)
		{
			idtarget = newObject;

			if (Target == null)
			{
				ResetRotate();
				return;
			}

			rotateTransform.TransformToRotateAround = Target.transform;
		}

		private void UpdateMe()
		{
			if(hasAuthority == false || mind.IsGhosting == false) return;

			if (Input.GetMouseButtonDown(0))
			{
				if (hasClicked == false)
				{
					DoubleClickTimer();
					return;
				}
				FindObjectToOrbitUnderMouse();
			}
			if (KeyboardInputManager.IsMovementPressed())
			{
				CmdStopOrbiting();
			}
		}

		private void FindObjectToOrbitUnderMouse()
		{
			var possibleTargets = MouseUtils.GetOrderedObjectsUnderMouse();
			foreach (var possibleTarget in possibleTargets)
			{
				if (possibleTarget.TryGetComponent<UniversalObjectPhysics>(out var pull) || possibleTarget.TryGetComponent<Singularity>(out var loose))
				{
					CmdServerOrbit(possibleTarget);
					return;
				}
			}
		}

		private async void DoubleClickTimer()
		{
			hasClicked = true;
			await Task.Delay(doubleClickTime).ConfigureAwait(false);
			hasClicked = false;
		}

		[Server]
		private void Orbit(GameObject thingToOrbit)
		{
			if(thingToOrbit == null) return;
			Target = thingToOrbit;

			var worldMove = Target.AssumedWorldPosServer();
			var matrix = MatrixManager.AtPoint(worldMove, isServer);
			ghostMove.ForcePositionClient( worldMove.ToLocal(matrix), matrix.Id, OrientationEnum.Down_By180);

			UpdateManager.Add(FollowTarget, 0.1f);
			Chat.AddExamineMsg(gameObject, $"You start orbiting {thingToOrbit.ExpensiveName()}");
		}

		[Server]
		private void StopOrbiting()
		{
			if(Target == null) return;

			Chat.AddExamineMsg(gameObject, $"You stop orbiting {Target.ExpensiveName()}");
			Target = null;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, FollowTarget);
			ResetRotate();
		}

		private void ResetRotate()
		{
			rotateTransform.TransformToRotateAround = null;

			var rotateTransformCache = rotateTransform.transform;
			rotateTransformCache.up = Vector3.zero;
			rotateTransformCache.localPosition = Vector3.zero;
		}

		[Command]
		public void CmdStopOrbiting()
		{
			if(Target == null) return;
			StopOrbiting();
		}

		/// <summary>
		/// Mirror does not support IEnumerable so we cannot turn the FindObjectToOrbit function into a command.
		/// </summary>
		[Command]
		public void CmdServerOrbit(GameObject thingToOrbit)
		{
			Orbit(thingToOrbit);
		}

		//This function is only really here to make sure the server keeps the ghost tile position correct
		//TODO: Might be worth changing this to be called from the target CNT OnTileReached instead?
		private void FollowTarget()
		{
			if (Target == null) return;


			var worldMove = Target.AssumedWorldPosServer();
			var matrix = MatrixManager.AtPoint(worldMove, isServer);
			ghostMove.ForcePositionClient( worldMove.ToLocal(matrix), matrix.Id, OrientationEnum.Down_By180);



			if (Target.AssumedWorldPosServer() == TransformState.HiddenPos)
			{
				//In closet so cancel orbit for clients
				StopOrbiting();
			}
		}
	}
}
