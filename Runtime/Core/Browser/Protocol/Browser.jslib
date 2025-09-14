const PeerList = {
    peers: [],
    index: 0,

    AddNext: function () {
        const peer = {

            index: 0,
            peerConnection: null,
            dataChannel: null,
            dataChannelReliable: null,

            offerJson: null,
            answerJson: null,

            opCreateOffer: null,
            opCreateOfferDone: null,

            opCreateAnswer: null,
            opCreateAnswerDone: null,

            opSetLocalDescription: null,
            opSetLocalDescriptionDone: null,

            opSetRemoteDescription: null,
            opSetRemoteDescriptionDone: null,

            onMessageCallback: null,

            onIceConnectionStateChangeCallback: null,
            onIceCandidateCallback: null,
            onIceCandidateGathertingStateCallback: null,

            onDataChannelOpenCallback: null,
            onDataChannelCreatedCallback: null,

            onDataChannelReliableOpenCallback: null,
            onDataChannelReliableCreatedCallback: null,
        }

        let indexNow = this.index;

        this.peers[indexNow] = peer;
        peer.index = indexNow;
        this.index++;

        return indexNow;
    },

    GetPeer: function (index) {
        return this.peers[index];
    }
}

function WebRTC_Unsafe_CreateRTCPeerConnection(configJson) {

    let index = PeerList.AddNext();
    let peer = PeerList.GetPeer(index);

    const json = UTF8ToString(configJson);

    let config = JSON.parse(json);
    peer.peerConnection = new RTCPeerConnection(config);

    peer.peerConnection.onicegatheringstatechange = (event) => {
        if (peer.onIceCandidateGathertingStateCallback) {

            let stateNumber = 0;

            switch (peer.peerConnection.iceGatheringState) {
                case "new":
                    stateNumber = 0;
                    break;
                case "gathering":
                    stateNumber = 1;
                    break;
                case "complete":
                    stateNumber = 2;
                    break;
            }

            Module.dynCall_vi(peer.onIceCandidateGathertingStateCallback, stateNumber);
        }
    };

    peer.peerConnection.onicecandidate = (event) => {

        if (peer.onIceCandidateCallback)
            Module.dynCall_vi(peer.onIceCandidateCallback, peer.index);
    };

    peer.peerConnection.oniceconnectionstatechange = (event) => {
        if (peer.onIceConnectionStateChangeCallback)
            Module.dynCall_vi(peer.onIceConnectionStateChangeCallback, peer.index);
    };

    peer.peerConnection.ondatachannel = (event) => {

        if (event.channel.label == "sendChannel") {
            peer.dataChannel = event.channel;

            if (peer.onDataChannelCreatedCallback) {
                Module.dynCall_vi(peer.onDataChannelCreatedCallback, peer.index);
            }


            peer.dataChannel.onopen = (event) => {
                if (peer.onDataChannelOpenCallback)
                    Module.dynCall_vi(peer.onDataChannelOpenCallback, peer.index);
            };

            peer.dataChannel.onmessage = (event) => {

                if (event.data instanceof ArrayBuffer) {
                    let array = new Uint8Array(event.data);
                    let arrayLength = array.length;

                    var ptr = Module._malloc(arrayLength);

                    let dataBuffer = new Uint8Array(HEAPU8.buffer, ptr, arrayLength);

                    Module.HEAPU8.set(dataBuffer, ptr);

                    dataBuffer.set(array);


                    if (peer.onMessageCallback)
                        Module.dynCall_viii(peer.onMessageCallback, peer.index, ptr, dataBuffer.length);
                }
            };
        }
        else if (event.channel.label == "sendChannelReliable") {
            peer.dataChannelReliable = event.channel;

            if (peer.onDataChannelReliableCreatedCallback) {
                Module.dynCall_vi(peer.onDataChannelReliableCreatedCallback, peer.index);
            }


            peer.dataChannelReliable.onopen = (event) => {
                if (peer.onDataChannelReliableOpenCallback)
                    Module.dynCall_vi(peer.onDataChannelReliableOpenCallback, peer.index);
            };

            peer.dataChannelReliable.onmessage = (event) => {
                if (event.data instanceof ArrayBuffer) {
                    let array = new Uint8Array(event.data);
                    let arrayLength = array.length;

                    var ptr = Module._malloc(arrayLength);

                    let dataBuffer = new Uint8Array(HEAPU8.buffer, ptr, arrayLength);

                    Module.HEAPU8.set(dataBuffer, ptr);

                    dataBuffer.set(array);

                    if (peer.onMessageCallback)
                        Module.dynCall_vii(peer.onMessageCallback, peer.index, ptr, dataBuffer.length);
                }
            };
        }
    };

    return index;
};

