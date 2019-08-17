using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
[System.Serializable]
public class SpriteDataForSH
{
	public List<List<List<SpriteHandler.SpriteInfo>>> spriteList = new List<List<List<SpriteHandler.SpriteInfo>>>();
	public int spriteIndex;
	public int VariantIndex;
	public uint AID;
	public List<SpriteHListPoint> Serialized = new List<SpriteHListPoint>();


	//Serialise is the sprite data so it can be stored by the unity prefab
	public void SerializeT()
	{
		Serialized.Clear();
		AID = 1; //used to assign each point an individual ID used to assess which list it as a part of and what list ID it is
		foreach (var Top in spriteList)
		{
			var TopListPoint = new SpriteHListPoint();
			TopListPoint.listID = AID;
			AID++; 

			foreach (var Mid in Top)
			{
				var MidListPoint = new SpriteHListPoint();
				MidListPoint.listID = AID;
				AID++;
				MidListPoint.inlistID = TopListPoint.listID;

				foreach (var Bot in Mid)
				{
					var BotListPoint = new SpriteHListPoint();
					BotListPoint.listID = AID;
					AID++;
					BotListPoint.inlistID = MidListPoint.listID;
					BotListPoint.sprite = Bot.sprite;
					BotListPoint.waitTime = Bot.waitTime;
					Serialized.Add(BotListPoint);

				}
				Serialized.Add(MidListPoint);
			}
			Serialized.Add(TopListPoint);
		}
	}

	//deSerialise is the sprite data so It is in an Usable format
	public void DeSerializeT()
	{
		spriteList.Clear();
		//Used to initialise the dictionaries With the appropriate data from each layer
		List<SpriteHListPoint> TopParentList = new List<SpriteHListPoint>();
		List<SpriteHListPoint> MidParentList = new List<SpriteHListPoint>();
		List<SpriteHListPoint> BotListPoint = new List<SpriteHListPoint>();
		foreach (var ListPoint in Serialized)
		{
			if (ListPoint.inlistID == 0)
			{
				TopParentList.Add(ListPoint);
			}
			else if (ListPoint.sprite != null)
			{
				BotListPoint.Add(ListPoint);
			}
			else
			{
				MidParentList.Add(ListPoint);
			}
		}

		int i = 0;

		//Using this information using the ID to assess which layer and therefore which list it would be in
		foreach (var Top in TopParentList) {			int c = 0;
			spriteList.Add(new List<List<SpriteHandler.SpriteInfo>>());
			foreach (var Mid in MidParentList.Where(m => m.inlistID == Top.listID))
			{
				spriteList[i].Add(new List<SpriteHandler.SpriteInfo>());
				foreach (var Bot in BotListPoint.Where(m => m.inlistID == Mid.listID))
				{
					//Logger.Log("addedddddd");
					spriteList[i][c].Add(new SpriteHandler.SpriteInfo()
					{
						sprite = Bot.sprite,
						waitTime = Bot.waitTime
					});

				}
				c++;
			}
			i++;
		}
		if (spriteList.Count == 0) {
			spriteList.Add(new List<List<SpriteHandler.SpriteInfo>>());
			spriteList[0].Add(new List<SpriteHandler.SpriteInfo>());
			spriteList[0][0].Add(new SpriteHandler.SpriteInfo());
		}
	}
}
