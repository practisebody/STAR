# STAR

System for Telementoring with Augmented Reality. Mentee subsystem.

* Homepage: <https://engineering.purdue.edu/starproj/>
* Mentor subsystem: <https://github.com/edkazar/MentorSystemUWPWebRTC>

## Installing

### Prerequisites

A PC and a HoloLens. Follow the instruction in <https://docs.microsoft.com/en-us/windows/mixed-reality/using-the-windows-device-portal> to set up Windows Device Portal for the HoloLens.

### Getting Started

1. Get the app
	1. On the PC, download the latest release at <https://github.com/practisebody/STAR/releases>
	2. Unzip STAR_x.x.x.x_Test.zip and locate STAR_x.x.x.x_x86.appxbundle
2. Connect to HoloLens
    1. Using Windows Device Portal to connect the PC to the HoloLens, either over Wi-Fi or over USB
3. Upload to HoloLens. *Note: This part might be not accurate if Windows Device Portal updates.*
	1. Using Windows Device Portal, click on "Views > Apps > Deploy apps"
	2. Click on "Choose File", select the .appxbundle file in step 1
	3. Install

## Compling

It is not recommended to compile from scratch. Follow the steps below when necessary.

### Prerequisites

* Unity (2017.4.3f1 or later)
* Visual Studio (2017, 15.9.9 or later), with the following components
	* Universal Windows Platform development
	* Game development with Unity

Also check <https://docs.microsoft.com/en-us/windows/mixed-reality/install-the-tools> of detailed instructions.

### Dependency
The following libraries are already included, listed here for the sake of completeness
* MixedRealityToolkit (used to called HoloToolkit)
	* Homepage: <https://github.com/Microsoft/MixedRealityToolkit-Unity>
	* Using 2017.4.0.0 <https://github.com/microsoft/MixedRealityToolkit-Unity/releases/tag/2017.4.0.0>
* HoloPoseWebRtc
	* Homepage: <https://github.com/DanAndersen/HoloPoseWebRtc>
	* Using commit 30651138c9: <https://github.com/DanAndersen/HoloPoseWebRtc/tree/30651138c916d1842e9608f4dfa5b38aad36b2cb>

### Getting Started

1. Download or "git clone" this repository
2. Build Visual Studio project by Unity
	1. Use Unity to open the project. *Note: The first time to open the project may take a while load all the files*
	2. Open "Scenes/Main" from project window
	3. Click on "File > Build Settings", choose "Universal Windows Platform" and click "Switch Platform"
	4. Check "Debugging > Unity C# Projects" and close the window
	5. If using later unity version, check "Edit > Project Settings > Player > Other Settings > Allow unsafe code". *Note: this option is not available in 2017.4.3f1*
	6. Click on "Mixed Reality Toolkit > Build Window", click "Build Unity Project"
3. Run app by Visual Studio
	1. Open "UWP/STAR.sln" using Visual Studio
	2. Connect HoloLens to the PC over Wi-Fi or over USB
	3. In Configuration Manager, switch to "Release" and "x86". Select "Remote Machine" and put IP address of HoloLens if connected over Wi-Fi, select "Device" if connected over USB
	4. In Solution Explorer, right click on "Assembly-CSharp", open "Properties", check "Build > Allow unsafe code". *Note: this step is not necessary if step 2e was performed*
	5. Click "Debug > Start Debugging"