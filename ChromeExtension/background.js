
// connect to local program com.translator.service
port = chrome.runtime.connectNative('com.translator.service');

port.onMessage.addListener(function(msg) {
  console.log("Translation received - index: " + msg['index'] + ", text: " + msg['text']);

  // Send received translation to tab
  chrome.tabs.sendMessage(msg['tab'], msg);
});

port.onDisconnect.addListener(function() {
  console.log("Disconnected");
});

chrome.runtime.onMessage.addListener(function(msg) {
  console.log("Message received from content.js");
  console.log(msg);

  // Send the message to host
  port.postMessage(msg);
})

//console.log("Sending message to host");
//port.postMessage({ text: "Hello, my_application" });
