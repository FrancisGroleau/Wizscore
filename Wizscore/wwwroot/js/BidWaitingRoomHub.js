"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/bidWaitingRoomHub").build();
var bidHubConnection = new signalR.HubConnectionBuilder().withUrl("/bidHub").build();

connection.start().then(function () {
    //nothing to do
}).catch(function (err) {
    return console.error(err.toString());
});

bidHubConnection.start().then(function () {
    //nothing to do
}).catch(function (err) {
    return console.error(err.toString());
});


connection.on("BidSubmittedAsync", function () {
    let gotToBidAnchor = document.getElementById("goToBid");
    if (gotToBidAnchor) {
        let url = gotToBidAnchor.href;
        if (url) {
            window.location.replace(url);
        }
    }
});

bidHubConnection.on("SuitChangedAsync", function (suit) {

    let previouslySelectedSuitButton = document.querySelector('.suit[data-active="true"]');
    if (previouslySelectedSuitButton) {
        previouslySelectedSuitButton.className = 'btn btn-outline-info'
        previouslySelectedSuitButton.dataset.active = 'false';
    }

    let suitButton = document.querySelector(`button[data-suit="${suit}"]`)
    if (suitButton) {
        suitButton.className = 'btn btn-success suit';
        suitButton.dataset.active = 'true';
    }
});
