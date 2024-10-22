"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/waitingRoomHub").build();

connection.on("PlayerAddedAsync", function (username) {

    console.log(username);
    let list = document.getElementById("waitingRoomList");
    if (list) {
        for (var i = 0, row; row = list.rows[i]; i++) {
            let isNotPlayerRow = row.dataset.isPlayerRow === "false"
            if (isNotPlayerRow) {
                row.cells[1].innerHTML = username;
                row.dataset.isPlayerRow = "false";
                return;
            }
        }
    }
});

connection.start().then(function () {
    //nothing to do
}).catch(function (err) {
    return console.error(err.toString());
});
