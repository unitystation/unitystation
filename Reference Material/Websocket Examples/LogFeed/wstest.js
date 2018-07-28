/*
 * echotest.js
 *
 * Derived from Echo Test of WebSocket.org (http://www.websocket.org/echo.html).
 *
 * Copyright (c) 2012 Kaazing Corporation.
 */

 var url = "ws://localhost:3005/checkConn";
//var url = "wss://localhost:5963/Echo";
var output;
var t;
var timer_is_on=0;
var lastLog;

function init () {
 doTimer();
}

function timedCount()
{
  output = document.getElementById ("output");
  doWebSocket ();
}

function doTimer()
{
  if (!timer_is_on)
  {
    timer_is_on=1;
    timedCount();
  }
}

function stopCount()
{
  clearTimeout(t);
  timer_is_on=0;
}

function doWebSocket () {
  websocket = new WebSocket (url);

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
  send ("log");
}

function onMessage (event) {
  if(lastLog != event.data){
    lastLog = event.data;
    writeToScreen ('<span style="color: blue;">' + event.data + '</span>');
  }
  t=setTimeout("timedCount()",4000);
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
  output.scrollTop = output.scrollHeight;
}

window.addEventListener ("load", init, false);