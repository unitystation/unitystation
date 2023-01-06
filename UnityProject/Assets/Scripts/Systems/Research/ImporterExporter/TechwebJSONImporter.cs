﻿using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Systems.Research.Data;
using UnityEngine;

namespace Systems.Research.ImporterExporter
{
	public class TechwebJSONImporter : TechwebImporter
	{
		public override Techweb Import(string filePath)
		{
			Techweb techweb = new Techweb();
			List<TechWebNode> Nodes = new List<TechWebNode>();
			var path = $"{filePath}";
			if(File.Exists(path) == false) return null;
			string json = File.ReadAllText(path);
			if(json == null || json.Length < 3) return null;
			var JsonTechweb = JsonConvert.DeserializeObject<List<Dictionary<String, System.Object>>>(json);
			for (var i = 0; i < JsonTechweb.Count; i++)
			{
				TechWebNode newNode = new TechWebNode();
				Technology TechnologyPass = new Technology();
				TechnologyPass.ID = JsonTechweb[i]["ID"].ToString();
				TechnologyPass.DisplayName = JsonTechweb[i]["DisplayName"].ToString();
				TechnologyPass.Description = JsonTechweb[i]["Description"].ToString();

				if (JsonTechweb[i].ContainsKey("DesignIDs"))
				{ TechnologyPass.DesignIDs = JsonConvert.DeserializeObject<List<string>>(JsonTechweb[i]["DesignIDs"].ToString()); }
				else
				{
					List<string> EmptyDesignIDs = new List<string>();
					TechnologyPass.DesignIDs = EmptyDesignIDs;
				}

				if (JsonTechweb[i].ContainsKey("ResearchCosts"))
				{
					TechnologyPass.ResearchCosts = int.Parse(JsonTechweb[i]["ResearchCosts"].ToString());
				}
				else
				{
					TechnologyPass.ResearchCosts = 0;
				}

				if (JsonTechweb[i].ContainsKey("ExportPrice"))
				{
					TechnologyPass.ExportPrice = int.Parse(JsonTechweb[i]["ExportPrice"].ToString());
				}
				else
				{
					TechnologyPass.ExportPrice = 0;
				}


				if (JsonTechweb[i].ContainsKey("RequiredTechnologies"))
				{
					TechnologyPass.RequiredTechnologies = JsonConvert.DeserializeObject<List<string>>(JsonTechweb[i]["RequiredTechnologies"].ToString());
				}
				else
				{
					List<string> EmptyRequiredTechnologies = new List<string>();
					TechnologyPass.RequiredTechnologies = EmptyRequiredTechnologies;
				}

				if (JsonTechweb[i].ContainsKey("StartingNode"))
				{
					TechnologyPass.StartingNode = bool.Parse(JsonTechweb[i]["StartingNode"].ToString());
				}
				else
				{
					TechnologyPass.StartingNode = false;
				}

				if (JsonTechweb[i].ContainsKey("PotentialUnlocks"))
				{
					TechnologyPass.PotentialUnlocks = JsonConvert.DeserializeObject<List<string>>(JsonTechweb[i]["PotentialUnlocks"].ToString());
				}
				else
				{
					var EmptyPotentialUnlocks = new List<string>();
					EmptyPotentialUnlocks.Add("");
					TechnologyPass.PotentialUnlocks = EmptyPotentialUnlocks;
				}

				newNode.technology = TechnologyPass;
				TechnologyPass.Techweb = techweb;
				Nodes.Add(newNode);

				if (newNode.technology.StartingNode == true) techweb.ResearchTechology(newNode.technology, false);
			}
			techweb.nodes = Nodes;

			return techweb;
		}
	}
}