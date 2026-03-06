using NUnit.Framework;
using US13.Items.Weapons;

namespace Tests.Asset
{
	[Category(nameof(Asset))]
	public class DynamicMagazineSpriteTests
	{
		[Test]
		public void AllDynamicMagazineSpritesHaveRequiredFieldsAssigned()
		{
			var report = new TestReport();

			foreach (var prefab in Utils.FindPrefabs())
			{
				foreach (var dms in prefab.GetComponentsInChildren<DynamicMagazineSprite>(true))
				{
					var name = $"{prefab.name} ({dms.gameObject.name})";

					report.FailIf(dms.Magazine == null)
						.Append($"{name}: magazine field is not assigned.")
						.AppendLine();

					report.FailIf(dms.SpriteHandler == null)
						.Append($"{name}: spriteHandler field is not assigned.")
						.AppendLine();
				}
			}

			report.AssertPassed();
		}
	}
}
