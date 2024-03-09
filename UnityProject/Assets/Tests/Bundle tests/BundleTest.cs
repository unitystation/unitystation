using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests
{
	public class BundleTest
	{
		[Test]
		public void CheckBundleDuplication()
		{
			var path = Application.dataPath.Remove(Application.dataPath.IndexOf("/Assets"));
			path = path + "/AddressablePackingProjects";
			var Directories = System.IO.Directory.GetDirectories(path);
			foreach (var Directori in Directories)
			{
				var newpath = Directori + "/ServerData";
				if (System.IO.Directory.Exists(newpath))
				{
					var Files = System.IO.Directory.GetFiles(newpath);

					string FoundFile = "";
					foreach (var File in Files)
					{
						if (File.EndsWith(".json"))
						{
							if (FoundFile != "")
							{
								Assert.Fail($"two catalogues present please only ensure one {FoundFile} and {File}" );
							}

							FoundFile = File;
						}
					}

					if (FoundFile == "")
					{
						Assert.Fail($"missing json file for Bundle in {newpath}" );
					}

				}
			}

		}
	}
}