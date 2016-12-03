using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS.Atmospherics {
	public class Graph {
		private List<Aperture> edges;
		private List<Area> vertices;

		public Graph( List<Aperture> e, List<Area> v) {
			e.CopyTo(edges);
			v.CopyTo(vertices);
		}
	}

	public class Area {
		//Pressure measured in bar
		public float pressure = 1.0f;
		public float volume = 1.0f;
		public bool breathable = true; //TODO change this for something more realistic

		public Area(float p, float v, bool b) {
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
	}		
}