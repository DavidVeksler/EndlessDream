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

window.initializeChat = () => {
    window.addEventListener('resize', adjustChatHeight);
    adjustChatHeight();
}

window.adjustChatHeight = () => {
    const chat = document.getElementById('chatMessages');
    if (chat) {
        const windowHeight = window.innerHeight;
        const chatTop = chat.getBoundingClientRect().top;
        const footerHeight = document.querySelector('.card-footer').offsetHeight;
        chat.style.height = `${windowHeight - chatTop - footerHeight - 20}px`; // 20px for padding
    }
}

window.scrollToBottom = (element) => {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}