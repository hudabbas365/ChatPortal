// ChatPortal - Chat JavaScript

let sessionId = null;

function appendMessage(role, content) {
    const isAi = role.toLowerCase() === 'assistant';
    const emptyState = document.getElementById('emptyState');
    if (emptyState) emptyState.style.display = 'none';

    const messageHtml = `
        <div class="d-flex ${isAi ? '' : 'justify-content-end'} mb-3 message-item">
            ${isAi ? `
            <div class="me-2 flex-shrink-0">
                <div class="rounded-circle bg-primary d-flex align-items-center justify-content-center" style="width:36px;height:36px;">
                    <i class="bi bi-robot text-white small"></i>
                </div>
            </div>` : ''}
            <div class="chat-bubble ${isAi ? 'chat-bubble-ai' : 'chat-bubble-user'} p-3 rounded-3 shadow-sm" style="max-width:75%;">
                <p class="mb-0 small" style="white-space:pre-wrap;">${escapeHtml(content)}</p>
            </div>
        </div>`;

    const container = document.getElementById('chatMessages');
    container.insertAdjacentHTML('beforeend', messageHtml);
    container.scrollTop = container.scrollHeight;
}

function showTypingIndicator() {
    const html = `
        <div class="d-flex mb-3" id="typingIndicator">
            <div class="me-2 flex-shrink-0">
                <div class="rounded-circle bg-primary d-flex align-items-center justify-content-center" style="width:36px;height:36px;">
                    <i class="bi bi-robot text-white small"></i>
                </div>
            </div>
            <div class="chat-bubble chat-bubble-ai p-3 rounded-3 shadow-sm">
                <div class="typing-indicator">
                    <div class="typing-dot"></div>
                    <div class="typing-dot"></div>
                    <div class="typing-dot"></div>
                </div>
            </div>
        </div>`;

    const container = document.getElementById('chatMessages');
    container.insertAdjacentHTML('beforeend', html);
    container.scrollTop = container.scrollHeight;
}

function removeTypingIndicator() {
    const indicator = document.getElementById('typingIndicator');
    if (indicator) indicator.remove();
}

async function sendMessage() {
    const input = document.getElementById('messageInput');
    const message = input.value.trim();
    if (!message) return;

    const model = document.getElementById('modelSelect')?.value ?? 'gpt-3.5-turbo';
    input.value = '';
    autoResizeTextarea(input);

    appendMessage('user', message);
    showTypingIndicator();

    const sendBtn = document.getElementById('sendBtn');
    sendBtn.disabled = true;

    try {
        const response = await fetch('/Chat/Send', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': getAntiForgeryToken() },
            body: JSON.stringify({ message, model, sessionId })
        });

        removeTypingIndicator();

        if (!response.ok) {
            const err = await response.json();
            appendMessage('assistant', `Error: ${err.error || 'Something went wrong.'}`);
        } else {
            const data = await response.json();
            appendMessage('assistant', data.content);
        }
    } catch (e) {
        removeTypingIndicator();
        appendMessage('assistant', 'Connection error. Please check your network.');
    } finally {
        sendBtn.disabled = false;
        input.focus();
    }
}

function handleKeyDown(event) {
    if (event.key === 'Enter' && !event.shiftKey) {
        event.preventDefault();
        sendMessage();
    }
    autoResizeTextarea(event.target);
}

function autoResizeTextarea(el) {
    el.style.height = 'auto';
    el.style.height = Math.min(el.scrollHeight, 200) + 'px';
}

function clearChat() {
    const container = document.getElementById('chatMessages');
    container.innerHTML = '';
    const emptyState = document.createElement('div');
    emptyState.id = 'emptyState';
    emptyState.className = 'text-center text-muted py-5';
    emptyState.innerHTML = `<i class="bi bi-chat-dots fs-1 mb-3 d-block opacity-25"></i><h5>Start a conversation</h5><p class="small">Ask me anything!</p>`;
    container.appendChild(emptyState);
}

function newChat() {
    clearChat();
    sessionId = null;
}

function loadSession(id) {
    sessionId = id;
    clearChat();
}

function useSuggestion(btn) {
    const input = document.getElementById('messageInput');
    input.value = btn.textContent;
    sendMessage();
}

function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.appendChild(document.createTextNode(text));
    return div.innerHTML;
}

// Initialize
document.getElementById('messageInput')?.addEventListener('input', function () {
    autoResizeTextarea(this);
});
