addEventListener("DOMContentLoaded", (event) => {
    let copyBtn = document.getElementById('copyGameKeyToKeyboard');
    if (copyBtn) {

        if (!navigator.clipboard) {
            return;
        }

        copyBtn.addEventListener('click', () => {
            let gameKey = copyBtn.dataset.gameKey;
            if (gameKey) {
                navigator.clipboard.writeText(gameKey).then(function () {
                    copyBtn.className = "btn btn-success";
                    copyBtn.innerHTML = "copied";
                    copyBtn.setAttribute("diabled", "");

                    setTimeout(() => {
                        copyBtn.className = "btn btn-outline-info";
                        copyBtn.innerHTML = "Copy to keyboard";
                        copyBtn.removeAttribute("diabled");
                    }, 1000);
                }, function (err) {
                    console.error('Async: Could not copy text: ', err);
                });
            }
        });
    }
});

