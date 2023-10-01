using System;
using System.Threading.Tasks;
using Logs;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

namespace AddressableReferences
{
	/// <summary>
	/// Note about this class, Currently if you want a custom AssetReference Like asset reference texture, might have to make a new class this needs to be explored
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[Serializable]
	public class AddressableReference<T> where T : UnityEngine.Object
	{
		public UnLoadSetting SetLoadSetting = UnLoadSetting.KeepLoaded;
		[FormerlySerializedAs("Path")] public string AssetAddress = "";
		public AssetReference AssetReference = null;

		public bool IsNotValidKey => NotValidKey();
		public bool IsReadyLoaded => ReadyLoaded();

		private T StoredLoadedReference = null;


		#region InternalStuff

		private bool ReadyLoaded()
		{
			if (IsNotValidKey) return false;
			return StoredLoadedReference != null;
		}

		private bool NotValidKey()
		{
			if (AssetReference?.RuntimeKey == null && string.IsNullOrEmpty(AssetAddress)) return true;
			return false;
		}


		private async Task<T> LoadAsset()
		{
			//Just comment the try out if you want to just load by AssetAddress
			try
			{
				if (AssetReference.OperationHandle.Status == AsyncOperationStatus.None)
				{
					await AssetReference.LoadAssetAsync<T>().Task;
					StoredLoadedReference = AssetReference.Asset as T;
					return StoredLoadedReference;
				}
				else
				{
					await AssetReference.LoadAssetAsync<T>().Task;
					StoredLoadedReference = AssetReference.Asset as T;
					return StoredLoadedReference;
				}

			}
			catch
			{
				if (string.IsNullOrEmpty(AssetAddress))
				{
					//Logger.LogError("Address is null for " + AssetReference.SubObjectName);
					return null;
				}

				var validateAddress = Addressables.LoadResourceLocationsAsync(AssetAddress);

				await validateAddress.Task;

				if (validateAddress.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded) {
					if (validateAddress.Result.Count > 0) {
						// asset exists go ahead and load
						var AsynchronousHandle = Addressables.LoadAssetAsync<T>(AssetAddress);
						await AsynchronousHandle.Task;
						StoredLoadedReference = AsynchronousHandle.Result;
						return StoredLoadedReference;
					}
					else
					{
						Loggy.LogError("Address is invalid for " + AssetReference, Category.Addressables);
					}
				}
			}
			return null;
		}

		#endregion
		#region Externally accessible stuff

		/// <summary>
		/// If you manually want the asset to be ready on demand
		/// </summary>
		public void Preload()
		{
			if (IsNotValidKey) return;
			if (IsReadyLoaded) return;
			_ = LoadAsset();
		}


		/// <summary>
		/// Assuming that you've loaded the asset allow you to access it instantly
		/// </summary>
		public T Retrieve()
		{
			if (IsNotValidKey) return null;
			if (IsReadyLoaded)
			{
				return StoredLoadedReference;
			}
			else
			{
				Loggy.LogError("Asset is not loaded", Category.Addressables);
				return null;
			}
		}

		/// <summary>
		/// Load asset
		/// </summary>
		public async Task<T> Load()
		{
			if (IsNotValidKey) return null;
			if (IsReadyLoaded)
			{
				return StoredLoadedReference;
			}

			//Add to manager tracker
			await LoadAsset();
			return (StoredLoadedReference);
		}

		/// <summary>
		/// Load an asset and passes the handle to the AssetManager
		/// not yet working with string path?
		/// </summary>
		public async Task<T> LoadThroughAssetManager()
		{
			if (IsNotValidKey) return null;
			if (IsReadyLoaded)
			{
				return (T)AssetReference.Asset;
			}

			//Add to manager tracker
			var handle = AssetReference.LoadAssetAsync<T>();
			AssetManager.Instance.AddLoadingAssetHandle(handle, AssetAddress);
			await AssetReference.LoadAssetAsync<T>().Task;
			return (T)(AssetReference.Asset);
		}

		public void Unload()
		{
			if (IsNotValidKey) return;
			if (IsReadyLoaded)
			{
				//Check manager To see if it's implemented
				Loggy.Log($"Addressable Manager not implemented yet, can't unload {AssetAddress}", Category.Addressables);
			}
		}

		/// <summary>
		/// Validates an addressable's address to ensure it points to an addressable asset
		/// </summary>
		/// <returns>True if the addressable has a valid address, False if it does not.</returns>
		public async Task<bool> HasValidAddress()
		{
			var validate = Addressables.LoadResourceLocationsAsync(AssetAddress);
			await validate.Task;
			if (validate.Status == AsyncOperationStatus.Succeeded) {
       			if (validate.Result.Count > 0) {
					   return true;
				}
			}
			Loggy.LogWarning($"Addressable Address is invalid: {AssetAddress}", Category.Addressables);
			return false;
        }

		#endregion
	}


	public enum UnLoadSetting
	{
		KeepLoaded, //Keep loaded until the game closes
		UnloadOnRoundEnd,//Unloads when the round ends
		When0Referenced //Unloads when there are zero references
	}

	public enum LoadSetting
	{
		PreLoad, //Preload on game start
		PreLoadScene,//Preload on Scene then Unload once Scene Has changed
		OnDemand //Load and unload when it's needed
	}

	[Serializable]
	public class AddressableSprite : AddressableReference<Sprite> { }

	[Serializable]
	public class AddressableAudioSource : AddressableReference<GameObject>
	{
		private WeakReference<AudioSource> audioSource = null;

		public AudioSource AudioSource
		{
			get
			{
				AudioSource result = null;

				if (audioSource == null || !audioSource.TryGetTarget(out result))
				{
					GameObject gameObject = base.Retrieve();
					if (gameObject == null || !gameObject.TryGetComponent(out result))
						return null;

					audioSource = new WeakReference<AudioSource>(result);
				}

				return result;
			}
		}

		/// <summary>
		/// A default constructor is required in order to pass addressableAudioSources as network messages
		/// </summary>
		/// <param name="assetReferenceGuid">The primary key (AssetGuid) of the AssetReference</param>
		public AddressableAudioSource()
		{
			AssetReference = null;
		}

		/// <summary>
		/// Constructor that provides an AddressableAudioSource by an AssetReference Primary Key (AssetGuid)
		/// </summary>
		/// <param name="assetReferenceGuid">The primary key (AssetGuid) of the AssetReference</param>
		public AddressableAudioSource(AssetReference assetReference)
		{
			AssetReference = assetReference;
		}

		/// <summary>
		/// Constructor that provides an AddressableAudioSource by an Addressable path
		/// </summary>
		/// <param name="addressablePath">The path of the addressable</param>
		public AddressableAudioSource(string addressablePath)
		{
			AssetAddress = addressablePath;
		}
	}

	[Serializable]
	public class AddressableTexture : AddressableReference<Texture> { }
}
