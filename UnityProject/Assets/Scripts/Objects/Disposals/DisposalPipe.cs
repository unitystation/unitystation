using System;
using System.Collections.Generic;
using UnityEngine;

namespace Disposals
{
	[CreateAssetMenu(fileName = "DisposalPipe", menuName = "Tiles/Disposal Pipe", order = 1)]
	public class DisposalPipe : BasicTile, IExaminable
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
		List<ConnectablePoint> _ConnectablePoints = new List<ConnectablePoint>();

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

		public string Examine(Vector3 worldPos = default)
		{
			return "It is wrenched to the floor and welded in place.";
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
