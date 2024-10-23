"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/waitingRoomHub").build();

connection.on("RefreshPlayerListAsync", function () {
    window.location.reload();
});

connection.on("GameStartedAsync", function () {
    let gotToBidAnchor = document.getElementById("goToBid");
    if (gotToBidAnchor) {
        let url = gotToBidAnchor.href;
        if (url) {
            window.location.replace(url);
        }
    }
});

connection.start().then(function () {
    //nothing to do
}).catch(function (err) {
    return console.error(err.toString());
});
