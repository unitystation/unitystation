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

- `SampleLength`
The number of samples in the sample segment

- `AudioClip`
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
Event fired when a sample of `SampleLength` has been populated by the instance. 
Includes the sample count.

- `OnTimestampedSampleReady`
Event fired when a sample of `SampleLength` has been populated by the instance. 
Includes the timestamp from when the sample was captured.

### Methods
- `SetDeviceIndex` changes the recording device. The method internally restarts the recording process
    - `Arguments`
        - `int index` the index of the device in the `Devices` list
    - `Returns`
        - `void`


- `StartRecording` starts the microphone recording
    - `Arguments`
        - `int frequency=16000` the frequency of the inner `AudioClip`
        - `int sampleDurationMS` the duration of a single sample segment in milliseconds that the instance keeps and fires on event
    - `Returns`
        - `void`

- `ResumeRecording` resumes the microphone recording at the frequency and sampleDurationMS 

- `StopRecording` stops the microphone recording
    - `Returns`
        - `void`

## Tips
Just open the Unity project in Unity 2017.4.40f1+ and try the sample scene.  

## Contact
[@github](https://www.github.com/adrenak)  
[@www](http://www.vatsalambastha.com)