function WebRTC_Unsafe_GetConnectionState(index) {

    let peer = PeerList.GetPeer(index);
    let connectionState = peer.peerConnection.connectionState;

    const pointer = _malloc(connectionState.length + 1); // +1 for null terminator
    stringToUTF8(connectionState, pointer, connectionState.length + 1);
    return pointer;
};

function WebRTC_IsConnectionOpen(index) {

    let peer = PeerList.GetPeer(index);

    if (peer.dataChannel == null)
        return false;

    if (peer.dataChannel.readyState === "open")
        return true;

    return false;
};

function WebRTC_DataChannelSend(index, dataPointer, dataLength) {

    let peer = PeerList.GetPeer(index);

    const byteArray = new Uint8Array(Module.HEAPU8.buffer, dataPointer, dataLength);

    peer.dataChannel.send(byteArray);
};

function WebRTC_DataChannelReliableSend(index, dataPointer, dataLength) {

    let peer = PeerList.GetPeer(index);

    const byteArray = new Uint8Array(Module.HEAPU8.buffer, dataPointer, dataLength);

    peer.dataChannelReliable.send(byteArray);
};

function WebRTC_GetOpCreateOfferIsDone(index) {
    let peer = PeerList.GetPeer(index);
    return peer.opCreateOfferDone;
};

function WebRTC_GetOpCreateAnswerIsDone(index) {
    let peer = PeerList.GetPeer(index);
    return peer.opCreateAnswerDone;
};

function WebRTC_DisposeOpCreateOffer(index) {
    let peer = PeerList.GetPeer(index);
    peer.opCreateOffer = null;
    peer.opCreateOfferDone = false;
};

function WebRTC_DisposeOpCreateAnswer(index) {
    let peer = PeerList.GetPeer(index);
    peer.opCreateAnswer = null;
    peer.opCreateAnswerDone = false;
};

function WebRTC_HasOpCreateOffer(index) {
    let peer = PeerList.GetPeer(index);
    return peer.opCreateOffer != null;
};

function WebRTC_HasOpCreateAnswer(index) {
    let peer = PeerList.GetPeer(index);
    return peer.opCreateAnswer != null;
};

async function WebRTC_CreateOffer (index) {
    let peer = PeerList.GetPeer(index);
    peer.opCreateOffer = peer.peerConnection.createOffer();
    let offer = await peer.opCreateOffer;
    peer.offerJson = JSON.stringify(offer);
    peer.opCreateOfferDone = true;
};

async function WebRTC_CreateAnswer (index) {
    let peer = PeerList.GetPeer(index);
    peer.opCreateAnswer = peer.peerConnection.createAnswer();
    let answer = await peer.opCreateAnswer;
    peer.answerJson = JSON.stringify(answer);
    peer.opCreateAnswerDone = true;
};

function WebRTC_Unsafe_CreateDataChannel(index, configJson) {

    let peer = PeerList.GetPeer(index);
    const json = UTF8ToString(configJson);

    let config = JSON.parse(json);

    peer.dataChannel = peer.peerConnection.createDataChannel("sendChannel", config);

    peer.dataChannel.onopen = (event) => {
        if (peer.onDataChannelOpenCallback)
            Module.dynCall_vi(peer.onDataChannelOpenCallback, peer.index);
    };

    peer.dataChannel.onmessage = (event) => {
        if (event.data instanceof ArrayBuffer) {
            let array = new Uint8Array(event.data);
            let arrayLength = array.length;

            var ptr = Module._malloc(arrayLength);

            let dataBuffer = new Uint8Array(HEAPU8.buffer, ptr, arrayLength);

            Module.HEAPU8.set(dataBuffer, ptr);

            dataBuffer.set(array);

            if (peer.onMessageCallback)
                Module.dynCall_viii(peer.onMessageCallback, peer.index, ptr, dataBuffer.length);
        }
    };
};

function WebRTC_Unsafe_CreateDataChannelReliable(index) {

    let peer = PeerList.GetPeer(index);
    peer.dataChannelReliable = peer.peerConnection.createDataChannel("sendChannelReliable");

    peer.dataChannelReliable.onopen = (event) => {
        if (peer.onDataChannelReliableOpenCallback)
            Module.dynCall_vi(peer.onDataChannelReliableOpenCallback, peer.index);
    };

    peer.dataChannelReliable.onmessage = (event) => {
        if (event.data instanceof ArrayBuffer) {
            let array = new Uint8Array(event.data);
            let arrayLength = array.length;

            var ptr = Module._malloc(arrayLength);

            let dataBuffer = new Uint8Array(HEAPU8.buffer, ptr, arrayLength);

            Module.HEAPU8.set(dataBuffer, ptr);

            dataBuffer.set(array);

            if (peer.onMessageCallback)
                Module.dynCall_viii(peer.onMessageCallback, peer.index, ptr, dataBuffer.length);
        }
    };
};



