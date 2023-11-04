using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Core.Threading;
using Logs;
using Tiles;

namespace Systems.Radiation
{
	public class RadiationThread : ThreadedBehaviour
	{
		private readonly HashSet<Vector2Int> CircleCircumference = new HashSet<Vector2Int>();
		private readonly HashSet<RadiationNode> CircleArea = new HashSet<RadiationNode>();

		private readonly List<RadiationPulse> WorkingPulseQueue = new List<RadiationPulse>();

		private readonly Stopwatch StopWatchlog = new Stopwatch();
		public override void ThreadedWork()
		{
			RadiationManager.Instance.sampler.Begin();
			PulsesRun();
			RadiationManager.Instance.sampler.End();
		}

		private void PulsesRun()
		{
			var pulseQueue = RadiationManager.Instance.PulseQueue;
			lock (pulseQueue)
			{
				WorkingPulseQueue.AddRange(pulseQueue);
				pulseQueue.Clear();
			}
			foreach (var radiationPulse in WorkingPulseQueue)
			{
				Pulse(radiationPulse);
			}
			WorkingPulseQueue.Clear();
		}

		private void Pulse(RadiationPulse Pulse)
		{
			StopWatchlog.Restart();

			//Radiation distance
			var radius = (int) Math.Round(Pulse.Strength / (Math.PI * 75));
			if (radius > 50)
			{
				radius = 50;
			}

			//Generates the conference
			circleBres(Pulse.Location.x, Pulse.Location.y, radius);

			//Radiation for each line ( Dont worry it stacks)
			var InitialRadiation = Pulse.Strength / CircleCircumference.Count;
			foreach (var ToPoint in CircleCircumference)
			{
				DrawRadiationLine(Pulse.Location.x, Pulse.Location.y, ToPoint.x, ToPoint.y, InitialRadiation, Pulse);
			}

			//Set values on tiles
			foreach (var NodePoint in CircleArea)
			{
				NodePoint.AddRadiationPulse(NodePoint.MidCalculationNumbers, DateTime.Now, Pulse.SourceID); //Drops off too quickly
				NodePoint.MidCalculationNumbers = 0;
			}

			CircleCircumference.Clear();
			CircleArea.Clear();

			StopWatchlog.Stop();
			Loggy.Log("StopWatchlog ElapsedMilliseconds time " + StopWatchlog.ElapsedMilliseconds, Category.Radiation);
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
			var RadiationOnStep = RadiationStrength;
			for (;;)
			{
				var WorldPSos = new Vector3Int(x0, y0, 0);
				var Matrix = MatrixManager.AtPoint(WorldPSos, CustomNetworkManager.IsServer);
				var Local = WorldPSos.ToLocal(Matrix.Matrix).RoundToInt();
				var NodePoint = Matrix.MetaDataLayer.Get(Local);
				var RadiationNode = NodePoint?.RadiationNode;
				if (RadiationNode != null)
				{
					foreach (var Layer in Matrix.MetaTileMap.Layers)
					{
						if (Layer.Key.IsUnderFloor()) continue;

						var basicTile = Matrix.MetaTileMap.GetTile(Local, Layer.Key) as BasicTile;
						if (basicTile != null)
						{
							RadiationOnStep *= basicTile.RadiationPassability;
						}
					}

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
	}
}