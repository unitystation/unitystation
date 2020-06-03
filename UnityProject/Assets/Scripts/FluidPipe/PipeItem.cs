using UnityEngine;

namespace Pipes
{
	public class PipeItem : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		public Color Colour;
		//This is to be never rotated on items
		public PipeTile pipeTile;
		public PipeActions PipeAction;

		public SpriteHandler SpriteHandler;
		public ObjectBehaviour objectBehaviour;

		private void Awake()
		{
			SpriteHandler = this.GetComponentInChildren<SpriteHandler>();
			objectBehaviour = this.GetComponent<ObjectBehaviour>();
		}

		[RightClickMethod]
		public void Dothing()
		{
			Logger.Log("transform.localRotation  " +  transform.localRotation);
		}

		public virtual bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.TargetObject != gameObject) return false;
			if (interaction.HandObject == null) return false;
			return true;
		}

		public virtual void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wrench))
			{
				BuildPipe();
				return;
			}

			this.transform.Rotate(0, 0, -90);
		}

		public virtual void BuildPipe()
		{
			var searchVec = objectBehaviour.registerTile.LocalPosition;
			var Tile = (GetPipeTile());
			Logger.Log("Tile " + Tile);
			if (Tile != null)
			{
				Logger.Log("Building!!");
				int Offset = PipeFunctions.GetOffsetAngle(transform.localEulerAngles.z);
				Quaternion rot = Quaternion.Euler(0.0f, 0.0f,Offset );
				var Matrix = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one);
				objectBehaviour.registerTile.Matrix.AddUnderFloorTile(searchVec, Tile,Matrix,Colour);
				Tile.InitialiseNode(searchVec,objectBehaviour.registerTile.Matrix);
				Despawn.ServerSingle(this.gameObject);
			}

		}

		public virtual void Setsprite()
		{
		}

		public virtual PipeTile GetPipeTile()
		{
			return (pipeTile);
		}

	}

}