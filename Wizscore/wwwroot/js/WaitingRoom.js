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
                    copyBtn.className = "btn btn-lg btn-success";
                    copyBtn.setAttribute("diabled", "");

                    setTimeout(() => {
                        copyBtn.className = "btn btn-lg btn-outline-info";
                        copyBtn.removeAttribute("diabled");
                    }, 1000);
                }, function (err) {
                    console.error('Async: Could not copy text: ', err);
                });
            }
        });
    }

    const shareButton = document.getElementById("share");
    const shareUrl = shareButton.dataset.shareUrl;

    if (navigator.share) {

        shareButton.addEventListener("click", (e) => {
            navigator.share({
                title: 'Wizscore',
                text: 'Join my wizscore game!',
                url: shareUrl,
            }).then(() => console.log('Successful share'))
              .catch((error) => console.log('Error sharing', error));

        });
    }
});

