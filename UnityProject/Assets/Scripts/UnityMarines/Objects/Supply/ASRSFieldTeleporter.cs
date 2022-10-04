using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;
using Gateway;

namespace Objects.TGMC
{

	public class ASRSFieldTeleporter : NetworkBehaviour
	{
		private RegisterTile registerTile;

		private Matrix Matrix => registerTile.Matrix;

		private SpriteHandler spriteHandler;

		private bool doingAnimation;

		[field: SerializeField]
		public int RequiredCredits{ get; private set; }

		[field: SerializeField]
		public float CoolDown { get; private set; }


		[Server]
		private void ServerSync(bool newVar)
		{
			doingAnimation = newVar;
		}

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
		}

		private void Start()
		{
			spriteHandler.ChangeSprite(0);
		}

		public void TeleportToBeacon(Vector3 beaconCoords)
		{

			var registerTileLocation = registerTile.LocalPositionServer;

			IEnumerable<UniversalObjectPhysics> objectsOnTile = Matrix.Get<UniversalObjectPhysics>(registerTileLocation, ObjectType.Object, true); //For stuff like closets and crates
			IEnumerable<UniversalObjectPhysics> itemsOnTile = Matrix.Get<UniversalObjectPhysics>(registerTileLocation, ObjectType.Item, true); //For individual items, not incluiding items sotred in crates as they at 0,0

			foreach (UniversalObjectPhysics objectPhysics in objectsOnTile.Concat(itemsOnTile))
			{
				if (objectPhysics.gameObject == gameObject) continue; //Don't teleport this object

				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.StealthOff, gameObject.RegisterTile().WorldPosition);
				TransportUtility.TransportObjectAndPulled(objectPhysics, beaconCoords);
			}

			if (!doingAnimation)
			{
				ServerSync(true);

				StartCoroutine(ServerAnimation());
			}
		}

		public IEnumerator ServerAnimation()
		{
			spriteHandler.ChangeSprite(1);
			yield return WaitFor.Seconds(1f);
			spriteHandler.ChangeSprite(0);
			ServerSync(false);
		}
	}
}
