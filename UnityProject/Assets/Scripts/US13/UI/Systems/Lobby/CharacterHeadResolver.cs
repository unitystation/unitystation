using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using US13.HealthV2.Living.BodyParts;
using US13.Systems.Lobby;
using US13.UI.Systems.Lobby.SubCustomisation.BodyPartCustomisations;

namespace US13.UI.Systems.Lobby
{
	/// <summary>
	/// A single sprite layer for the player's head. Heads are made up of multiple stacked layers.
	/// </summary>
	public struct HeadLayer
	{
		public Sprite Sprite;
		public Color Colour;
		public int Order;
	}

	/// <summary>
	/// Builds the front facing head sprites for a character sheet.
	/// </summary>
	public static class CharacterHeadResolver
	{
		private class HeadContext
		{
			public BodyType BodyType;
			public Color SkinTone;
			public List<CustomisationStorage> Customisations;
			public List<BodyPart> SkinToneParts;
		}

		/// <summary>
		/// Builds a character's head layers, back to front.
		/// </summary>
		public static List<HeadLayer> Resolve(CharacterSheet sheet)
		{
			var layers = new List<HeadLayer>();
			if (sheet == null) return layers;

			var race = sheet.GetRaceSo();
			if (race == null) return layers;
			if (race.Base == null) return layers;
			if (race.Base.Head == null) return layers;

			var context = new HeadContext();
			context.BodyType = sheet.BodyType;
			context.SkinTone = GetSkinTone(sheet.SkinTone);
			context.Customisations = sheet.SerialisedBodyPartCustom;
			context.SkinToneParts = race.Base.BodyPartsThatShareTheSkinTone;

			foreach (var element in race.Base.Head.Elements)
			{
				if (element == null) continue;
				if (element.TryGetComponent<BodyPart>(out var head) == false) continue;

				AddPart(head, "", context, layers);
			}

			layers.Sort(CompareByOrder);
			return layers;
		}

		private static void AddPart(BodyPart part, string parentPath, HeadContext context, List<HeadLayer> layers)
		{
			string path = parentPath + "/" + part.name;

			AddLayersForPart(part, path, context, layers);
			AddSubOrgans(part, path, context, layers);
		}

		private static void AddSubOrgans(BodyPart part, string path, HeadContext context, List<HeadLayer> layers)
		{
			if (part.OrganStorage == null) return;
			if (part.OrganStorage.Populater == null) return;
			if (part.OrganStorage.Populater.DeprecatedContents == null) return;

			foreach (var organ in part.OrganStorage.Populater.DeprecatedContents)
			{
				if (organ == null) continue;
				if (organ.TryGetComponent<BodyPart>(out var subPart) == false) continue;

				AddPart(subPart, path, context, layers);
			}
		}

		private static void AddLayersForPart(BodyPart part, string path, HeadContext context, List<HeadLayer> layers)
		{
			if (part.GetBodyTypesSprites == null) return;

			var sprites = part.GetBodyTypeSprites(context.BodyType);
			if (sprites == null) return;
			if (sprites.Item2 == null) return;
			if (sprites.Item2.Count == 0) return;

			Color colour = GetPartColour(part, context);
			int baseOrder = GetBaseOrder(sprites.Item1);

			var partLayers = new List<HeadLayer>();
			for (int i = 0; i < sprites.Item2.Count; i++)
			{
				var layer = new HeadLayer();
				layer.Sprite = GetDownFacingSprite(sprites.Item2[i]);
				layer.Colour = colour;
				layer.Order = baseOrder + i;
				partLayers.Add(layer);
			}

			ApplyCustomisation(part, path, context, partLayers);

			foreach (var layer in partLayers)
			{
				if (layer.Sprite == null) continue;

				layers.Add(layer);
			}
		}

		private static void ApplyCustomisation(BodyPart part, string path, HeadContext context, List<HeadLayer> partLayers)
		{
			if (part.LobbyCustomisation == null) return;

			string data = GetCustomisationData(path, context);

			if (data == null) return;

			if (part.LobbyCustomisation is BodyPartSpriteAndColour spriteAndColour)
			{
				ApplySpriteAndColour(spriteAndColour, data, part, context, partLayers);
				return;
			}

			if (part.LobbyCustomisation is BodyPartColourSprite)
			{
				ApplyColourOnly(data, part, context, partLayers);
			}
		}

		private static void ApplySpriteAndColour(BodyPartSpriteAndColour spriteAndColour, string data,
			BodyPart part, HeadContext context, List<HeadLayer> partLayers)
		{
			var choice = JsonConvert.DeserializeObject<BodyPartSpriteAndColour.ColourAndSelected>(data);
			var chosenSprite = BodyPartSpriteAndColour.GetSpriteForChoice(spriteAndColour.OptionalSprites, choice.Chosen);

			var layer = partLayers[0];
			layer.Sprite = GetDownFacingSprite(chosenSprite);
			layer.Colour = GetCustomisedColour(choice.color, part, context);
			partLayers[0] = layer;
		}

		private static void ApplyColourOnly(string data, BodyPart part, HeadContext context, List<HeadLayer> partLayers)
		{
			var layer = partLayers[0];
			layer.Colour = GetCustomisedColour(data, part, context);
			partLayers[0] = layer;
		}

		private static string GetCustomisationData(string path, HeadContext context)
		{
			if (context.Customisations == null) return null;

			foreach (var custom in context.Customisations)
			{
				if (custom.path != path) continue;

				return custom.Data.Replace("@£", "\"");
			}

			return null;
		}

		private static Color GetCustomisedColour(string html, BodyPart part, HeadContext context)
		{
			if (SharesSkinTone(part, context)) return context.SkinTone;
			if (ColorUtility.TryParseHtmlString(html, out var colour) == false) return Color.white;

			colour.a = 1;
			return colour;
		}

		private static Color GetPartColour(BodyPart part, HeadContext context)
		{
			if (part.IsSurface) return context.SkinTone;
			if (SharesSkinTone(part, context)) return context.SkinTone;

			return Color.white;
		}

		private static bool SharesSkinTone(BodyPart part, HeadContext context)
		{
			if (context.SkinToneParts == null) return false;

			return context.SkinToneParts.Contains(part);
		}

		private static Color GetSkinTone(string html)
		{
			if (ColorUtility.TryParseHtmlString(html, out var colour) == false) return Color.white;

			colour.a = 1;
			return colour;
		}

		private static int GetBaseOrder(SpriteOrder spriteOrder)
		{
			if (spriteOrder == null) return 0;
			if (spriteOrder.Orders == null) return 0;
			if (spriteOrder.Orders.Count == 0) return 0;

			return spriteOrder.Orders[0];
		}

		private static Sprite GetDownFacingSprite(SpriteDataSO spriteData)
		{
			if (spriteData == null) return null;
			if (spriteData.Variance == null) return null;
			if (spriteData.Variance.Count == 0) return null;

			var frames = spriteData.Variance[0].Frames;
			if (frames == null) return null;
			if (frames.Count == 0) return null;
			if (frames[0] == null) return null;

			return frames[0].sprite;
		}

		private static int CompareByOrder(HeadLayer a, HeadLayer b)
		{
			return a.Order.CompareTo(b.Order);
		}
	}
}
