var connection = new signalR.HubConnectionBuilder().withUrl("/bidHub").build();

connection.start().then(function () {

    let suitButtons = document.getElementsByClassName('suit');
    let isDealer = document.getElementById('isDealer');

    if (suitButtons && isDealer) {
        for (let i = 0; i < suitButtons.length; i++) {
            let suitButton = suitButtons[i];
            suitButton.addEventListener('click', () => {
                let suit = suitButton.dataset.suit;
                if (suit) {
                    connection.invoke("suitChanged", suit)
                }
            });
        }
    }


}).catch(function (err) {
    return console.error(err.toString());
});


connection.on("SuitChangedAsync", function (suit) {

    let previouslySelectedSuitButton = document.querySelector('.suit[data-active="true"]');
    if (previouslySelectedSuitButton) {
        previouslySelectedSuitButton.className = 'btn btn-lg btn-outline-info'
        previouslySelectedSuitButton.dataset.active = 'false';
    }

    let suitButton = document.querySelector(`button[data-suit="${suit}"]`)
    if (suitButton) {
        suitButton.className = 'btn btn-lg btn-success suit';
        suitButton.dataset.active = 'true';
    }

});