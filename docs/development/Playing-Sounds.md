# Playing Sounds

Use the `Sound` facade (`US13.Managers.Sound`) for all sound playback. It wraps the internal `SoundManager` with a fluent builder pattern that is concise, readable, and defaults to sensible settings.

**Do not use `SoundManager` directly in new code.**

## Quick Reference

```csharp
using US13.Managers;

// Play a sound at a GameObject's position (3D spatial by default)
Sound.At(mySound, gameObject).PlayNetworked();

// Play a random sound from a list
Sound.At(mySoundList, gameObject).PlayNetworked();

// Play a global sound (heard everywhere)
Sound.Global(mySound).PlayNetworked();

// Stop a looping sound
var token = await Sound.At(mySound, gameObject).WithLooping().PlayNetworkedAsync();
Sound.Stop(token);

// Fade out and stop
Sound.Fade(token, 0f, 2f).AndStop().SendToAll();
```

## Entry Points

| Method | Description |
|--------|-------------|
| `Sound.At(sound, gameObject)` | Positional sound at the object's location. Automatically 3D spatial. Also sets the object as the sound source. |
| `Sound.At(sound, vector3)` | Positional sound at a world position. Use `.WithSource(obj)` if you need to attach it to an object. |
| `Sound.At(soundList, ...)` | Same as above but picks one sound at random from the list. |
| `Sound.Global(sound)` | Non-positional sound heard by everyone regardless of distance. |
| `Sound.Stop(token)` | Stop a playing sound by its token (networked, tells all clients). |
| `Sound.StopLocal(token)` | Stop a sound locally and return it to the pool. |
| `Sound.PauseLocal(token)` | Pause a sound locally without pooling it, so it can be resumed later. |
| `Sound.Fade(token, targetVolume, duration)` | Fade an already-playing sound from its current volume to `targetVolume` over `duration` seconds. Returns a `FadeBuilder`. |

## Builder Methods

Chain these between the entry point and the terminal method to configure the sound:

| Method | Description |
|--------|-------------|
| `.WithPitch(float)` | Fixed pitch (1.0 = normal). |
| `.WithRandomPitch(min, max)` | Random pitch in range (e.g. `0.8f, 1.2f`). |
| `.WithPitchVariation(float)` | Random pitch around 1.0 (e.g. `0.05f` = range 0.95–1.05). |
| `.WithVolume(float)` | Volume from 0 to 1. |
| `.WithMixer(MixerType)` | Output mixer (`SoundFX`, `Ambient`, `Music`, `JukeBox`, `Muffled`). |
| `.WithMaxDistance(float)` | Maximum distance the sound can be heard. |
| `.WithMinDistance(float)` | Distance at which the sound is at full volume. |
| `.WithLooping()` | Loop until stopped with `Sound.Stop(token)`. |
| `.WithSource(gameObject)` | Attach sound to an object. Useful with `Sound.At(sound, vector3)` when the position and source object differ. |
| `.AsPolyphonic()` | Allow overlapping instances (e.g. rapid footsteps). |
| `.WithFadeIn(duration)` | Fade from silence to target volume after playing. Automatically handles muting. The fade matches the terminal used — networked terminals send a fade message, `PlayLocal()` fades client-side. |

## Terminal Methods

These trigger playback. Call exactly one to finish the chain:

| Method | Returns | Description |
|--------|---------|-------------|
| `.PlayNetworked()` | `void` | Fire-and-forget. Plays for all clients. |
| `.PlayNetworkedAsync()` | `UniTask<string>` | Returns a token for stopping or modifying the sound later. |
| `.PlayNetworkedFor(player)` | `void` | Play only for a specific player. Does not support `WithFadeIn`. |
| `.PlayLocal()` | `UniTaskVoid` | Client-side only, not networked. |

## Fading Already-Playing Sounds

