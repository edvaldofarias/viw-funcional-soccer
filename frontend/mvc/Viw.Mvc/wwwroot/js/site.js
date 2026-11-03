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

function showMissingPhoto(image) {
  image.referrerPolicy = 'no-referrer';
  image.src = image.dataset.mediaPlaceholder;
  image.alt = 'Foto não disponível.';
  image.removeAttribute('data-media-placeholder');
}
document.addEventListener('error', event => {
  const image = event.target;
  if (image instanceof HTMLImageElement && image.hasAttribute('data-media-placeholder')) {
    showMissingPhoto(image);
  }
}, true);
document.querySelectorAll('img[data-media-placeholder]').forEach(image => {
  if (image.complete && image.naturalWidth === 0) {
    showMissingPhoto(image);
  }
});

const playerDialog = document.getElementById("edition-player-dialog");
if (playerDialog) {
    const content = playerDialog.querySelector("[data-player-dialog-content]");
    const status = playerDialog.querySelector("[data-player-dialog-status]");
    const retry = playerDialog.querySelector("[data-player-dialog-retry]");
    const closeButton = playerDialog.querySelector("[data-player-dialog-close]");
    let request;
    let trigger;

    async function loadPlayer() {
        request?.abort();
        const current = new AbortController();
        request = current;
        content.replaceChildren();
        content.setAttribute("aria-busy", "true");
        status.hidden = false;
        status.textContent = "Carregando os detalhes...";
        retry.hidden = true;
        try {
            const response = await fetch(trigger.dataset.playerDetailsUrl, { signal: current.signal });
            if (!response.ok) {
                throw new Error(response.status === 404
                    ? "Os detalhes deste jogador não estão disponíveis neste campeonato."
                    : "Não foi possível carregar os detalhes. Tente novamente.");
            }
            if (!response.headers.get("content-type")?.includes("text/html")) {
                throw new Error("Os detalhes recebidos são inválidos. Tente novamente.");
            }
            const html = await response.text();
            if (current.signal.aborted) return;
            content.innerHTML = html;
            status.hidden = true;
        } catch (error) {
            if (current.signal.aborted) return;
            status.textContent = error instanceof TypeError
                ? "Não foi possível conectar ao servidor. Tente novamente."
                : error.message;
            retry.hidden = false;
        } finally {
            if (!current.signal.aborted) content.setAttribute("aria-busy", "false");
        }
    }

    document.querySelectorAll("[data-player-details-url]").forEach(button => {
        button.addEventListener("click", () => {
            trigger = button;
            playerDialog.showModal();
            loadPlayer();
        });
    });
    retry.addEventListener("click", () => {
        closeButton.focus();
        loadPlayer();
    });
    closeButton.addEventListener("click", () => playerDialog.close());
    playerDialog.addEventListener("keydown", event => {
        if (event.key === "Escape") {
            event.preventDefault();
            playerDialog.close();
        }
    });
    playerDialog.addEventListener("click", event => {
        if (event.target !== playerDialog) return;
        const bounds = playerDialog.getBoundingClientRect();
        if (event.clientX < bounds.left || event.clientX > bounds.right ||
            event.clientY < bounds.top || event.clientY > bounds.bottom) playerDialog.close();
    });
    playerDialog.addEventListener("close", () => {
        request?.abort();
        content.replaceChildren();
        trigger?.focus();
    });
}
