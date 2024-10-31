var connection = new signalR.HubConnectionBuilder().withUrl("/scoreHub").build();

connection.start().then(function () {

}).catch(function (err) {
    return console.error(err.toString());
});


connection.on("BidResultSubmittedAsync", function () {
    let goToScoreAnchor = document.getElementById("goToScore");
    if (goToScoreAnchor) {
        let url = goToScoreAnchor.href;
        if (url) {
            window.location.replace(url);
        }
    }
});

connection.on("NextRoundStartedAsync", function () {
    let gotToBidAnchor = document.getElementById("goToBid");
    if (gotToBidAnchor) {
        let url = gotToBidAnchor.href;
        if (url) {
            window.location.replace(url);
        }
    }
});