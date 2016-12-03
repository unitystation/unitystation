using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS.Atmospherics {
	public class Graph : MonoBehaviour{
		private List<Aperture> edges;
		private Hashtable vertices;

		public Graph( List<Aperture> e, Hashtable v) {
			edges = e;
			vertices = v;
		}

		//TODO ensure there's fallback if the tick takes too long to process
		public void PerformSimulationTick() {
			IEnumerator coroutine = SimulationTick(edges, vertices);
			StartCoroutine(coroutine);
		}

		private IEnumerator SimulationTick(List<Aperture> edges, Hashtable vertices) {
			//Step 1: Iterate through all apetures, calculate rate of flow for both sides
			Hashtable toProcess = new Hashtable();
			//TODO can we make this parallel too?
			foreach (Aperture edge in edges) {
				edge.PerformFlowCalcluation(toProcess);
			}

			//Step 2: Iterate through all effected areas, apply flow from all affecting apertures
			foreach (int id in toProcess.Keys) {
				Area currentArea = (Area)vertices[id];
				currentArea.pressure = currentArea.pressure + (float)toProcess[id];
			}
			yield return null;
		}
	}

	public class Area {
		public int id;
		//Pressure measured in bar
		public float pressure = 1.0f;
		public float volume = 1.0f;
		public bool breathable = true; //TODO change this for something more realistic

		public Area(int i, float p, float v, bool b) {
			id = i;
			pressure = p;
			volume = v;
			breathable = b;
		}
	}

	public class Aperture {
		private Area left;
		private Area right;
		private float width;

		public Aperture(Area l, Area r, float w) {
			left = l;
			right = r;
			width = w;
		}

		public void PerformFlowCalcluation(Hashtable toProcess) {
			//TODO replace with heat equation
			float flow = 1.0f;
			if (left.pressure >= right.pressure) {
				AddToHashTable(toProcess, left, -flow);
				AddToHashTable(toProcess, right, flow);
			} else {
				AddToHashTable(toProcess, left, flow);
				AddToHashTable(toProcess, right, -flow);
			}
		}

		private void AddToHashTable(Hashtable toProcess, Area area, float flow) {
			if (toProcess.ContainsKey(area.id)) {
				Area currentArea = (Area)toProcess[area.id];
				toProcess[area.id] = currentArea.pressure + flow;
			} else {
				toProcess.Add(area.id, flow);
			}
		}
	}		
}