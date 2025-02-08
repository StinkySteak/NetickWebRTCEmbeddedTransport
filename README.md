## Overview

Utilizing WebRTC as a transport for Netick. Allowing Developers to Utilize DTLS (Secure UDP) for WebGL and Native platform!

If you are new to WebRTC, check out the resources section down below

### Features

| Feature        | Description                                  | Status       |
|----------------|----------------------------------------------|--------------|
| Native Support | Based on the Unity WebRTC supported platform | Beta |
| WebGL Support  | WebGL acting as a client                     | Beta |

## Installation

### Prerequisites

Unity Editor version 2021 or later.

Install Netick 2 before installing this package.
https://github.com/NetickNetworking/NetickForUnity

### Dependencies
1. [UnityWebRTC 3.0.0-pre-8](https://github.com/Unity-Technologies/com.unity.webrtc) (Core functionality)
1. [SimpleWebTransport](https://github.com/James-Frowen/SimpleWebTransport) (As Signaling Server)
1. [UnitySimulationTimer](https://github.com/StinkySteak/Unity-Simulation-Timer) (Update Loop Timer)
1. [Newtonsoft Json Unity](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html)

### Steps

- Open the Unity Package Manager by navigating to Window > Package Manager along the top bar.
- Click the plus icon.
- Select Add package from git URL
- Enter https://github.com/StinkySteak/NetickMultiplexTransport.git
- You can then create an instance by double clicking in the Assets folder and going to `Create > Netick > Transport > NetickWebRTCTransport`

### How to Use?

| API                    | Description                                                                                            |
|------------------------|--------------------------------------------------------------------------------------------------------|
| Timeout Duration | Define how long timeout will be called upon failed to connect |
| ICE Servers            | URLs of your STUN/TURN servers                                                                         |

### Technical Design
- This implementation of WebRTC is a little different from the others, where we run the Signaling server inside unity itself.

#### Seperate Process
![Preview](https://github.com/StinkySteak/NetickWebRTCTransport/blob/docs/tech_design_seperate.png)

#### Inside Unity
![Preview](https://github.com/StinkySteak/NetickWebRTCTransport/blob/docs/tech_design_unified.png)


| Inside Unity                              | Seperate Process                         |
|-------------------------------------------|------------------------------------------|
| More in control                           | Difficult to be controlled               |
| Running host is not supported in browser  | Running host may supported in browser    |
| Require Port forward the Signaling server | Port forward can be handled by the cloud |
| Less overhead                             | More overhead                            |

### Resources to learn WebRTC
- [Simple WebRTC Introduction](https://www.youtube.com/watch?v=8I2axE6j204)
- [Unity Sample](https://docs.unity3d.com/Packages/com.unity.webrtc@3.0/manual/sample.html)
