using NUnit.Framework;
using Systems.Permissions;
using UnityEngine;

namespace Tests.Permissions
{
	public class PermissionsManagerTests
	{
		private GameObject managerObject;
		private PermissionsManager manager;

		[SetUp]
		public void Setup()
		{
			managerObject = new GameObject();
			managerObject.AddComponent<PermissionsManager>();
			manager = managerObject.GetComponent<PermissionsManager>();
			manager.LoadPermissionsConfig();
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
		[TestCase("hostPlayer", "fun")]
		[TestCase("hostPlayer", "perma_promote")]
		[TestCase("adminPlayer", "fun")]
		public void GivenAUserWhoHasPermission_WhenCheckingPermission_ThenPermissionIsGranted(string identifier, string permission)
		{
			Assert.True(manager.HasPermission(identifier, permission));
		}

		[Test]
		[TestCase("normalPlayer", "perma_promote")]
		[TestCase("normalPlayer", "fun")]
		public void GivenAUserWhoDoesNotHavePermission_WhenCheckingPermission_ThenPermissionIsNotGranted(string identifier, string permission)
		{
			Assert.False(manager.HasPermission(identifier, permission));
		}

		[Test]
		public void GivenAUserThatDoesNotExist_WhenCheckingPermission_ThenPermissionIsNotGranted()
		{
			Assert.False(manager.HasPermission("nonexistentPlayer", "fun"));
		}
	}
}