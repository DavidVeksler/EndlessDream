window.scrollToBottom = (element) => {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}

function openModal(modalId) {
    var modal = new bootstrap.Modal(document.getElementById(modalId));
    modal.show();
}

function closeModal(modalId) {
    var modal = bootstrap.Modal.getInstance(document.getElementById(modalId));
    if (modal) {
        modal.hide();
    }
}

async function postMessage(data) {
    window.parent.postMessage(data, "*");
    return;
}

async function isEmbedded() {
    return window.parent !== window;  //JavaScript helper to detect the context (embedded or standalone)
};