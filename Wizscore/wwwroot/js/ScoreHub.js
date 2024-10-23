var connection = new signalR.HubConnectionBuilder().withUrl("/scoreHub").build();

connection.start().then(function () {

}).catch(function (err) {
    return console.error(err.toString());
});


connection.on("BidResultSubmittedAsync", function (suit) {
    window.location.reload();
});