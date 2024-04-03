## [1.0.3](https://github.com/adrenak/unimic/compare/v1.0.2...v1.0.3) (2020-12-03)


### Bug Fixes

* Handle when no devices connected ([2e07121](https://github.com/adrenak/unimic/commit/2e071210d09d3b769fee2b9f88207c4460a0d4c7))

## [1.0.2](https://github.com/adrenak/unimic/compare/v1.0.1...v1.0.2) (2020-04-27)


### Bug Fixes

* Update to .NET 4.x ([f7cd072](https://github.com/adrenak/unimic/commit/f7cd072445347e4365d85bd20bb6c3a5a10b08d8))

## [1.0.1](https://github.com/adrenak/unimic/compare/v1.0.0...v1.0.1) (2020-04-27)


### Bug Fixes

* Add ASMDEF ([5a42cc4](https://github.com/adrenak/unimic/commit/5a42cc4a31125a9528104a9dd169e5ddbb180f9a))

# 1.0.0 (2020-04-25)


### Features

* Mic class now only handles Mic audio streaming and does not have an internal AudioSource ([bd44c30](https://github.com/adrenak/UniMic/commit/bd44c30a48827c335c25da2027b7270356669cfe))


### BREAKING CHANGES

* Internal AudioSource and related methods deleted

![Cover](https://github.com/adrenak/UniMic/blob/master/cover.jpg)
## UniMic
A wrapper for Unity's Microphone class.

## API
`Mic` class in the `Adrenak.UniMic` namespace is a singleton and is accessed using `Mic.Instance`

### Properties
- `IsRecording` 
Returns if the Mic instance is recording audio

- `Frequency`
The frequency of the Microphone AudioClip

- `Sample`
The last populated sample of the audio data

- `SampleDurationMS`
The duration of the sample segment in milliseconds that the instance maintains and fires in events. 

- `SampleLen`
The number of samples in the sample segment

- `Clip`
The inner `AudioClip` of the instance

- `Devices`
The recording devices that are connected to the machine running the code

- `CurrentDeviceIndex`
The index of the active device in the `Devices` list

- `CurrentDeviceName`
The name of the active device


### Events
- `OnStartRecording`
Event fired when the instance starts to record the audio

- `OnStopRecording`
Event fired when the instance stops recording the audio

- `OnSampleReady`
Event fired when a sample of `SampleLen` has been populated by the instance

### Methods
- `ChangeDevice` changes the recording device. The method internally restarts the recording process
    - `Arguments`
        - `int index` the index of the device in the `Devices` list
    - `Returns`
        - `void`


- `StartRecording` starts the microphone recording
    - `Arguments`
        - `int frequency=16000` the frequency of the inner `AudioClip`
        - `int sampleLen` the length of a single sample segment that the instance keeps and fires on event
    - `Returns`
        - `void`

- `StopRecording` stops the microphone recording
    - `Returns`
        - `void`

- `GetSpectrumData` provides a block of the microphone input spectrum data.
    - `Arguments`
        - `FFTWindow fftWindow` the Fast Fourier Transform window to be used. [Info](https://docs.unity3d.com/ScriptReference/FFTWindow.html)
        - `int sampleCount` the sample count for the internal `AudioSource.GetSpectrumData` call
    - `Returns`
        - `float[]` the spectrum data

- `GetOutputData` provides a block of the microphone input output data.
    - `Arguments`
        - `int sampleCount` the sample count for the internal `AudioSource.GetOutputData` call
    - `Returns`
        - `float[]` the output data

## Tips
Just open the Unity project in Unity5+ and try the demo scene.  

## Soon  
Ready to use spectrum methods such as for standard octaves

## Contact
[@github](https://www.github.com/adrenak)  
[@www](http://www.vatsalambastha.com)
