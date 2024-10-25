addEventListener("DOMContentLoaded", (event) => {

    var plusBtn = document.getElementById('plus');
    var minusBtn = document.getElementById('minus');

    var bid = document.getElementById('bid');
    var bidDisplay = document.getElementById('bidDisplay');
    let bidValue = parseInt(bidDisplay.textContent);

    if (bidValue == 0) {
        minusBtn.setAttribute("disabled", "true");
    }


    var round = 0;
    var roundBtn = document.getElementById('round');
    if (roundBtn) {
        round = parseInt(roundBtn.dataset.round);
    }

    plusBtn.addEventListener('click', () => {

        bidValue += 1;
        bidDisplay.textContent = bidValue;
        bid.value = bidValue;

        if (bidValue == round) {
            plusBtn.setAttribute("disabled", "true");
        }

        if (bidValue > 0) {
            minusBtn.removeAttribute("disabled");
        }
    });




    minusBtn.addEventListener('click', () => {

        if (bidValue > 0) {
            bidValue -= 1;
            bidDisplay.textContent = bidValue;
            bid.value = bidValue;
        }

        if (bidValue < round) {
            plusBtn.removeAttribute("disabled");
        }

        if (bidValue == 0) {
            minusBtn.setAttribute("disabled", "true");
        }

    });
});