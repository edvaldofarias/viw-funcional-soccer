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

const cellText = cell => {
    const text = cell.cloneNode(true);
    text.querySelectorAll(".avatar, .team-avatar, [aria-hidden=true], .sr-only, .visually-hidden").forEach(node => node.remove());
    return text.textContent.trim().replace(/\s+/g, " ");
};

// Grids são cartões por padrão; viram tabela apenas quando cabem sem rolagem horizontal.
function fitGrid(grid) {
    const box = grid.table.parentElement;
    grid.wrap.classList.add("is-table");
    grid.width = box.clientWidth;
    grid.wrap.classList.toggle("is-table", grid.width > 0 && grid.table.getBoundingClientRect().width <= grid.width + 0.5);
}

function describeGrid(table) {
    const headers = Array.from(table.querySelectorAll("thead th"));
    const rows = Array.from(table.querySelectorAll("tbody tr"));
    const labels = headers.map(header => header.querySelector("abbr[title]")?.title || cellText(header));
    const cardLabels = headers.map((header, index) => header.dataset.cardLabel || labels[index]);
    const rank = headers.findIndex(header => /^pos/i.test(cellText(header)));
    const title = headers.findIndex((header, index) => index !== rank && !header.classList.contains("numeric"));
    const compact = headers.map((header, index) => header.classList.contains("numeric") ||
        rows.every(row => !row.cells[index] || cellText(row.cells[index]).length <= 6));
    table.querySelectorAll("tbody tr, tfoot tr").forEach(row => {
        let index = 0;
        Array.from(row.cells).forEach(cell => {
            const column = index;
            index += cell.colSpan;
            if (cell.colSpan > 1 || column >= headers.length) return;
            if (column === rank) cell.classList.add("cell-rank");
            else if (column === title) cell.classList.add("cell-title");
            else {
                cell.dataset.label = cardLabels[column];
                if (compact[column]) cell.classList.add("cell-stat");
            }
        });
    });
    return headers.map((header, index) => ({ index, label: labels[index], numeric: compact[index] && index !== title && index !== rank }));
}

function addCardSort(table, api, columns, id) {
    const sort = document.createElement("div");
    sort.className = "table-sort";
    const label = document.createElement("label");
    label.htmlFor = id;
    label.textContent = "Ordenar por";
    const select = document.createElement("select");
    select.id = id;
    select.add(new Option("Ordem inicial", ""));
    columns.forEach(column => select.add(new Option(column.label, String(column.index))));
    const direction = document.createElement("button");
    direction.type = "button";
    direction.className = "table-sort-direction";
    const sync = () => {
        const current = api.order()[0];
        const descending = current?.[1] === "desc";
        select.value = current ? String(current[0]) : "";
        direction.hidden = !current;
        direction.textContent = descending ? "↓ Maior primeiro" : "↑ Menor primeiro";
        direction.setAttribute("aria-label", descending ? "Ordem decrescente; ativar para crescente" : "Ordem crescente; ativar para decrescente");
    };
    select.addEventListener("change", () => {
        const column = columns.find(item => String(item.index) === select.value);
        api.order(column ? [[column.index, column.numeric ? "desc" : "asc"]] : []).draw();
    });
    direction.addEventListener("click", () => {
        const current = api.order()[0];
        if (current) api.order([[current[0], current[1] === "desc" ? "asc" : "desc"]]).draw();
    });
    api.on("order", sync);
    sync();
    sort.append(label, select, direction);
    table.closest(".dt-layout-table").before(sort);
}

const grids = [];
document.querySelectorAll("main table").forEach((table, position) => {
    const grid = { table, wrap: table.closest(".table-wrap, .table-responsive") ?? table.parentElement, width: -1 };
    grids.push(grid);
    try {
        if (typeof DataTable === "undefined" || !globalThis.viwTableOptions) {
            throw new Error("Os arquivos locais do DataTables não foram carregados.");
        }
        DataTable.ext.errMode = "throw";
        table.querySelectorAll("tbody tr").forEach(row => {
            Array.from(row.cells).forEach(cell => {
                const value = cellText(cell);
                if (!cell.hasAttribute("data-order")) {
                    cell.dataset.order = /^[+-]?\d+(?:[.,]\d+)?$/.test(value) ? value.replace(",", ".") : value === "—" ? "" : value;
                }
                if (!cell.hasAttribute("data-search")) cell.dataset.search = value;
            });
        });
        const columns = describeGrid(table);
        const api = new DataTable(table, {
            ...globalThis.viwTableOptions,
            displayStart: (Number(table.dataset.tablePage || 1) - 1) * globalThis.viwTableOptions.pageLength
        });
        addCardSort(table, api, columns, "ordenar-tabela-" + (position + 1));
        const search = grid.wrap.querySelector(".dt-search input");
        if (search) search.placeholder = "Buscar na tabela";
        api.on("draw", () => fitGrid(grid));
    } catch (error) {
        console.error("Falha ao ativar a tabela.", error);
        const notice = document.createElement("p");
        notice.className = "notice";
        notice.setAttribute("role", "alert");
        notice.textContent = "Não foi possível ativar a ordenação, busca e paginação desta tabela. Recarregue a página.";
        grid.wrap.before(notice);
    }
    fitGrid(grid);
});
if (grids.length > 0) {
    const refit = () => grids.forEach(fitGrid);
    // A troca de formato altera só a altura; reagir apenas à largura evita medições em laço.
    const observer = new ResizeObserver(entries => {
        const changed = grids.filter(grid => entries.some(entry =>
            entry.target === grid.table.parentElement && Math.abs(entry.contentRect.width - grid.width) >= 1));
        if (changed.length > 0) requestAnimationFrame(() => changed.forEach(fitGrid));
    });
    grids.forEach(grid => observer.observe(grid.table.parentElement));
    document.fonts?.ready.then(refit);
    window.addEventListener("load", refit);
}
