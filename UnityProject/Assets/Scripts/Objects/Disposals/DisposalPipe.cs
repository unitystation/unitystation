using System;
using System.Collections.Generic;
using Core;
using UnityEngine;
using Systems.DisposalPipes;
using TileManagement;
using Tiles;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

namespace Objects.Disposals
{
	[CreateAssetMenu(fileName = "DisposalPipe", menuName = "Tiles/Disposal Pipe", order = 1)]
	public class DisposalPipe : FuncPlaceRemoveTile, IExaminable
	{
		[Tooltip("Set the type of disposal pipe this is.")]
		public DisposalPipeType PipeType;

		[Tooltip("Set the orientation for the DisposalPipeObject to use as it is spawned when this pipe is deconstructed.")]
		public OrientationEnum DisposalPipeObjectOrientation;

		[Tooltip("Set the sprite this particular disposal pipe should use.")]
		public Sprite sprite;

		public override Sprite PreviewSprite => sprite;

		[SerializeField]
		[Tooltip("Set the sides available for connecting to other disposal pipes.")]
		private List<ConnectablePoint> _ConnectablePoints = new List<ConnectablePoint>();

		Dictionary<OrientationEnum, DisposalPipeConnType> connectablePoints;
		public Dictionary<OrientationEnum, DisposalPipeConnType> ConnectablePoints {
			get {
				if (connectablePoints == null)
				{
					connectablePoints = new Dictionary<OrientationEnum, DisposalPipeConnType>();
					foreach (ConnectablePoint side in _ConnectablePoints)
					{
						connectablePoints.Add(side.Side, side.Type);
					}
				}

				return connectablePoints;
			}
		}

		[Serializable]
		struct ConnectablePoint
		{
			public OrientationEnum Side;
			public DisposalPipeConnType Type;
			private ConnectablePoint((OrientationEnum side, DisposalPipeConnType type) pair)
			{
				Side = pair.side;
				Type = pair.type;
			}

			public static implicit operator ConnectablePoint((OrientationEnum side, DisposalPipeConnType type) pair)
			{
				return new ConnectablePoint(pair);
			}
		}

		public void InitialiseNode(Vector3Int Location, Matrix matrix)
		{
			var ZeroedLocation = new Vector3Int(x: Location.x, y: Location.y, 0);
			var metaData = matrix.MetaDataLayer.Get(ZeroedLocation);
			var disPipeNode = new DisposalPipeNode();
			disPipeNode.Initialise(this, Location);
			metaData.DisposalPipeData.Add(disPipeNode);
		}


		public string Examine(Vector3 worldPos = default)
		{
			return "It is wrenched to the floor and welded in place.";
		}

		public override void OnPlaced(Vector3Int TileLocation, Matrix AssociatedMatrix, TileLocation tileLocation)
		{
			InitialiseNode(TileLocation, AssociatedMatrix);
		}

		public override void OnRemoved(Vector3Int TileLocation, Matrix AssociatedMatrix, TileLocation tileLocation, bool SpawnItems)
		{
			var Node = AssociatedMatrix.MetaDataLayer.Get(TileLocation, false);
			if (Node != null)
			{
				DisposalPipeNode IndividualNode = null;
				foreach (var DPN in Node.DisposalPipeData)
				{
					if (DPN.DisposalPipeTile == this)
					{
						IndividualNode = DPN;
						break;
					}
				}
				Node.DisposalPipeData.Remove(IndividualNode);

				// Spawn pipe GameObject
				if (this.SpawnOnDeconstruct == null) return;

				if (SpawnItems)
				{
					var spawn = Spawn.ServerPrefab(this.SpawnOnDeconstruct, TileLocation.ToWorld(AssociatedMatrix));
					if (spawn.Successful == false) return;

					if (spawn.GameObject.TryGetComponent<Rotatable>(out var Rotatable))
					{
						Rotatable.FaceDirection(this.DisposalPipeObjectOrientation);
					}

					if (spawn.GameObject.TryGetComponent<UniversalObjectPhysics>(out var behaviour))
					{
						behaviour.SetIsNotPushable(true);
					}
				}

			}
		}
	}

	public enum DisposalPipeType
	{
		Basic,
		Terminal,
		Merger,
		Splitter,
	}

	public enum DisposalPipeConnType
	{
		Input,
		Output,
		InputOutput,
	}
}
