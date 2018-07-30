/*
 * echotest.js
 *
 * Derived from Echo Test of WebSocket.org (http://www.websocket.org/echo.html).
 *
 * Copyright (c) 2012 Kaazing Corporation.
 */

 var url = "ws://ap2_OEls@localhost:3005/rconchat";
//var url = "wss://localhost:5963/Echo";
var output;
var inputField;

function init () {
 output = document.getElementById ("output");
 inputField = document.getElementById("chatinput");
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
  send ("chatfull");
}

function onMessage (event) {
  writeToScreen ('<span style="color: blue; overflow-wrap: break-word;">' + event.data + '</span>');
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

function sendInput(){
  websocket.send("1" + inputField.value);
  inputField.value = "";
}

function writeToScreen (message) {
  var pre = document.createElement ("span");
  pre.style.wordWrap = "break-word";
  pre.innerHTML = message;
  output.appendChild (pre);
  output.scrollTop = output.scrollHeight;
}

window.addEventListener ("load", init, false);