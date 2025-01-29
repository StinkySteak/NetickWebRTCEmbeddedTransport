## Overview

Utilizing WebRTC as a transport for Netick.

If you are new to WebRTC, check out the resources section down below

### Features

| Feature                 | Description               | Status      |
|-------------------------|---------------------------|-------------|
| Native Platform Support | Based on the Unity WebRTC | Preview     |
| WebGL Support           |                           | Coming Soon |
| Connection Payload      |                           | Coming Soon |

## Installation

### Prerequisites

Unity Editor version 2021 or later.

Install Netick 2 before installing this package.
https://github.com/NetickNetworking/NetickForUnity

### Dependencies
1. [UnityWebRTC 3.0.0-pre-8](https://github.com/Unity-Technologies/com.unity.webrtc)
	- Core functionality
1. [SimpleWebTransport](https://github.com/James-Frowen/SimpleWebTransport)
	- As signaling server
1. [UnitySimulationTimer](https://github.com/StinkySteak/Unity-Simulation-Timer)
	- Timer in update loop

### Steps

- Open the Unity Package Manager by navigating to Window > Package Manager along the top bar.
- Click the plus icon.
- Select Add package from git URL
- Enter https://github.com/StinkySteak/NetickMultiplexTransport.git
- You can then create an instance by by double clicking in the Assets folder and going to `Create > Netick > Transport > MultiplexTransportProvider`

### How to Use?

| API                    | Description                                                                                            |
|------------------------|--------------------------------------------------------------------------------------------------------|
| Ice Trickling Duration | Define how long the local peer gather ice candidates after they set offer/answer for local description |
| ICE Servers            | URLs of your STUN/TURN servers                                                                         |

### Technical Design
- This implementation of WebRTC is a little different from the others, where we run the Signaling server inside unity itself.

| Inside Unity                              | Seperate Process                         |
|-------------------------------------------|------------------------------------------|
| More in control                           | Difficult to be controlled               |
| Running host is not supported in browser  | Running host may supported in browser    |
| Require Port forward the Signaling server | Port forward can be handled by the cloud |
| Less overhead                             | More overhead                            |

### Resources to learn WebRTC
- [Simple WebRTC Introduction](https://www.youtube.com/watch?v=8I2axE6j204)
- [Unity Sample](https://docs.unity3d.com/Packages/com.unity.webrtc@3.0/manual/sample.html)
