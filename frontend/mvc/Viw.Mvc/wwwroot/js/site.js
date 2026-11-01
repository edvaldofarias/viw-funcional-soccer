"use strict";
document.querySelectorAll("[data-share-url]").forEach(button => {
    button.addEventListener("click", async () => {
        const status = button.parentElement.querySelector("[role=status]");
        try {
            if (navigator.share) {
                await navigator.share({ title: button.dataset.shareTitle, url: button.dataset.shareUrl });
                status.textContent = "";
            } else if (navigator.clipboard && window.isSecureContext) {
                await navigator.clipboard.writeText(button.dataset.shareUrl);
                status.textContent = "Link copiado!";
            } else {
                status.textContent = "Para compartilhar, copie o endereço da página.";
            }
        } catch (error) {
            status.textContent = error.name === "AbortError" ? "" : "Não foi possível compartilhar. Copie o endereço da página.";
        }
    });
});
