# Flippit for Unity

[![openupm](https://flippit.com)](https://openupm.com/packages/com.flippit.flippitstudio/))
[![openupm](https://img.shields.io/npm/v/com.flippit.flippitstudio?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.flippit.flippitstudio/)
[![openupm](https://img.shields.io/badge/dynamic/json?color=brightgreen&label=downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.flippit.flippitstudio)](https://openupm.com/packages/com.flippit.flippitstudio/)
<p>
# Unity SDK

<aside>
💡 This document outlines the various functionalities offered by our Unity SDK.

</aside>

*Current version*: 1.0.2.

## Main features

- Scene understanding
- Gesture generation
- Dialog generation
- Speech-to-text API
- Text-to-speech API

## Available functionalities

- Importing your characters from Flippit studio
- Re-defining characters’ settings (e.g., personality traits, backstory, goals, voice) from Unity editor
- Setting the modality of interaction for both the players and the NPC through:
    - Text
    - Audio
    - Text and audio
- Selecting NPC animations/emotes for your character
- Linking custom NPC animations to specific events/triggers
- Upload/update game lore

## Pre-requisites

### Getting your credentials

For the time being, we provide credentials on demand. 

Contact us at [founders@flippit.ai](https://www.notion.so/d98c36d57d0f4cbcb82264e8f798be48?pvs=21) to get yours! 

Once the beta of our web-based Studio will be released (Q3 2023), the credentials will be generated upon account creation.

### Unity Version

The minimum supported Unity version is 2021.3.X. Any version below that may not be compatible. Other versions of Unity will be added to the list after testing.

### Platform[](https://docs.inworld.ai/docs/tutorial-integrations/Unity/compatibility/#platform)

Here is the detailed compatibility for each platform, scripting backends, and .net levels.

| Platform | MONO | IL2CPP | .NET |
| --- | --- | --- | --- |
| Windows | ✅ | ✅ | .NET Standard 2.1 or .NET 4.x+ |
| Mac Intel 64-bit | ✅ | ✅ | .NET Standard 2.1 or .NET 4.x+ |
| Android | ❌ | ✅ | .NET 4.x+ |
| Oculus | ❌ | ✅ | .NET 4.x+ |
| iOS | N/A | ✅ | .NET 4.x+ |
| Mac Apple Silicon (M1) | Only in Editor Mode | Only in Editor Mode | .NET Standard 2.1 or .NET 4.x+ |
| Linux | ❌ | ❌ | N/A |
| WebGL | ✅ | ✅ | N/A |

### Installation

**In order to successfully use the Flippit Plugin, you should know how to:**

- Import external packages into a Unity project
- Navigate the Unity Editor interface
- Build and deploy an application to your chosen platform.

## Troubleshooting

### Newtonsoft Json[](https://docs.inworld.ai/docs/tutorial-integrations/Unity/compatibility/#newtonsoft-json)

By default, the [Newtonsoft Json](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.0/manual/index.html) package is automatically added to Unity. However, it may not be applied to some templates or may be removed from your `package.json`. If you encounter the error: `error CS2046: The type or namespace name 'NewtonSoft' could not be found` or errors related to JObject, as shown in the image below:

Please refer to [this page](https://forum.unity.com/threads/newtonsoft-json-package-missing-after-moving-project.1272587/) for more information.

### Android[](https://docs.inworld.ai/docs/tutorial-integrations/Unity/compatibility/#android)

Unity cannot proceed Android build by 2021.3.6f1. It’s a known bug for Unity. To solve this, you need to copy the whole **Tools** folder from the previous Unity version. Check [this page](https://forum.unity.com/threads/cant-build-for-android.1306098/) for more details.

### MacOS[](https://docs.inworld.ai/docs/tutorial-integrations/Unity/compatibility/#macos)

### 1. Compatibility with the MacOS build[](https://docs.inworld.ai/docs/tutorial-integrations/Unity/compatibility/#2-compatibility-with-the-macos-build)

This is a known bug for [Unity](https://issuetracker.unity3d.com/issues/macos-build-fails-with-command-failing-to-write-to-output-file-when-using-3rd-party-plugin), it has been fixed in version 2022.2.X.

### 2. Microphone Usage Description[](https://docs.inworld.ai/docs/tutorial-integrations/Unity/compatibility/#3-microphone-usage-description)

If you want to build an iOS app, please fill in `Microphone Usage Description` under `Project Settings > Player > iOS > Other Settings > Configuration`.

Also, for the default iOS app, the sound only comes out of the earpiece and may be relatively quiet. To output sound from the loudspeaker, you need to set `Force IOS Speakers When Recording` as well.

!https://docs.inworld.ai/assets/images/iOSSpeaker-168ff4cff1aebb74377eb266689ce9fa.png

# How to install the Flippit Plugin ?

To install the Flippit plugin, you must use the Unity Package Manager.

Here are the different steps:

1. In Unity, go to Window> Package Manager

![image](https://s3-us-west-2.amazonaws.com/secure.notion-static.com/8d998b6d-aa4b-45a3-9456-b273b2ae9a9b/Capture_decran_2023-07-13_a_14.20.44.png)

1. In the Package Manager, Click on the “+” icon in the top left and select “Add package from disk”. 

![image](https://s3-us-west-2.amazonaws.com/secure.notion-static.com/7f61954e-8f30-4227-aea6-08bb5a55a575/Capture_decran_2023-07-13_a_14.18.13.png)

1. Select the package.json file

# Menus

Once the Flippit plugin is installed, a Flippit tab is displayed in the main menu of Unity. 

It contains 2 possible menus:

https://lh3.googleusercontent.com/4JYV4K-gq6AEEPlP4xL0rov2RKvwwFK-P_2xzlkUVjLZQM_6s6Joth02OafAc6n40nf2EvJcMbYtGqsJL3AW0YQ2yTBtaEWMu4dyJlp94r8DOZCulev0NfWuX3Ne9SvsFeBuErvbcwyqmY0qRhRpqdw

- **Studio** - This menu is useful to:
    - Access and download all your characters created in the [Flippit studio platform](https://studio.flippit.ai/) into your Unity scene.
    - Update already imported character’s information from Flippit Studio to Unity
    - Update already imported character’s information from Unity to Flippit Studio
- **Character Converter** - This menu is useful to:
    - Convert existing characters that were not created through Flippit Studio from your Unity scene into AI characters. This will also upload them into your Flippit Studio account to ease the edition of their personalities.

## Studio Menu

1. First, enter your credentials to connect to your Flippit Studio account.

![image](https://s3-us-west-2.amazonaws.com/secure.notion-static.com/a5d50294-c8bc-415e-bd4c-674169dd40aa/Capture_decran_2023-07-13_a_16.02.03.png)

1. Once connected, all the characters from your Flippit Studio account are displayed as follows:

![image](https://s3-us-west-2.amazonaws.com/secure.notion-static.com/db8ffd79-613a-49c9-ab57-6fc0d9c439a1/Capture_decran_2023-07-13_a_16.10.23.png)

For each character you can:

- Import it into your scene by clicking on its image
- Update its characteristics in Unity with the changes made in Flippit studio (if already imported) by clicking on the icon “**Update**” on the top left of the image
- Delete its characteristics in Unity (if already imported) by clicking on the icon “**Delete**” on the top right of the image
    - This will not delete the 3D asset nor the character in Flippit Studio.

## Character converter Menu

If you already have existing characters in your Unity scene, this menu allows you to convert them into AI characters or Player in simple steps:

### The “Convert to Smart NPC” button

1. Select the object of your 3D model in your scene
2. Open the Character Converter Menu
3. Fill in all the relevant information

![image](https://s3-us-west-2.amazonaws.com/secure.notion-static.com/6d9e5300-9e91-4418-8a2b-cb9a8c82bd6b/Capture_decran_2023-07-13_a_15.34.47.png)

1. Click on the “Convert to Smart NPC”

This action will:

- Turn your character into an AI character
- Upload your new AI character into Flippit Studio
    - You can now edit it directly from our web interface

### The “Convert to Player” button

This button allows you to quickly create a Player from your 3D model so you can also directly go and talk to your new AI Character (mind that depending on the specific rig of the character you convert some animations may fail to play).

No name, backstory, or any kind of other parameter is needed for this action.

Creating this Player implies that the following are created:

- Tag: to identify the player(s) in the area of interaction of the AI characters
- Physic collider and rigid Body: for movements, driven by the Player Script.

The player Script has its own dependencies to work with Flippit.

![image](https://s3-us-west-2.amazonaws.com/secure.notion-static.com/917a9492-287d-4734-826e-28cc2498dc90/Capture_decran_2023-07-13_a_15.20.09.png)

# Interacting with your AI characters

Once you have added an AI character to your Unity scene, you can directly play and talk to it!

Before starting, you may want to adjust settings to change the way you will interact with it. 

These settings can be changed through the Dialogue Panel object in your hierarchy:

![image](https://s3-us-west-2.amazonaws.com/secure.notion-static.com/8c5c3bb3-6080-4d81-9e0f-dfb76f69a3fd/Capture_decran_2023-07-13_a_16.25.48.png)

Once selected, you can modify the following options in the inspector. 

![image](https://s3-us-west-2.amazonaws.com/secure.notion-static.com/1fdb8742-cf93-43dc-9b6c-5cdee07fc13a/Capture_decran_2023-07-13_a_15.28.13.png)

1. The player can engage (see input options) in the discussion using:
    1. Text
    2. Audio
    3. Text or audio
2. The AI character can reply (output options) using:
    1. Text
    2. Audio
    3. Text and audio
3. UX/UI settings:
    1. What button to press to start talking
    2. How to exit the discussion
    3. Max duration for recording the voice
    4. Size of the chat window

You can also set the different objects in the scene which should be reachable by your AI character as follows:

1. Click on your AI character object in the hierarchy
2. Drag and drop the object into the “Quest object” field in the inspector

![image](https://s3-us-west-2.amazonaws.com/secure.notion-static.com/02930313-6291-4483-9012-5f7b2479e621/Capture_decran_2023-07-13_a_16.24.26.png)

Finally, you may change the camera options.

By default, we used the power of Cinemachine to follow the player's movements.

If you don’t have a camera set in your scene, using the [“Convert to Player” button](https://www.notion.so/659d18b45f40468eb13a922cc2d7f2a7?pvs=21) will check if the essential game objects are available in the scene, and create the missing ones.

This way, Camera, virtual camera, and Dialogue Panel will be added to your scene in one click.

You still have the possibility to change the following settings:

- Camera Offset target: it is used to offset the camera when you enter a discussion
- Camera Offset duration: it is used to define the time to translate the offset.

# Understanding the AI characters’ settings

Your AI characters will contain the following components:

- Unity components: these ones are useful to allow them to move in the 3D space.

https://lh6.googleusercontent.com/iktWN4l7epGPGvTYwC9QklKR0mQKYQJyF0xTVvoyEQFQ6K2d5BEuelfGMcGRZXKjbr94le6jSbPRu_vpEi-GPn1t-VCX6AlIkFeWMJb1la688IsqLGI6SqiCo-R68DoBdg6GJoymiBBFyDtcM41HY6Q

- Flippit components: these ones are useful to make sure your character is able to interact

![image](https://s3-us-west-2.amazonaws.com/secure.notion-static.com/468226ae-4b1d-46b0-aae9-7839fe18b12d/Capture_decran_2023-07-13_a_16.27.47.png)

- AI Character script: it refers to the personality of the character, which is editable.
    - If edited, make sure to [update](https://www.notion.so/659d18b45f40468eb13a922cc2d7f2a7?pvs=21) Flippit Studio for these changes to be taken into account
- Detector: it is a child object with a sphere collider as Trigger. It is used to detect if the player is entering the area of interaction.
- Avatar: It refers to the mesh to animate.
- Quest Objects: It allows the AI character to walk to multiple specific locations if requested through the chat
    - Note: make sure to name the objects properly so that our AI engine can understand your intention and determine whether or not an action towards a specific object is needed
- TTS (Script): It allows your AI character to use AI-generated voices to make your character's speech
    - The Audio source component has to be attached to it

# Project nomenclature

- The core of the plugin is stored in: **Packages > Flippit Studio**
    - It contains the Editor and Runtime folders that unlock all the features displayed previously.
- All the assets created are stored in: **Assets > Flippit**
    - All of the following folders are created by default if they do not exist
    - The Resources folder contains all the objects related to your AI characters:
        - Personalities: all the characteristics defined for your avatar (eg, backstory)
            - You can edit them directly in Unity —> don’t forget to update Flippit studio
        - Prefabs: the 3D models properly rigged
        - Thumbnails: the thumbnails to display them in your library

# Contacts

If you have any questions or issues, please reach out to lucien@flippit.ai, we will be happy to assist you! Happy Unity Integration 😄
</p>
