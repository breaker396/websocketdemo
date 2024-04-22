'use strict';

var video = document.getElementById('video');
var server = document.getElementById("server");
var btnConnect = document.getElementById("btnConnect");
var btnOpenCamera = document.getElementById("btnOpenCamera");
var btnClose = document.getElementById("btnClose");
var lblState = document.getElementById("lblState");
var img = document.getElementById('img');

var socket;
var scheme = document.location.protocol === "https:" ? "wss" : "ws";
var port = document.location.port ? (":" + document.location.port) : "";

var hubUrl = document.location.origin + '/cnnctn';
var hubConnection = new signalR.HubConnectionBuilder()
    .withUrl(hubUrl, signalR.HttpTransportType.WebSockets)
    .configureLogging(signalR.LogLevel.None)
    .build();

var subject = new signalR.Subject();
var acceptinguser = "";
var users = [];
var user = null;
var caller = null;

$(document).ready(function () {
    server.value = scheme + "://" + document.location.hostname + port + "/ws";
});




btnOpenCamera.onclick = function () {
    var state = btnOpenCamera.getAttribute('data-state')
    if (state === 'opened') {
        var stream = video.srcObject;
        var tracks = stream.getTracks();

        for (var i = 0; i < tracks.length; i++) {
            var track = tracks[i];
            track.stop();
        }
        video.srcObject = null;
        btnOpenCamera.setAttribute('data-state', 'closed');
        document.getElementById('imgHub').src = '';
        btnOpenCamera.classList.add('btn-info');
        btnOpenCamera.classList.remove('btn-danger');
        btnOpenCamera.innerHTML = "Open Camera";
    } else {

        if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
            //$("#fromClient").width()
            navigator.mediaDevices.getUserMedia({ video: { width: 1920, height: 1080 }, frameRate: { ideal: 10, max: 30 }, audio: false }).then(function (stream) {
                video.srcObject = stream;
                video.play();
            });
        }
        btnOpenCamera.setAttribute('data-state', 'opened');
        btnOpenCamera.classList.add('btn-danger');
        btnOpenCamera.classList.remove('btn-info');
        btnOpenCamera.innerHTML = "Close Camera";
    }
};

btnClose.onclick = function () {
    if (!socket || socket.readyState !== WebSocket.OPEN) {
        alert("WebSocket not connected.");
    }
    socket.close(1000, "Closing...");
};

btnConnect.onclick = function () {
    lblState.innerHTML = "Connecting...";
    socket = new WebSocket(server.value);
    socket.onopen = function (event) {
        updateState();
        lblState.innerHTML = `Connected to ${server.value}`;
    };
    socket.onclose = function (event) {
        updateState();
    };
    socket.onerror = updateState;
    socket.onmessage = message => {
        console.info(`Receive ${Date.now()}`);
        document.getElementById('img').src = '';
        var image = new Image();
        //image.src = `data:image/jpeg;base64,${message.data}`;
        image.src = message.data;
        document.getElementById('img').src = image.src;
    }
};
btnStart.onclick = function () {
    setInterval(() => {
        if (isOpen(socket)) {
            var data = getVideoFrame();
            console.info(`Send ${Date.now()}`);
            socket.send(data);
        }
    }, 1000 / 20);
};

const updateState = () => {
    function disable() {
        btnClose.disabled = true;
    }
    function enable() {
        btnClose.disabled = false;
    }

    server.disabled = true;
    btnConnect.disabled = true;

    if (!socket) {
        disable();
    } else {
        switch (socket.readyState) {
            case WebSocket.CLOSED:
                lblState.innerHTML = "Closed";
                disable();
                server.disabled = false;
                btnConnect.disabled = false;
                img.src = '';
                break;
            case WebSocket.CLOSING:
                lblState.innerHTML = "Closing...";
                disable();
                break;
            case WebSocket.CONNECTING:
                lblState.innerHTML = "Connecting...";
                disable();
                break;
            case WebSocket.OPEN:
                lblState.innerHTML = "Open";
                enable();
                break;
            default:
                lblState.innerHTML = "Unknown state: " + htmlEscape(socket.readyState);
                disable();
                break;
        }
    }
}

const getVideoFrame = () => {
    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    canvas.getContext('2d').drawImage(video, 0, 0);
    const data = canvas.toDataURL('image/jpeg', 1);
    return data;
}

const isOpen = (ws) => {
    return ws.readyState === ws.OPEN;
}

const htmlEscape = (str) => {
    return str.toString()
        .replace(/&/g, '&amp;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');
}