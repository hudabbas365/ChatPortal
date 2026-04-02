// ChatPortal - Chat JavaScript

let sessionId = null;

function appendMessage(role, content) {
    const isAi = role.toLowerCase() === 'assistant';
    const emptyState = document.getElementById('emptyState');
    if (emptyState) emptyState.style.display = 'none';

    // Check if content is a structured AI response (JSON object)
    let displayContent = content;
    let isStructured = false;
    if (isAi && typeof content === 'string' && content.trim().startsWith('{')) {
        try {
            const parsed = JSON.parse(content);
            if (parsed.narrative || parsed.query) {
                displayContent = buildStructuredHtml(parsed);
                isStructured = true;
            }
        } catch { /* not JSON, render as plain text */ }
    }

    const messageHtml = `
        <div class="d-flex ${isAi ? '' : 'justify-content-end'} mb-3 message-item">
            ${isAi ? `
            <div class="me-2 flex-shrink-0">
                <div class="rounded-circle bg-primary d-flex align-items-center justify-content-center" style="width:36px;height:36px;">
                    <i class="bi bi-robot text-white small"></i>
                </div>
            </div>` : ''}
            <div class="chat-bubble ${isAi ? 'chat-bubble-ai' : 'chat-bubble-user'} p-3 rounded-3 shadow-sm" style="max-width:80%;">
                ${isStructured ? displayContent : `<p class="mb-0 small" style="white-space:pre-wrap;">${escapeHtml(String(content))}</p>`}
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

function buildStructuredHtml(data) {
    let html = '';
    if (data.narrative) html += `<p class="mb-2 small" style="white-space:pre-wrap;">${escapeHtml(data.narrative)}</p>`;
    if (data.query) {
        html += `<details class="mb-2"><summary class="small text-muted" style="cursor:pointer;">Generated Query</summary>
            <pre class="mt-1 p-2 rounded small" style="background:#1e1e2e;color:#cdd6f4;overflow-x:auto;">${escapeHtml(data.query)}</pre>
            </details>`;
    }
    if (data.result && Array.isArray(data.result) && data.result.length > 0) {
        const keys = Object.keys(data.result[0]);
        html += `<div class="table-responsive mb-2"><table class="table table-sm mb-0" style="font-size:0.78rem;">
            <thead><tr>${keys.map(k => `<th>${escapeHtml(k)}</th>`).join('')}</tr></thead>
            <tbody>${data.result.slice(0,20).map(r => `<tr>${keys.map(k => `<td>${escapeHtml(String(r[k] ?? ''))}</td>`).join('')}</tr>`).join('')}</tbody>
            </table></div>`;
    }
    if (data.prompts?.length) {
        html += `<div class="d-flex flex-wrap gap-1 mb-1">${data.prompts.map(p =>
            `<button class="btn btn-sm rounded-pill chat-prompt-btn" style="background:#F3E8FF;color:#7c4dff;font-size:0.75rem;"
             data-prompt="${escapeHtml(p)}">${escapeHtml(p)}</button>`
        ).join('')}</div>`;
    }
    if (data.examples?.length) {
        html += `<div class="mt-1"><small class="text-muted fw-semibold">Examples: </small>${data.examples.map(e =>
            `<button class="btn btn-sm btn-outline-secondary ms-1 rounded-pill chat-prompt-btn" style="font-size:0.72rem;"
             data-prompt="${escapeHtml(e)}">${escapeHtml(e)}</button>`
        ).join('')}</div>`;
    }
    return html || `<p class="mb-0 small text-muted">No content</p>`;
}

// Initialize
document.getElementById('messageInput')?.addEventListener('input', function () {
    autoResizeTextarea(this);
});

// Event delegation for structured response prompt buttons
document.addEventListener('click', e => {
    const btn = e.target.closest('.chat-prompt-btn');
    if (btn) {
        const prompt = btn.dataset.prompt;
        if (prompt) {
            const input = document.getElementById('messageInput');
            if (input) {
                input.value = prompt;
                autoResizeTextarea(input);
                input.focus();
            }
        }
    }
});

// ── Voice Input (Web Speech API) ──────────────────────────────────────────
let recognition = null;
let isRecording = false;

function toggleMic() {
    if (!('SpeechRecognition' in window || 'webkitSpeechRecognition' in window)) {
        alert('Voice input is not supported in your browser. Try Chrome or Edge.');
        return;
    }

    if (isRecording) {
        recognition?.stop();
        return;
    }

    const SR = window.SpeechRecognition || window.webkitSpeechRecognition;
    recognition = new SR();
    recognition.lang = 'en-US';
    recognition.interimResults = false;
    recognition.maxAlternatives = 1;

    recognition.onstart = () => {
        isRecording = true;
        const btn = document.getElementById('micBtn');
        const icon = document.getElementById('micIcon');
        if (btn) { btn.classList.add('text-danger'); btn.title = 'Stop recording'; }
        if (icon) { icon.className = 'bi bi-record-circle-fill'; }
    };

    recognition.onresult = (e) => {
        const transcript = e.results[0][0].transcript;
        const input = document.getElementById('messageInput');
        if (input) {
            input.value += (input.value ? ' ' : '') + transcript;
            autoResizeTextarea(input);
            input.focus();
        }
    };

    recognition.onerror = (e) => {
        console.warn('Speech recognition error:', e.error);
        stopMicUI();
    };

    recognition.onend = () => {
        stopMicUI();
    };

    recognition.start();
}

function stopMicUI() {
    isRecording = false;
    const btn = document.getElementById('micBtn');
    const icon = document.getElementById('micIcon');
    if (btn) { btn.classList.remove('text-danger'); btn.title = 'Voice input'; }
    if (icon) { icon.className = 'bi bi-mic-fill'; }
}
