# Universal Gestures: Unity Client

Unity/C# repository for the Universal Gestures XR client. This is where we house the developer-facing features of the Universal Gestures Package. See the [Technical Specification](https://docs.google.com/document/d/1wDUTpCBaXz3XE8t48t-PqcnbpXUm-zK26sfdVPcba5U/edit?usp=sharing) for more details.

*Related:* [Universal Gestures Lab](https://github.com/uwrealitylabs/universal-gestures-lab) (ML Repository) and Scrum Board.

# Setup

## Unity 2022.3
Unity is the editor that we use to create 3D/XR applications. We will be using it as our engine for the project. Download the [Unity Hub](https://unity.com/download), then use it to install Editor version 2022.3.x (the x indicates that any variant of 2022.3 works fine). Create a Unity account to go along with your install.

Clone the repository, then open Unity Hub on your computer. Choose "Add" and select the cloned folder. This will add the Universal Gestures project to your Unity Hub, where you can then launch the project.

## Oculus PC App
If you want to run the project on a Quest headset to test its functionality, you will need to install the [Oculus PC App](https://www.meta.com/help/quest/articles/headsets-and-accessories/oculus-rift-s/install-app-for-link/). Note that this only supports Windows PCs. To connect your Quest headset to your PC, you can use a Link Cable (requires USB3.0 port on your computer) or Air Link (requires strong Wifi connection that Quest can independently connect to: eduroam is not compatible).

Once connected to your PC, launch Link Mode on your Quest if it's not automatically started. In Unity, ensure Oculus is added to the list of active loaders (Edit > Project Settings > Meta XR). Then, you can simply start Play Mode and the app will be displayed to the Quest headset.
