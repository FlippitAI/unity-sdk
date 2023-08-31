# Flippit for Unity

<p>
Please visit the online documentation and join our public `discord` community.

![](https://i.imgur.com/zGamwPM.png) **[Online Documentation](https://flippitai.notion.site/Unity-SDK-fb1a3d5acfbb433e9334ffb124b8800c )**
</p>

## Quick Install
### Requirements
Unity Version 2020.3 or higher

Git needs to be installed to fetch the Unity package. [Download here](https://git-scm.com/downloads)

1. To add the Flippit Unity SDK to your project please use the Unity Package Manager to import the package directly from the Git URL.

2. Within your Unity Project, open up the Package Manager window by going to Window > Package Manager.

3. In the Package Manager window click on the + icon on the top left corner and select Add Package From Git URL.

![Screen2023-08-25 15 00 11](https://github.com/FlippitAI/unity-sdk/assets/1887378/0401e12a-253e-4e3e-9188-bc641bef40ee)

4. Paste the following URL:

https://github.com/FlippitAI/unity-sdk.git

![Screen2023-08-25 15 00 23](https://github.com/FlippitAI/unity-sdk/assets/1887378/811166a7-7a9e-46fe-915a-52bff5a9bba0)

5. Click add and wait for the import process to finish.

6. After completing the process, your project will likely show errors because of some missing dependencies.
Repeat the procedure to add the following packages through the Package Manager with git URLs:
   
   https://github.com/readyplayerme/rpm-unity-sdk-core.git --> This installs the Ready Player Me package. It is necessary to download your characters from Flippit Studio
   
   https://github.com/endel/NativeWebSocket.git#upm --> This installs a package to handle WebSocket connections. It is necessary to interact with our technology.
   
   https://github.com/srcnalt/OpenAI-Unity.git --> This installs a plugin to handle communication with OpenAI. It is necessary to use Speech-To-Text (Whisper model)

   https://github.com/atteneder/glTFast.git --> This installs the glTFast plugin for Unity.
   
7. Once installed, the last step is to create an additional Tag named "Flippit/NPC". It will be used by all your characters. You only need to do it once at the first character creation (see screenshots).
   
![Screen2023-08-25 13 44 03](https://github.com/FlippitAI/unity-sdk/assets/1887378/f8b730d2-ad73-4e3c-a111-bbd9c2159589)

![Screen2023-08-25 13 45 45](https://github.com/FlippitAI/unity-sdk/assets/1887378/5e5db71e-b64b-4a56-9b84-8cdc0be12464)

8. To start your first test with smart NPCs, you need a small area for ground, create a simple plane, scale it by 10, and then, mark it as static

![Screen2023-08-31 14 06 11](https://github.com/FlippitAI/unity-sdk/assets/1887378/36bf9c5d-b395-4cb3-a1da-51b6dc75f976)

9. Open the menu Window>Ai> Navigation and then Bake the Navmesh.

![Screen2023-08-31 14 09 50](https://github.com/FlippitAI/unity-sdk/assets/1887378/959818fc-c200-46d2-8bdb-d0c806f31a6d)

![Screen2023-08-31 14 09 35](https://github.com/FlippitAI/unity-sdk/assets/1887378/887d3ee3-9a94-4a7e-8dc1-56baa3e4b7e4)

10. are now ready to add the player character. You can use any kind of GameObject or animated character.

For this exemple, we will use a flippit NPC, 
![Screen2023-08-31 14 21 19](https://github.com/FlippitAI/unity-sdk/assets/1887378/577ce0b5-5ceb-45ca-aa57-617f86204323)


Unpack the prefab,
![Screen2023-08-31 14 21 36](https://github.com/FlippitAI/unity-sdk/assets/1887378/125ccb73-3ce2-4452-b8f7-06ed588ec2df)


and convert it as Player
![Screen2023-08-31 14 22 07](https://github.com/FlippitAI/unity-sdk/assets/1887378/b82e9cc6-4b43-4575-94a0-2b595e94cf67)

Once done, we have our Player character created with all usefull assets (camera, canvas with dialogue box, and controls)

