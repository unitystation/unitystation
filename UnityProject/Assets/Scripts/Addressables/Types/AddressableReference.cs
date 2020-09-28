using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
		public string Path = "";
		public AssetReference AssetReference = null;
		public T Asset;

		public bool IsNotValidKey => NotValidKey();
		public bool IsReadyLoaded => ReadyLoaded();

		#region InternalStuff

		private bool ReadyLoaded()
		{
			if ((AssetReference == null) && (!string.IsNullOrWhiteSpace(Path)))
			{
				return !(Asset == null);
			}

			if (IsNotValidKey) return false;
			return AssetReference.Asset != null;
		}

		public bool NotValidKey()
		{
			if (AssetReference.RuntimeKey == null) return true;
			return false;
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
			AssetReference.LoadAssetAsync<T>();
		}


		/// <summary>
		/// Assuming that you've loaded the asset allow you to access it instantly
		/// </summary>
		public virtual T Retrieve()
		{
			if ((AssetReference == null) && (!string.IsNullOrWhiteSpace(Path)))
			{
				if (IsReadyLoaded)
					return Asset;
				else
				{
					Logger.LogError("Asset is not loaded", Category.Addressables);
					return null;
				}
			}

			if (IsNotValidKey) return null;
			if (IsReadyLoaded)
			{
				return (T)AssetReference.Asset;
			}
			else
			{
				Logger.LogError("Asset is not loaded", Category.Addressables);
				return null;
			}
		}

		/// <summary>
		/// Load asset
		/// </summary>
		public async Task<T> Load()
		{
			// Special load when AssetReference is null but we have an addressable path
			if ((AssetReference == null) && (!string.IsNullOrWhiteSpace(Path)))
			{
				if (IsReadyLoaded)
				{
					return Retrieve();
				}

				Asset = await Addressables.LoadAssetAsync<T>(Path).Task;
				return Retrieve();
			}

			if (IsNotValidKey) return null;
			if (IsReadyLoaded)
			{
				return (T)AssetReference.Asset;
			}

			//Add to manager tracker
			await AssetReference.LoadAssetAsync<T>().Task;
			return (T)(AssetReference.Asset);
		}

		/// <summary>
		/// Load an asset and passes the handle to the AssetManager
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
			AssetManager.Instance.AddLoadingAssetHandle(handle, Path);
			await AssetReference.LoadAssetAsync<T>().Task;
			return (T)(AssetReference.Asset);
		}

		public void Unload()
		{
			if (IsNotValidKey) return;
			if (IsReadyLoaded)
			{
				//Check manager To see if it's implemented
				Logger.Log("Not implemented yet");
			}
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
		private AudioSource audioSource = null;

		public new AssetReference AssetReference
		{
			get
			{
				if (base.AssetReference == null)
				{
					base.AssetReference = new AssetReference(base.Asset.)
				}

				return base.AssetReference;
			}
		}

		public AudioSource AudioSource
		{
			get
			{
				if (audioSource == null)
				{
					GameObject gameObject = base.Retrieve();
					if (!gameObject.TryGetComponent(out audioSource))
					{
						throw new ArgumentException($"Invalid AddressableAudioSource {gameObject.name} is missing AudioSource component");
					}
				}

				return audioSource;
			}
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
			Path = addressablePath;

			Addressables.Lo
		}
	}

	[System.Serializable]
	public class AddressableTexture : AddressableReference<Texture> { }
}