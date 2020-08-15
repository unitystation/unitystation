using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

namespace Radiation
{
	public class RadiationManager : MonoBehaviour
	{
		public List<RadiationPulse> PulseQueue = new List<RadiationPulse>();
		private List<RadiationPulse> WorkingPulseQueue = new List<RadiationPulse>();

		public static RadiationManager Instance
		{
			get
			{
				if (instance == null)
				{
					instance = FindObjectOfType<RadiationManager>();
				}

				return instance;
			}
			set { instance = value; }
		}

		private static RadiationManager instance;

		public bool Running { get; private set; }
		public float MSSpeed = 100;

		private void OnApplicationQuit()
		{
			StopSim();
		}

		void OnEnable()
		{
			EventManager.AddHandler(EVENT.RoundStarted, StartSim);
			EventManager.AddHandler(EVENT.RoundEnded, StopSim);
		}

		void OnDisable()
		{
			EventManager.RemoveHandler(EVENT.RoundStarted, StartSim);
			EventManager.RemoveHandler(EVENT.RoundEnded, StopSim);
		}

		public void StopSim()
		{
			if (!CustomNetworkManager.Instance._isServer) return;

			Running = false;
			Reset();
		}

		public void StartSim()
		{
			if (!CustomNetworkManager.Instance._isServer) return;

			Running = true;
			SetSpeed((int) MSSpeed);
			if (!running)
			{
				running = true;
				thread = new Thread(Run);
				thread.Start();
			}
		}

		private bool running;

		private Stopwatch StopWatch = new Stopwatch();
		private Stopwatch StopWatchlog = new Stopwatch();

		private int MillieSecondDelay;

		private CustomSampler sampler;

		public Thread thread;

		RadiationManager()
		{
			sampler = CustomSampler.Create("RadiationUpdate");
		}

		public void SetSpeed(int inMillieSecondDelay)
		{
			MillieSecondDelay = inMillieSecondDelay;
		}

		public void PulsesRun()
		{
			lock (PulseQueue)
			{
				WorkingPulseQueue.AddRange(PulseQueue);
				PulseQueue.Clear();
			}
			for (int i = 0; i < WorkingPulseQueue.Count; i++)
			{
				Pulse(WorkingPulseQueue[i]);
			}

			WorkingPulseQueue.Clear();
		}

		private void Run()
		{
			Profiler.BeginThreadProfiling("Unitystation", "Radiation");
			while (running)
			{
				sampler.Begin();
				StopWatch.Restart();
				PulsesRun();
				StopWatch.Stop();
				sampler.End();
				if (StopWatch.ElapsedMilliseconds < MillieSecondDelay)
				{
					Thread.Sleep(MillieSecondDelay - (int) StopWatch.ElapsedMilliseconds);
				}
			}

			Profiler.EndThreadProfiling();
			thread.Abort();
		}

		public void Reset()
		{
		}

		private HashSet<Vector2Int> CircleCircumference = new HashSet<Vector2Int>();
		private HashSet<RadiationNode> CircleArea = new HashSet<RadiationNode>();

		private void Pulse(RadiationPulse Pulse)
		{
			StopWatchlog.Restart();

			//Radiation distance
			int Radius = (int) Math.Round(Pulse.Strength/(Math.PI*75));
			if (Radius > 50)
			{
				Radius = 50;
			}

			//Logger.Log("Radius > " + Radius);
			//Generates the conference
			circleBres(Pulse.Location.x, Pulse.Location.y, Radius);

			//Logger.Log("CircleCircumference.Count > " + CircleCircumference.Count);
			//Logger.Log("Pulse.Strengt> " + Pulse.Strength);
			//Radiation for each line ( Dont worry it stacks)
			float InitialRadiation = Pulse.Strength / CircleCircumference.Count;
			foreach (var ToPoint in CircleCircumference)
			{
				DrawRadiationLine(Pulse.Location.x, Pulse.Location.y, ToPoint.x, ToPoint.y, InitialRadiation, Pulse);
			}

			//Logger.Log("CircleArea.Count > " + CircleArea.Count);
			//Set values on tiles
			foreach (var NodePoint in CircleArea)
			{

				NodePoint.AddRadiationPulse(NodePoint.MidCalculationNumbers, DateTime.Now, Pulse.SourceID); //Drops off too quickly
				NodePoint.MidCalculationNumbers = 0;
				//Logger.Log("rad onv " + NodePoint.RadiationLevel);
			}

			CircleCircumference.Clear();
			CircleArea.Clear();

			StopWatchlog.Stop();
			Logger.Log("StopWatchlog ElapsedMilliseconds time " + StopWatchlog.ElapsedMilliseconds);
		}

