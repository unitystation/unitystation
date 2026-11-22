using System;
using System.Threading.Tasks;
using Logs;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;
using US13.Managers;

namespace US13.Core.Addressables.Types
{
	/// <summary>
	/// Note about this class, Currently if you want a custom AssetReference Like asset reference texture, might have to make a new class this needs to be explored
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[Serializable]
	public class AddressableReference<T> where T : UnityEngine.Object
	{
		[FormerlySerializedAs("Path")] public string AssetAddress = "";
		[HideInInspector] public AssetReference AssetReference = null;

		public bool IsNotValidKey => NotValidKey();
		public bool IsReadyLoaded => ReadyLoaded();

		private T StoredLoadedReference = null;
		[NonSerialized] public UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation ResolvedLocation;
		private AsyncOperationHandle<T> ownedHandle;
		private Task<T> pendingLoad;
		private int loadGeneration;


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
			int generation = loadGeneration;
			try
			{
				if (this is AddressableAudioSource)
				{
					var manager = US13.Managers.AddressableManager.AddressableCatalogueManager.Instance;
					if (manager == null) throw new InvalidOperationException("Audio catalogue manager is missing from the scene.");
					await manager.WaitForInitialLoadAsync();
					if (generation != loadGeneration) return null;
					// Direct Preload/Load calls must use the same catalogue and override resolution
					// as AudioManager, rather than racing startup or loading a serialized GUID first.
					if (!string.IsNullOrEmpty(AssetAddress)) ResolvedLocation = manager.ResolveAudio(AssetAddress);
				}
				// Keep IResourceLocation typed: casting it to object selects the key overload,
				// which searches the catalogues for the location object itself and fails.
				var handle = ResolvedLocation != null
					? UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(ResolvedLocation)
					: UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(
						AssetReference != null && AssetReference.RuntimeKeyIsValid() ? AssetReference.RuntimeKey : AssetAddress);
				ownedHandle = handle;
				var result = await handle.Task;
				if (generation != loadGeneration) return null;
				if (handle.Status != AsyncOperationStatus.Succeeded) throw handle.OperationException;
				StoredLoadedReference = result;
				return result;
			}
			catch (OperationCanceledException) { return null; }
			catch (Exception exception)
			{
				if (generation == loadGeneration && ownedHandle.IsValid()) UnityEngine.AddressableAssets.Addressables.Release(ownedHandle);
				Loggy.Error($"Unable to load addressable {AssetAddress}: {exception.Message}", Category.Addressables);
				return null;
			}
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
			_ = Load();
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
				Loggy.Error($"Asset is not loaded with ID of {AssetAddress}", Category.Addressables);
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
			if (pendingLoad == null || pendingLoad.IsCompleted) pendingLoad = LoadAsset();
			await pendingLoad;
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
			loadGeneration++;
			StoredLoadedReference = null;
			pendingLoad = null;
			if (ownedHandle.IsValid()) UnityEngine.AddressableAssets.Addressables.Release(ownedHandle);
			ownedHandle = default;
		}

		/// <summary>
		/// Validates an addressable's address to ensure it points to an addressable asset
		/// </summary>
		/// <returns>True if the addressable has a valid address, False if it does not.</returns>
		public async Task<bool> HasValidAddress()
		{
			var validate = UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync(AssetAddress);
			await validate.Task;
			if (validate.Status == AsyncOperationStatus.Succeeded) {
       			if (validate.Result.Count > 0) {
					   return true;
				}
			}
			Loggy.Warning($"Addressable Address is invalid: {AssetAddress}", Category.Addressables);
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
