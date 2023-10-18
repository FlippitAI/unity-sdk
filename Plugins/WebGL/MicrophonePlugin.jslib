var MicrophonePlugin = 
{
	$MicrophoneWebGL: 
    {
	audioContext: null,
	devices: {},
	},

  MicrophoneWebGL_FetchMicrophoneDevices: async function (resolve, reject) {
 
      function getPtrFromString(str) {
          var bufferSize = lengthBytesUTF8(str) + 1;
          var buffer = _malloc(bufferSize);
          stringToUTF8(str, buffer, bufferSize);
          return buffer;
      }
      
     navigator.mediaDevices.enumerateDevices().then(devices => {
         let audioDevices = devices.filter(device => device.kind === 'audioinput');
         let audioDeviceNames = audioDevices.map(device => device.label || 'Default').join('|');
         var buffer = getPtrFromString(audioDeviceNames);
         dynCall_vi(resolve, buffer);
     }).catch(error => {
         var buffer = getPtrFromString(error.toString());
         console.log('Error fetching microphone devices:', error);
         dynCall_vi(resolve, buffer);
     });
  },

  MicrophoneWebGL_Start: async function(deviceNamePtr, loop, lengthSec, frequency, channelCount, callback, deleteCallback) {

    function getPtrFromString(str) {
      var bufferSize = lengthBytesUTF8(str) + 1;
      var buffer = _malloc(bufferSize);
      stringToUTF8(str, buffer, bufferSize);
      return buffer;
   }

    const contextConstraints = {
      sampleRate: frequency,
      channelCount: channelCount,
      //echoCancellation: false,
      //autoGainControl: true,
      //noiseSuppression: true,
    };
    let deviceName = UTF8ToString(deviceNamePtr);
    
    const maxSamples = frequency * lengthSec;
    const chunkTime = 10; // ms
    const maxChunks = lengthSec / (chunkTime / 1000);

    async function _UpdateData(device) {
      let blob = new Blob(device.chunks, { 'type': 'audio/ogg; codecs=opus' });
      let alignedSize = Math.floor(blob.size / 4) * 4;
      let alignedBlob = await blob.slice(0, alignedSize).arrayBuffer();
      let buf = new Uint8Array(alignedBlob);
      let audioBuffer = await device.audioContext.decodeAudioData(buf.buffer);
      let offlineContext = new OfflineAudioContext(audioBuffer.numberOfChannels, audioBuffer.length, audioBuffer.sampleRate);
      let source = offlineContext.createBufferSource();
      source.buffer = audioBuffer;
      source.connect(offlineContext.destination);
      source.start(0);
  
      let channels = device.channelCount;
      let renderedBuffer = await offlineContext.startRendering();
      device.samples = _CreateInterleavedBuffer(channels, renderedBuffer);
      let samplesSize = device.chunks.map(s => s.size / 4).reduce((a, b) => a + b, 0);
      device.position = samplesSize;
      
      dynCall_vi(callback, getPtrFromString(deviceName));
      device.remaining--;
      if (!device.isRecording && device.remaining === 0) {
        dynCall_vi(deleteCallback, getPtrFromString(deviceName));
      }
    }

    function _CreateInterleavedBuffer(channelCount, renderedBuffer) {
      let numberOfChannels = channelCount;
      let channelData = [];
      let sampleLength = renderedBuffer.length;
      
      // Get channel data
      for (let channel = 0; channel < numberOfChannels; channel++) {
          channelData.push(renderedBuffer.getChannelData(channel));
      }
      
      // Create an interleaved buffer
      let interleavedBuffer = new Float32Array(sampleLength * numberOfChannels);
      
      for (let i = 0; i < sampleLength; i++) {
          for (let channel = 0; channel < numberOfChannels; channel++) {
              interleavedBuffer[i * numberOfChannels + channel] = channelData[channel][i];
          }
      }
      return interleavedBuffer;
    }
    
    var existing = MicrophoneWebGL.devices[deviceName];
    var device = existing ? existing : {};
    if (device.isRecording) {
      console.warn('Microphone is already recording.');
      return;
    }
    device.audioContext = new AudioContext(contextConstraints);
    device.channelCount = channelCount;
    device.position = 0;
    device.isRecording = true;
    device.chunks = [];
    device.close = () => {
          if (!device.isRecording) return;
          device.mediaRecorder.stop();
          device.isRecording = false;
          device.stream.getTracks().forEach(track => track.stop());
    };

    device.stream = await navigator.mediaDevices.getUserMedia({
      audio: {
        sampleRate: frequency,
        channelCount: channelCount
      },
      video: false,
      deviceId: {
        exact: deviceName,
      },
    });

    if (device.stream == null) {
      console.error("Failed to find device with the requested characteristics");
      return null;
    }

    MicrophoneWebGL.devices[deviceName] = device;

    const options = {
      channelCount: channelCount,
      sampleRate: frequency,
      mimeType: 'audio/webm;codecs=pcm',
    };

    device.mediaRecorder = new MediaRecorder(device.stream, options);
    device.mediaRecorder.addEventListener("dataavailable", async (e) => {
      device.remaining++;
      device.chunks.push(e.data);
      await _UpdateData(device);
      if (device.chunks.length >= maxChunks) {
          if (!loop) {
              device.close();
              console.log("Closed device " + deviceName + " recorded " + device.chunks.length + " chunks");
          } else {
              device.chunks = [];
              device.position = 0;
              console.log("Shifted chunks, now we have " +  device.chunks.length);
          }
        }
    });
    device.mediaRecorder.start(chunkTime);
  },

  MicrophoneWebGL_GetData: function(deviceNamePtr, samples, samplesLength, offset) 
  {
    let deviceName = UTF8ToString(deviceNamePtr);
    let device = MicrophoneWebGL.devices[deviceName];
    if (device == null) 
    {
      console.warn("Device has not started recording yet")
      return null;
    }

    var samplesArray = new Float32Array(Module.HEAP8.buffer, samples, samplesLength);
    samplesArray.set(device.samples.slice(offset, samplesLength));
  },

  // Get current recording position
  MicrophoneWebGL_GetPosition: function(deviceNamePtr) {
    let deviceName = UTF8ToString(deviceNamePtr);
    let device = MicrophoneWebGL.devices[deviceName];
    if (device == null) {
      console.warn("Device has not started recording yet")
      return null;
    }
    return device.position;
  },
  
  MicrophoneWebGL_IsRecording: function(deviceNamePtr) {
      let deviceName = UTF8ToString(deviceNamePtr);
      let device = MicrophoneWebGL.devices[deviceName];
      if (device == null) {
        return false;
      }
  
      return device.isRecording;
    },

  // End microphone recording
  MicrophoneWebGL_End: function(deviceNamePtr) {
    let deviceName = UTF8ToString(deviceNamePtr);
    let device = MicrophoneWebGL.devices[deviceName];
    if (device == null) {
      console.warn("Device has not started recording yet")
      return null;
    }

    if (!device.isRecording) {
      console.warn('Microphone is not recording.');
      return;
    }

    device.close();
  },
  
  MicrophoneWebGL_GetPermission: async function(deviceNamePtr) {
    let deviceName = UTF8ToString(deviceNamePtr);
    let stream = await navigator.mediaDevices.getUserMedia({
        audio: true,
        deviceId: {
          exact: deviceName,
        },
    });
  },
    
    MicrophoneWebGL_HasPermission: async function(deviceNamePtr) {
        let deviceName = UTF8ToString(deviceNamePtr);
        return false;
    },
  

};

autoAddDeps(MicrophonePlugin, '$MicrophoneWebGL');
mergeInto(LibraryManager.library, MicrophonePlugin);
