using NUnit.Framework;
using Systems.Permissions;
using UnityEngine;

namespace Tests.Permissions
{
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
			Assert.True(manager.HasPermission(identifier, permission));
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
			Assert.False(manager.HasPermission(identifier, permission));
		}

		[Test]
		[TestCase("player_god", "perm_a")]
		[TestCase("player_god", "perm_b")]
		[TestCase("player_god", "perm_c")]
		[TestCase("player_god", "perm_d")]
		[TestCase("player_god", "perm_not_listed")]
		public void GivenAUserWithWildcardPermission_WhenCheckingPermission_ThenPermissionIsGranted(string identifier, string permission)
		{
			Assert.True(manager.HasPermission(identifier, permission));
		}

		[Test]
		public void GivenAUserThatDoesNotExist_WhenCheckingPermission_ThenPermissionIsNotGranted()
		{
			Assert.False(manager.HasPermission("nonexistentPlayer", "perm_a"));
		}
	}
}