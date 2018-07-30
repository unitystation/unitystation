/*
 * echotest.js
 *
 * Derived from Echo Test of WebSocket.org (http://www.websocket.org/echo.html).
 *
 * Copyright (c) 2012 Kaazing Corporation.
 */

var url = "ws://ap2_OEls@localhost:3005/rconmonitor";
//var url = "wss://localhost:5963/Echo";
var output;

function init () {
	output = document.getElementById ("output");
	doWebSocket ();
}

function doWebSocket () {
	websocket = new WebSocket (url);
	websocket.maxIdleTime = -1;
	websocket.onopen = function (e) {
		onOpen (e);
	};

	websocket.onmessage = function (e) {
		onMessage (e);
	};

	websocket.onerror = function (e) {
		onError (e);
	};

	websocket.onclose = function (e) {
		onClose (e);
	};
}

function onOpen (event) {
	send ("login");
}

function onMessage (event) {
	while (output.firstChild) {
		output.removeChild(output.firstChild);
	}
	writeToScreen ('<span style="color: blue;">' + event.data + '</span>');
}

function onError (event) {
	writeToScreen ('<span style="color: red;">ERROR: ' + event.data + '</span>');
}

function onClose (event) {
	writeToScreen ("DISCONNECTED");
}

function send (message) {
	websocket.send (message);
}

function writeToScreen (message) {
	var pre = document.createElement ("p");
	pre.style.wordWrap = "break-word";
	pre.innerHTML = message;
	output.appendChild (pre);
}

window.addEventListener ("load", init, false);