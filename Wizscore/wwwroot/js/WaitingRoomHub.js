"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/waitingRoomHub").build();

connection.on("PlayerAddedAsync", function () {

    //let list = document.getElementById("waitingRoomList");
    //if (list) {
    //    for (var i = 0, row; row = list.rows[i]; i++) {
    //        let isNotPlayerRow = row.dataset.isPlayerRow === "false"
    //        if (isNotPlayerRow) {
    //            row.cells[1].innerHTML = username;
    //            row.dataset.isPlayerRow = "false";
    //            return;
    //        }
    //    }
    //}

    window.location.reload();
});

connection.on("PlayerRemovedAsync", function () {
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
