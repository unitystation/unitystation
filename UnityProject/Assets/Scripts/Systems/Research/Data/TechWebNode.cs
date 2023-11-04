using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace Systems.Research.Data
{
	[Serializable]
	public class Technology
	{
		public String ID;

		public String DisplayName;

		public String Description;

		/// <summary>
		/// A bool to tell whether or not it should be already researched on load
		/// </summary>
		public bool StartingNode;

		/// <summary>
		/// What technologies "ID" are required to be able to research this Technology
		/// </summary>
		public List<String> RequiredTechnologies;

		/// <summary>
		/// What designs it unlocks as ID
		/// </summary>
		public List<String> DesignIDs;


		/// <summary>
		/// How much it costs to research
		/// </summary>
		public int ResearchCosts;

		/// <summary>
		/// How much money you get from exporting the data disk with it on
		/// </summary>
		public int ExportPrice;

		/// <summary>
		/// Worked out in the initialization, gives a list of potential technologies that can be made available when this technologies researched
		/// </summary>
		public List<String> PotentialUnlocks;

		/// <summary>
		/// The prefab ID of the item we're trying to research. Taken from the prefab tracker's randomly generated ID.
		/// Empty if we're not trying to produce an item from this node.
		/// </summary>
		public String prefabID;

		public TechType techType;

		public Techweb Techweb;

		public System.Drawing.Color Colour
		{
			get
			{
				return System.Drawing.Color.FromArgb((int)(ColourPublic.a * 255), (int)(ColourPublic.r * 255), (int)(ColourPublic.g * 255), (int)(ColourPublic.b * 255));
			}
		}

		[JsonIgnore] public Color ColourPublic; //Unity Colours do not work with our Json Serialisation, but System.Drawing Colours dont work with the inspector. So we use the system.color struct to serialise and then convert back and forth between the two.
	}

	public enum TechType
	{
		None = 0,
		Robotics = 1,
		Machinery = 2,
		Equipment = 3,
		Chemistry = 4,
	}

	public class TechWebNode
	{
		public Technology technology = new Technology();
	}
}