function WebRTC_Unsafe_GetOffer(index) {
    let peer = PeerList.GetPeer(index);
    const pointer = _malloc(peer.offerJson.length + 1); // +1 for null terminator
    stringToUTF8(peer.offerJson, pointer, peer.offerJson.length + 1);
    return pointer;
};

function WebRTC_Unsafe_GetAnswer(index) {
    let peer = PeerList.GetPeer(index);
    const pointer = _malloc(peer.answerJson.length + 1); // +1 for null terminator
    stringToUTF8(peer.answerJson, pointer, peer.answerJson.length + 1);
    return pointer;
};

async function WebRTC_Unsafe_SetLocalDescription (index, sdpJson) {

    let peer = PeerList.GetPeer(index);
    const json = UTF8ToString(sdpJson);

    const sdp = JSON.parse(json);

    peer.opSetLocalDescription = peer.peerConnection.setLocalDescription(sdp);

    await peer.opSetLocalDescription;

    peer.opSetLocalDescriptionDone = true;
};

async function WebRTC_Unsafe_SetRemoteDescription(index, sdpJson) {

    let peer = PeerList.GetPeer(index);
    const json = UTF8ToString(sdpJson);

    const sdp = JSON.parse(json);

    peer.opSetRemoteDescription = peer.peerConnection.setRemoteDescription(sdp);

    await peer.opSetRemoteDescription;

    peer.opSetRemoteDescriptionDone = true;
};

function WebRTC_HasOpSetLocalDescription(index) {
    let peer = PeerList.GetPeer(index);
    return peer.opSetLocalDescription != null;
};

function WebRTC_IsOpSetLocalDescriptionDone(index) {
    let peer = PeerList.GetPeer(index);
    return peer.opSetLocalDescriptionDone;
};

function WebRTC_DisposeOpSetLocalDescription(index) {
    let peer = PeerList.GetPeer(index);
    peer.opSetLocalDescription = null;
    peer.opSetLocalDescriptionDone = false;
};

function WebRTC_HasOpSetRemoteDescription(index) {
    let peer = PeerList.GetPeer(index);
    return peer.opSetRemoteDescription != null;
};

function WebRTC_IsOpSetRemoteDescriptionDone(index) {
    let peer = PeerList.GetPeer(index);
    return peer.opSetRemoteDescriptionDone;
};

function WebRTC_DisposeOpSetRemoteDescription(index) {
    let peer = PeerList.GetPeer(index);
    peer.opSetRemoteDescription = null;
    peer.opSetRemoteDescriptionDone = false;
};

function WebRTC_Unsafe_GetLocalDescription(index) {

    let peer = PeerList.GetPeer(index);
    let localDescription = peer.peerConnection.localDescription;
    let localDescriptionJson = JSON.stringify(localDescription);

    const pointer = _malloc(localDescriptionJson.length + 1); // +1 for null terminator
    stringToUTF8(localDescriptionJson, pointer, localDescriptionJson.length + 1);
    return pointer;
};

function WebRTC_Unsafe_GetRemoteDescription(index) {
    let peer = PeerList.GetPeer(index);
    let remoteDescription = peer.peerConnection.remoteDescription;
    let remoteDescriptionJson = JSON.stringify(remoteDescription);

    const pointer = _malloc(remoteDescriptionJson.length + 1); // +1 for null terminator
    stringToUTF8(remoteDescriptionJson, pointer, remoteDescriptionJson.length + 1);
    return pointer;
};

function WebRTC_SetCallbackOnMessage(index, callback) {
    let peer = PeerList.GetPeer(index);
    peer.onMessageCallback = callback;
};

function WebRTC_SetCallbackOnIceConnectionStateChange(index, callback) {
    let peer = PeerList.GetPeer(index);
    peer.onIceConnectionStateChangeCallback = callback;
};

function WebRTC_SetCallbackOnDataChannelCreated(index, callback) {
    let peer = PeerList.GetPeer(index);
    peer.onDataChannelCreatedCallback = callback;
};

function WebRTC_SetCallbackOnDataReliableChannelCreated(index, callback) {
    let peer = PeerList.GetPeer(index);
    peer.onDataChannelReliableCreatedCallback = callback;
};

function WebRTC_SetCallbackOnIceCandidate(index, callback) {
    let peer = PeerList.GetPeer(index);
    peer.onIceCandidateCallback = callback;
};

function WebRTC_SetCallbackOnIceCandidateGatheringState(index, callback) {
    let peer = PeerList.GetPeer(index);
    peer.onIceCandidateGathertingStateCallback = callback;
};