		//https://www.geeksforgeeks.org/bresenhams-circle-drawing-algorithm/
		// Function for circle-generation
		// using Bresenham's algorithm
		void circleBres(int xc, int yc, int r)
		{
			int x = 0, y = r;
			int d = 3 - 2 * r;
			drawCircle(xc, yc, x, y);
			while (y >= x)
			{
				// for each pixel we will
				// draw all eight pixels

				x++;

				// check for decision parameter
				// and correspondingly
				// update d, x, y
				if (d > 0)
				{
					y--;
					d = d + 4 * (x - y) + 10;
				}
				else
					d = d + 4 * x + 6;

				drawCircle(xc, yc, x, y);
				//delay(50);
			}
		}

		// Function to put Locations
		// at subsequence points
		void drawCircle(int xc, int yc, int x, int y)
		{
			CircleCircumference.Add(new Vector2Int(xc + x, yc + y));
			CircleCircumference.Add(new Vector2Int(xc - x, yc + y));
			CircleCircumference.Add(new Vector2Int(xc + x, yc - y));
			CircleCircumference.Add(new Vector2Int(xc - x, yc - y));
			CircleCircumference.Add(new Vector2Int(xc + y, yc + x));
			CircleCircumference.Add(new Vector2Int(xc - y, yc + x));
			CircleCircumference.Add(new Vector2Int(xc + y, yc - x));
			CircleCircumference.Add(new Vector2Int(xc - y, yc - x));
		}

		//https://rosettacode.org/wiki/Bitmap/Bresenham%27s_line_algorithm#C.23
		void DrawRadiationLine(int x0, int y0, int x1, int y1, float RadiationStrength, RadiationPulse Pulse)
		{
			int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
			int dy = Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
			int err = (dx > dy ? dx : -dy) / 2, e2;
			float RadiationOnStep = RadiationStrength;
			for (;;)
			{
				var NodePoint = Pulse.Matrix.GetMetaDataNode(new Vector2Int(x0, y0));
				var RadiationNode = NodePoint?.RadiationNode;
				if (RadiationNode != null)
				{
					if (NodePoint.IsOccupied)
					{
						RadiationOnStep *= 0.15f;
					}
					//RadiationOnStep *= RadiationNode.RadiationPassability;

					CircleArea.Add(RadiationNode);
					RadiationNode.MidCalculationNumbers += RadiationOnStep;
				}
				if (x0 == x1 && y0 == y1) break;
				e2 = err;
				if (e2 > -dx)
				{
					err -= dy;
					x0 += sx;
				}

				if (e2 < dy)
				{
					err += dx;
					y0 += sy;
				}
			}
		}


		public void RequestPulse(Matrix Matrix, Vector3Int Location, float Strength, int InSourceID)
		{
			lock (PulseQueue)
			{
				PulseQueue.Add(new RadiationPulse(Matrix, Location, Strength, InSourceID));
			}
		}

		public struct RadiationPulse
		{
			public Matrix Matrix;
			public Vector3Int Location;
			public float Strength;
			public int SourceID;

			public RadiationPulse(Matrix InMatrix, Vector3Int InLocation, float InStrength, int InSourceID)
			{
				Matrix = InMatrix;
				Location = InLocation;
				Strength = InStrength;
				SourceID = InSourceID;
			}
		}
	}
}