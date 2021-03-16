// using System.Text;
// using Systems.Cargo;
// using UnityEditor;
// using UnityEngine;
//
// namespace Tests.Balance
// {
// 	public static class CargoDataMigrator
// 	{
// 		private const string PATH_CATEGORIES = "Assets/ScriptableObjects/Cargo/Categories";
// 		private const string PATH_ORDERS = "Assets/ScriptableObjects/Cargo/Orders";
// 		private static CargoData cargoData;
//
// 		[MenuItem("Tools/OrderMigration")]
// 		public static void MigrateOrders()
// 		{
// 			LoadCargoData();
// 			cargoData.Categories.Clear();
//
// 			foreach (var category in cargoData.Supplies)
// 			{
// 				var cat = ScriptableObject.CreateInstance<CargoCategory>();
// 				cat.CategoryName = category.CategoryName;
//
// 				foreach (var order in category.Supplies)
// 				{
// 					var newOrder = ScriptableObject.CreateInstance<CargoOrderSO>();
// 					newOrder.OrderName = order.OrderName;
// 					newOrder.Crate = order.Crate;
// 					newOrder.Items = order.Items;
// 					newOrder.CreditCost = order.CreditsCost;
//
// 					AssetDatabase.CreateAsset(newOrder, GetFilePath(newOrder.OrderName));
// 					var orderFile = AssetDatabase.LoadAssetAtPath<CargoOrderSO>(GetFilePath(newOrder.OrderName));
//
// 					cat.Orders.Add(orderFile);
// 				}
//
// 				AssetDatabase.CreateAsset(cat, GetFilePath(cat.CategoryName, false));
// 				var catFile = AssetDatabase.LoadAssetAtPath<CargoCategory>(GetFilePath(cat.CategoryName, false));
// 				cargoData.Categories.Add(catFile);
// 			}
// 		}
//
// 		private static string GetFilePath(string name, bool isOrder = true)
// 		{
// 			name = name.Replace("/", "_");
// 			return isOrder ? $"{PATH_ORDERS}/{name}.asset" : $"{PATH_CATEGORIES}/{name}.asset";
// 		}
//
// 		private static void LoadCargoData()
// 		{
//
// 			Utils.TryGetScriptableObjectGUID(typeof(CargoData), new StringBuilder(), out string id);
// 			cargoData = AssetDatabase.LoadAssetAtPath<CargoData>(AssetDatabase.GUIDToAssetPath(id));
// 		}
// 	}
// }