function WebRTC_SetCallbackOnDataChannelOpen(index, callback) {
    let peer = PeerList.GetPeer(index);
    peer.onDataChannelOpenCallback = callback;
};
function WebRTC_SetCallbackOnDataChannelReliableOpen(index, callback) {
    let peer = PeerList.GetPeer(index);
    peer.onDataChannelReliableOpenCallback = callback;
};

function WebRTC_CloseConnection(index) {
    let peer = PeerList.GetPeer(index);
    if (peer.dataChannel)
        peer.dataChannel.close();

    if (peer.dataChannelReliable)
        peer.dataChannelReliable.close();

    if (peer.peerConnection)
        peer.peerConnection.close();
};

function WebRTC_GetIsPeerConnectionCreated(index) {
    let peer = PeerList.GetPeer(index);
    return peer.peerConnection != null;
};

function WebRTC_Reset(index) {
    let peer = PeerList.GetPeer(index);
    peer.peerConnection = null;
    peer.dataChannel = null;

    peer.offerJson = null;
    peer.answerJson = null;

    peer.opCreateOffer = null;
    peer.opCreateOfferDone = null;

    peer.opCreateAnswer = null;
    peer.opCreateAnswerDone = null;

    peer.opSetLocalDescription = null;
    peer.opSetLocalDescriptionDone = null;

    peer.opSetRemoteDescription = null;
    peer.opSetRemoteDescriptionDone = null;

    peer.onMessageCallback = null;
    peer.onIceConnectionStateChangeCallback = null;
    peer.onIceCandidateCallback = null;
    peer.onIceCandidateGathertingStateCallback = null;

    peer.onDataChannelCreatedCallback = null;
    peer.onDataChannelOpenCallback = null;

    peer.onDataChannelReliableCreatedCallback = null;
    peer.onDataChannelReliableOpenCallback = null;
};

function WebRTC_Unsafe_GetGatheringState(index) {

    let peer = PeerList.GetPeer(index);
    if (peer.peerConnection.iceGatheringState == null)
        return 0;

    let stateNumber = 0;

    switch (peer.peerConnection.iceGatheringState) {
        case "new":
            stateNumber = 0;
            break;
        case "gathering":
            stateNumber = 1;
            break;
        case "complete":
            stateNumber = 2;
            break;
    }

    return stateNumber;
};

const WebRTCLib = {
    $PeerList: PeerList,
    WebRTC_Unsafe_CreateRTCPeerConnection,
    WebRTC_Unsafe_GetConnectionState,
    WebRTC_IsConnectionOpen,
    WebRTC_DataChannelSend,
    WebRTC_DataChannelReliableSend,

    WebRTC_GetOpCreateOfferIsDone,
    WebRTC_GetOpCreateAnswerIsDone,
    WebRTC_DisposeOpCreateOffer,
    WebRTC_DisposeOpCreateAnswer,
    WebRTC_HasOpCreateOffer,
    WebRTC_HasOpCreateAnswer,
    WebRTC_CreateOffer,
    WebRTC_CreateAnswer,

    WebRTC_Unsafe_CreateDataChannel,
    WebRTC_Unsafe_CreateDataChannelReliable,

    WebRTC_Unsafe_GetOffer,
    WebRTC_Unsafe_GetAnswer,

    WebRTC_Unsafe_SetLocalDescription,
    WebRTC_Unsafe_SetRemoteDescription,
    WebRTC_HasOpSetLocalDescription,
    WebRTC_IsOpSetLocalDescriptionDone,
    WebRTC_DisposeOpSetLocalDescription,
    WebRTC_HasOpSetRemoteDescription,
    WebRTC_IsOpSetRemoteDescriptionDone,
    WebRTC_DisposeOpSetRemoteDescription,

    WebRTC_Unsafe_GetLocalDescription,
    WebRTC_Unsafe_GetRemoteDescription,

    WebRTC_SetCallbackOnMessage,
    WebRTC_SetCallbackOnIceConnectionStateChange,
    WebRTC_SetCallbackOnDataChannelCreated,
    WebRTC_SetCallbackOnDataReliableChannelCreated,
    WebRTC_SetCallbackOnIceCandidate,
    WebRTC_SetCallbackOnIceCandidateGatheringState,
    WebRTC_SetCallbackOnDataChannelOpen,
    WebRTC_SetCallbackOnDataChannelReliableOpen,

    WebRTC_CloseConnection,
    WebRTC_GetIsPeerConnectionCreated,
    WebRTC_Reset,
    WebRTC_Unsafe_GetGatheringState,
};

autoAddDeps(WebRTCLib, "$PeerList");
mergeInto(LibraryManager.library, WebRTCLib);