using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using Google.Protobuf.WellKnownTypes;
using NaughtyAttributes;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Animations;
using UnityEngine.Serialization;

namespace Pipes
{
	public class PipeItem : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		public PipeLayer PipeLayer = PipeLayer.Second;
		public CorePipeType PipeType;
		public bool NetCompatible = true;
		public Color Colour;
		//This is to be never rotated on items
		public Connections Connections = new Connections();

		public Connections OutConnections = new Connections();//Need to be rotated

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

			this.transform.Rotate(0, 0, 90);
		}

		public virtual void BuildPipe()
		{
			var searchVec = objectBehaviour.registerTile.LocalPosition;
			var Tile = (PipeTileSingleton.Instance.GetTile(Connections, PipeType));
			Logger.Log("Tile " + Tile);
			if (Tile != null)
			{
				Logger.Log("Building!!");
				int Offset = GetOffsetAngle(transform.localEulerAngles.z);
				Quaternion rot = Quaternion.Euler(0.0f, 0.0f,Offset );
				var Matrix = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one);
				objectBehaviour.registerTile.Matrix.AddUnderFloorTile(searchVec, Tile,Matrix,Colour);
				var checkPos = searchVec;
				checkPos.z = 0;
				var metaData = objectBehaviour.registerTile.Matrix.MetaDataLayer.Get(checkPos, true);
				var pipeNode = new PipeNode();

				pipeNode.Initialise(Tile, metaData, searchVec, objectBehaviour.registerTile.Matrix, this,Offset);
				metaData.PipeData.Add(pipeNode);
				Despawn.ServerSingle(this.gameObject);
			}

		}

		//Even how Funny it may be to have Oddly rotated tiles It would be a pain
		public int GetOffsetAngle(float z)
		{
			if (z > 255)
			{
				return (270);
			}
			else if (z > 135)
			{
				return (180);
			}
			else if (z > 45)
			{
				return (90);
			}
			else if (z > -45)
			{
				return (0);
			}
			else if (z > -135)
			{
				return (90);
			}
			else if (z < -135)
			{
				return (180);
			}

			return (0);
		}

		public virtual void Setsprite()
		{
		}
	}

	[System.Serializable]
	public class Connections
	{
		public ConnectAndType[] Directions = new ConnectAndType[4];

		public void Rotate(float InRotate)
		{
			if (InRotate == 0)
			{
			}
			else
			{
				if (InRotate > 181)
				{
					PipeOffset(3);
				}
				else if (InRotate > 91)
				{
					PipeOffset(2);
				}
				else
				{
					PipeOffset(1);
				}
			}
		}

		private void PipeOffset(int OffSet)
		{
			var NewConnectionss = new ConnectAndType[4];


			for (int i = 0; i < Directions.Length; i++)
			{
				int Location = i + OffSet;
				if (Location > 3)
				{
					Location -= 4;
				}

				NewConnectionss[Location] = Directions[i];
			}

			Directions = NewConnectionss;
		}

		public override string ToString()
		{
			string DD = "";
			for (int i = 0; i < Directions.Length; i++)
			{
				DD += ("," + Directions[i]);
			}


			return DD + "\n";
		}

		public static Connections CopyFrom(Connections InConnection)
		{
			var newone = new Connections();
			for (int i = 0; i < InConnection.Directions.Length; i++)
			{
				newone.Directions[i] = InConnection.Directions[i].Copy();
			}

			return (newone);
		}

		public Connections Copy()
		{
			return (CopyFrom(this));
		}
	}

	[System.Serializable]
	public class ConnectAndType
	{
		public bool Bool;

		[EnumFlags]
		public PipeType pipeType = PipeType.PipeRun;

		public OutputType OutputType = OutputType.None;

		public ConnectAndType Copy()
		{
			var Newone = new ConnectAndType();
			Newone.Bool = Bool;
			Newone.pipeType = pipeType;
			Newone.OutputType = OutputType;
			return (Newone);
		}
	}

	[System.Serializable]
	public class MixAndVolume
	{
		public float Volume = 50;
		public ReagentMix Mix = new ReagentMix();

		public float Density()
		{
			return (Mix.Total / Volume);
		}

		public void EqualiseWith(PipeData Another)
		{
			float TotalVolume = Volume + PipeFunctions.PipeOrNet(Another).Volume;
			float TotalReagents = Mix.Total + PipeFunctions.PipeOrNet(Another).Mix.Total;
			float TargetDensity = TotalReagents / TotalVolume;

			float thisAmount = TargetDensity * Volume;
			float AnotherAmount = TargetDensity * PipeFunctions.PipeOrNet(Another).Volume;

			if (thisAmount > Mix.Total)
			{
				PipeFunctions.PipeOrNet(Another).Mix.TransferTo(Mix, PipeFunctions.PipeOrNet(Another).Mix.Total-AnotherAmount);
			}
			else
			{
				this.Mix.TransferTo(PipeFunctions.PipeOrNet(Another).Mix, AnotherAmount- PipeFunctions.PipeOrNet(Another).Mix.Total);
			}
		}

		public void EqualiseWithMultiple( List<PipeData>  others)
		{
			float TotalVolume = Volume;
			foreach (var Pipe in others)
			{
				TotalVolume += PipeFunctions.PipeOrNet(Pipe).Volume;
			}

			float TotalReagents = Mix.Total;
			foreach (var Pipe in others)
			{
				TotalReagents += PipeFunctions.PipeOrNet(Pipe).Mix.Total;
			}
			float TargetDensity = TotalReagents / TotalVolume;


			foreach (var Pipe in others)
			{
				PipeFunctions.PipeOrNet(Pipe).Mix.TransferTo(Mix, PipeFunctions.PipeOrNet(Pipe).Mix.Total);
			}

			foreach (var Pipe in others)
			{
				Mix.TransferTo(PipeFunctions.PipeOrNet(Pipe).Mix,  TargetDensity*PipeFunctions.PipeOrNet(Pipe).Volume);
			}
		}

		public void EqualiseWithOutputs(List<PipeData> others)
		{
			bool DifferenceInPressure = false;
			float density = this.Density();
			foreach (var Pipe in others)
			{
				if (Math.Abs(Pipe.mixAndVolume.Density() - density) > 0.1f)
				{
					DifferenceInPressure = true;
					break;
				}
			}

			if (DifferenceInPressure)
			{
				if (others.Count > 1)
				{
					EqualiseWithMultiple(others);
				}
				else if (others.Count > 0)
				{
					EqualiseWith(others[0]);
				}
			}
		}

		public override string ToString()
		{
			return "Volume > " + Volume + " Mix > " + Mix.ToString();
		}
	}

	public enum PipeDirection
	{
		North = 0,
		East = 1,
		South = 2,
		West = 3,
	}

	[Flags]
	public enum PipeType
	{
		None = 0,
		PipeRun = 1 << 0,
		CoolingPipe = 1 << 1,
		//Used for stopping cooling pipes to connect to usual pipes
	}

	public enum CorePipeType //Would be nice to integrate into one at some point but Its hard to work out how to implement Liquids into a gas simulator : P
	{
		Unset,
		AtmosPipe,
		WaterPipe,
		//Stuff like vents and things
	}

	public enum PipeLayer
	{
		First = 0,
		Second = 1,
		Third = 2
	}


	//Used to keeping track of output connections
	public enum OutputType
	{
		None,
		Pumpout,
	}

}