using System.Text;
using NUnit.Framework;
using UnityEngine;
using US13.Systems.Permissions;

namespace Tests.Permissions
{
	[Category("Security")]
	public class PermissionsManagerTests
	{
		private GameObject managerObject;
		private PermissionsManager manager;

		private const string CONFIG_CONTENT = @"
		[ranks.god]
		show_in_chat = false
		abbreviation = """"
		color = """"
		permissions = [""*""]

		[ranks.abc]
		show_in_chat = false
		abbreviation = """"
		color = """"
		permissions = [""perm_a"", ""perm_b"", ""perm_c""]

		[ranks.bcd]
		show_in_chat = false
		abbreviation = """"
		color = """"
		permissions = [""perm_b"", ""perm_c"", ""perm_d""]

		[ranks.no_perms]
		show_in_chat = false
		abbreviation = """"
		color = """"
		permissions = []

		[[players]]
		identifier = ""player_god""
		rank = ""god""

		[[players]]
		identifier = ""player_abc""
		rank = ""abc""

		[[players]]
		identifier = ""player_bcd""
		rank = ""bcd""

		[[players]]
		identifier = ""player_no_perms""
		rank = ""no_perms""
		";

		[SetUp]
		public void Setup()
		{
			managerObject = new GameObject();
			managerObject.AddComponent<PermissionsManager>();
			manager = managerObject.GetComponent<PermissionsManager>();
			manager.LoadPermissionsConfig(CONFIG_CONTENT);
		}

		[TearDown]
		public void Teardown()
		{
			Object.DestroyImmediate(managerObject);
		}

		[Test]
		public void GivenInvalidConfig_WhenLoadingConfig_ThenDefaultIsLoadedSuccessfully()
		{
			manager.LoadPermissionsConfig("");
			Assert.NotNull(manager.Config);
			Assert.NotNull(manager.Config.Players);
			Assert.NotNull(manager.Config.Ranks);
		}

		[Test]
		public void GivenValidConfig_WhenLoadingConfig_ThenConfigIsLoaded()
		{
			Assert.NotNull(manager.Config);
		}

		[Test]
		[TestCase("player_abc", "perm_a")]
		[TestCase("player_abc", "perm_b")]
		[TestCase("player_abc", "perm_c")]
		[TestCase("player_bcd", "perm_b")]
		[TestCase("player_bcd", "perm_c")]
		[TestCase("player_bcd", "perm_d")]
		public void GivenAUserWhoHasPermission_WhenCheckingPermission_ThenPermissionIsGranted(string identifier, string permission)
		{
			Assert.True(manager.PlayerHasPermission(identifier, permission));
		}

		[Test]
		[TestCase("player_abc", "perm_d")]
		[TestCase("player_bcd", "perm_a")]
		[TestCase("player_no_perms", "perm_a")]
		[TestCase("player_no_perms", "perm_a")]
		[TestCase("player_no_perms", "perm_b")]
		[TestCase("player_no_perms", "perm_c")]
		[TestCase("player_no_perms", "perm_d")]
		public void GivenAUserWhoDoesNotHavePermission_WhenCheckingPermission_ThenPermissionIsNotGranted(string identifier, string permission)
		{
			Assert.False(manager.PlayerHasPermission(identifier, permission));
		}

		[Test]
		[TestCase("player_god", "perm_a")]
		[TestCase("player_god", "perm_b")]
		[TestCase("player_god", "perm_c")]
		[TestCase("player_god", "perm_d")]
		[TestCase("player_god", "perm_not_listed")]
		public void GivenAUserWithWildcardPermission_WhenCheckingPermission_ThenPermissionIsGranted(string identifier, string permission)
		{
			Assert.True(manager.PlayerHasPermission(identifier, permission));
		}

		[Test]
		public void GivenAUserThatDoesNotExist_WhenCheckingPermission_ThenPermissionIsNotGranted()
		{
			Assert.False(manager.PlayerHasPermission("nonexistentPlayer", "perm_a"));
		}

		[Test]
		public void GivenThatAutoRankIsDefined_WhenPlayerHasNoRank_ThenPlayerHasAutoRank()
		{
			StringBuilder newConfig = new("auto_rank = \"no_perms\"");
			newConfig.AppendLine(CONFIG_CONTENT);
			manager.LoadPermissionsConfig(newConfig.ToString());
			Rank rank = manager.GetRankForAccount("non_defined_player");

			Assert.That(rank?.Name == "no_perms");
		}

		[Test]
		[TestCase("player_god", "god")]
		[TestCase("player_abc", "abc")]
		[TestCase("player_bcd", "bcd")]
		public void GivenThatAutoRankIsDefined_WhenPlayerHasRank_ThenPlayerHasRank(string accountId, string expectedRank)
		{
			StringBuilder newConfig = new("auto_rank = \"no_perms\"");
			newConfig.AppendLine(CONFIG_CONTENT);
			manager.LoadPermissionsConfig(newConfig.ToString());
			Rank rank =  manager.GetRankForAccount(accountId);

			Assert.That(rank?.Name == expectedRank);
		}
	}
}