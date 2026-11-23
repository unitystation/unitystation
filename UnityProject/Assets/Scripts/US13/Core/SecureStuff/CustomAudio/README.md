# Server audio packs

Put each pack's `catalog*.json` and its `.bundle` files together under the server's `StreamingAssets/CustomAudio` folder, optionally in separate pack subfolders.

The server discovers these files when preparing the audio manifest for a joining player. No separate HTTP server is required. 
Discovery exposes the sounds; use `AudioManifest.LobbyTracks` and `AudioManifest.Overrides` in the server config to choose the playlist and replacements.

Clients request missing bundles in bounded Mirror messages, verify their SHA-256, and cache them in `Application.persistentDataPath/CustomAudioCache`. 
Joining again reuses matching files. Hashes identify content and detect corruption; they do not authenticate a publisher.

For live changes, the server can call `SendCataloguesToClient.BroadcastManifest(AddressableCatalogueManager.GetServerManifest())`.

Adding files alone does not broadcast a change to already connected players.

Clients stage the new content before updating the audio registry and playlist.

## Accepted content

Custom packs use JSON catalogues and self-contained UnityFS bundles compressed with LZ4 or left uncompressed, with type trees retained. 

Each catalogue address must resolve to one prefab with exactly one bundle dependency. 

The named asset must be a single GameObject with only a Transform and one AudioSource, with a non-null AudioClip and no mixer reference or children. 

Import clips as Streaming. Clips must be at most 30 minutes, 8 channels, and 192 kHz.

The receiving client performs a managed binary preflight **before** passing the file to Unity. It allows only GameObject, Transform, AudioSource, AudioClip, and AssetBundle serialized classes. 

It rejects scripts, ScriptableObjects, TextAssets, scenes, other serialized classes, external references, extra archive files, invalid bounds, and unsupported layouts/compression. 

Streaming clip references must stay inside the bundle. It then validates the actual native objects and constructs its own locations using fixed Unity providers. Server catalogue provider/type metadata is never activated on the client. 

HTTP catalogues supplied by a server are accepted only when the same URL is already configured locally by the client.

This prevents packs from introducing executable managed assets or selecting arbitrary providers.

## Limits and failures

- Up to 256 MiB total per manifest; 128 MiB per bundle and 256 MiB expanded.
- Custom offer metadata is limited to 32 KiB UTF-8.
- Transfers use 16 KiB chunks, four per request, with a 30-second idle timeout.
- A connection may download at most 256 MiB during its lifetime, including retries.
- The client cache is limited to 512 MiB. Active session files are protected from eviction until disconnect; repeated live updates can exhaust this budget.
- Discovery allows three subfolder levels, without symlinks.
