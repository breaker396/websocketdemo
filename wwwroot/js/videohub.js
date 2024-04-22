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

    hubConnection.start().then(() => {
        user = generateId();
        console.info(`Connected as ${user}`);

    }).catch(err => console.error(err));

    hubConnection.on('receiveData', (signalingUser, data) => {
        document.getElementById('imgHub').src = '';
        if (data.toString().startsWith('data:,') == false) {
            var image = new Image();
            image.src = `${data}`;
            document.getElementById('imgHub').src = image.src;
        }
    });
});



const sendSignal = (candidate, partnerClientId) => {
    hubConnection.invoke('sendData', candidate, partnerClientId).catch(err => console.error(err));
};


const dataStream = (acceptingUser) => {
    subject = new signalR.Subject();
    if (hubConnection.state === 'Connected'){
        hubConnection.send("UploadStream", subject, `${(acceptingUser) ? acceptingUser.connectionId : ''}`);
    }
};

const intervalHandle = setInterval(() => {
    var state = btnOpenCamera.getAttribute('data-state');
    if (state === 'opened' && hubConnection.state === 'Connected') {
        subject.next(`${(acceptinguser) ? acceptinguser.connectionId : ''}|${getVideoFrame()}`);
        hubConnection.stream("DownloadStream", 500)
            .subscribe({
                next: (item) => {
                    console.info(item);
                },
                complete: () => {

                },
                error: (err) => {
                    console.info(err);
                },
            });
    } else {
        //subject.complete();
    }
}, 500);



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
        subject.complete();
    } else {

        if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
            navigator.mediaDevices.getUserMedia({ video: { width: 480, height: 360 }, frameRate: { ideal: 10, max: 30 }, audio: false }).then(function (stream) {
                video.srcObject = stream;
                video.play();
                btnOpenCamera.setAttribute('data-state', 'opened');
                btnOpenCamera.classList.add('btn-danger');
                btnOpenCamera.classList.remove('btn-info');
                btnOpenCamera.innerHTML = "Close Camera";
                dataStream("abc");
            });
        }
    }
};


const getVideoFrame = () => {
    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    canvas.getContext('2d').drawImage(video, 0, 0);
    const data = canvas.toDataURL('image/jpeg', 1);
    return data;
}