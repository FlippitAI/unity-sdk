mergeInto(LibraryManager.library, {
    convertMP3ToAudioData: function(mp3Data, dataLength, sampleRate, numChannels, gameObjectName) {
        var decodedData = atob(mp3Data);
        var dataArray = new Uint8Array(new ArrayBuffer(dataLength));

        for (var i = 0; i < dataLength; i++) {
            dataArray[i] = decodedData.charCodeAt(i);
        }

        // Envoyer les données audio au GameObject Unity
        var unityInstance = UnityLoader.instantiate(gameObjectName, gameObjectName);
        unityInstance.SendMessage("YourScriptName", "ReceiveAudioClipData", {
            channels: numChannels,
            length: dataLength,
            samples: dataArray
        });
    }
});