Use `Sound.Fade(token, targetVolume, duration)` to build a fade operation, optionally chain `.AndStop()`, then pick a delivery terminal:

| Terminal | Description |
|----------|-------------|
| `.SendToAll()` | Send fade to all clients (networked). |
| `.SendTo(recipient)` | Send fade to a specific player only. |
| `.Local()` | Run the fade client-side only (no network message). |

```csharp
// Fade out and stop (networked)
Sound.Fade(token, 0f, 2f).AndStop().SendToAll();

// Fade to half volume (networked)
Sound.Fade(token, 0.5f, 1f).SendToAll();

// Fade out for a specific player
Sound.Fade(token, 0f, 2f).AndStop().SendTo(player);

// Fade locally (no networking)
Sound.Fade(token, 0f, 1f).AndStop().Local();
```

## Common Patterns

### Simple sound at a position

```csharp
Sound.At(clickSound, gameObject).PlayNetworked();
```

### Sound with random pitch variation (e.g. impacts, hits)

```csharp
Sound.At(hitSound, gameObject).WithRandomPitch(0.8f, 1.2f).PlayNetworked();
```

### Global announcement

```csharp
Sound.Global(alarmSound).WithVolume(0.8f).PlayNetworked();
```

### Sound for a specific player

```csharp
Sound.At(bwoinkSound, gameObject).PlayNetworkedFor(player);
```

### Looping sound with fade-in (e.g. machines)

```csharp
// Start looping with a fade-in, keep the token to stop it later
loopToken = await Sound.At(humSound, gameObject)
    .WithPitch(voltageModifier)
    .WithLooping()
    .WithFadeIn(fadeInDuration)
    .PlayNetworkedAsync();

// Later, stop it abruptly
Sound.Stop(loopToken);

// Or fade it out gracefully over 2 seconds, then stop
Sound.Fade(loopToken, 0f, 2f).AndStop().SendToAll();

// Or fade to half volume without stopping
Sound.Fade(loopToken, 0.5f, 1f).SendToAll();
```

### Crossfading between tracks (e.g. a DJ mixing songs)

```csharp
// Fade out the current track while fading in the next one over 3 seconds.
Sound.Fade(trackToken, 0f, 3f).AndStop().SendToAll();
trackToken = await Sound.At(nextTrack, jukeboxObject)
    .WithMixer(MixerType.JukeBox)
    .WithFadeIn(3f)
    .PlayNetworkedAsync();
```

This composes `Fade` and `WithFadeIn`, two independent operations that can each use any terminal (networked, targeted, or local).

### Pausing and resuming a local loop (e.g. toggling a light or vendor)

```csharp
// Pause the loop without pooling it — can be resumed later
Sound.PauseLocal(loopKey);

// Or stop it entirely and return to pool
Sound.StopLocal(loopKey);
```

### Position differs from source object

```csharp
// Sound plays at the interaction target but is attached to the performer
Sound.At(sound, interaction.WorldPositionTarget)
    .WithSource(interaction.Performer)
    .PlayNetworked();
```

### Random sound from a list

```csharp
[SerializeField] private List<AddressableAudioSource> impactSounds;

Sound.At(impactSounds, gameObject).PlayNetworked();
```

## Sound Assets

Sound assets are `AddressableAudioSource` references. Common sounds are available via `CommonSounds.Instance`:

```csharp
Sound.At(CommonSounds.Instance.Click01, gameObject).PlayNetworked();
```

For custom sounds, add a serialized field to your component:

```csharp
[SerializeField] private AddressableAudioSource myCustomSound;
```

Then assign the sound asset in the Unity Inspector.

## Defaults

- `Sound.At()` defaults to **3D spatial** sound, it attenuates with distance automatically.
- `Sound.Global()` defaults to **no spatialization**, heard at full volume everywhere.
- Volume and pitch default to the audio prefab's settings unless overridden.
- Sounds are **not polyphonic** by default, playing the same sound again stops the previous instance.
