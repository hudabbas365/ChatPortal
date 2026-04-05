// ChatPortal - Main Chat JavaScript
// Handles message sending, rendering (Markdown + code highlighting),
// file attachment, PDF export, right panel, and voice input.

// ── Constants ─────────────────────────────────────────────────────────────
/** Maximum number of data rows rendered inline in a structured AI response table. */
const MAX_TABLE_ROWS = 20;

// ── Attachment state ──────────────────────────────────────────────────────
let attachedFile = null;

/**
 * Append a message bubble to the chat messages container.
 * AI responses are rendered as Markdown; user messages are plain-text escaped.
 * @param {string} role - "user" or "assistant".
 * @param {string} content - Raw message content.
 * @param {boolean} [persist=true] - Whether to save the message to the active localStorage session.
 */
function appendMessage(role, content, persist = true) {
    const isAi = role.toLowerCase() === 'assistant';
    const emptyState = document.getElementById('emptyState');
    if (emptyState) emptyState.remove();

    // Determine how to render the content
    let renderedContent;
    if (isAi) {
        // Try structured JSON first (data insights responses)
        if (typeof content === 'string' && content.trim().startsWith('{')) {
            try {
                const parsed = JSON.parse(content);
                if (parsed.narrative || parsed.query) {
                    renderedContent = buildStructuredHtml(parsed);
                    if (persist && typeof currentSessionId !== 'undefined' && currentSessionId) {
                        addMessageToSession(currentSessionId, role, content);
                        renderSidebar(document.getElementById('sessionSearch')?.value ?? '');
                    }
                    _insertMessageBubble(isAi, renderedContent, true);
                    initPendingCharts();
                    updateRightPanel(parsed);
                    return;
                }
            } catch { /* not JSON */ }
        }
        // Render Markdown (marked.js + DOMPurify)
        renderedContent = renderMarkdown(content);
    } else {
        renderedContent = `<p class="mb-0 small" style="white-space:pre-wrap;">${escapeHtml(String(content))}</p>`;
    }

    if (persist && typeof currentSessionId !== 'undefined' && currentSessionId) {
        addMessageToSession(currentSessionId, role, content);
        renderSidebar(document.getElementById('sessionSearch')?.value ?? '');
    }

    _insertMessageBubble(isAi, renderedContent, false);
}

/**
 * Insert a message bubble DOM element into the chat messages container.
 * @param {boolean} isAi - True if the message is from the AI assistant.
 * @param {string} renderedContent - HTML string for the bubble body.
 * @param {boolean} isStructured - True when the content was built from structured JSON.
 */
function _insertMessageBubble(isAi, renderedContent, isStructured) {
    const messageHtml = `
        <div class="d-flex ${isAi ? '' : 'justify-content-end'} mb-3 message-item">
            ${isAi ? `
            <div class="me-2 flex-shrink-0">
                <div class="rounded-circle bg-primary d-flex align-items-center justify-content-center" style="width:36px;height:36px;">
                    <i class="bi bi-robot text-white small"></i>
                </div>
            </div>` : ''}
            <div class="chat-bubble ${isAi ? 'chat-bubble-ai' : 'chat-bubble-user'} p-3 rounded-3 shadow-sm" style="max-width:80%;">
                ${renderedContent}
            </div>
        </div>`;

    const container = document.getElementById('chatMessages');
    container.insertAdjacentHTML('beforeend', messageHtml);

    // Apply syntax highlighting to any newly added code blocks
    container.querySelectorAll('pre code:not(.hljs)').forEach(block => {
        if (typeof hljs !== 'undefined') hljs.highlightElement(block);
    });

    container.scrollTop = container.scrollHeight;
}

/**
 * Render a Markdown string to sanitised HTML.
 * Uses marked.js for parsing and DOMPurify for XSS sanitisation.
 * Falls back to escaped plain text if libraries are unavailable.
 * @param {string} markdown - The raw Markdown string.
 * @returns {string} Safe HTML string.
 */
function renderMarkdown(markdown) {
    if (typeof marked === 'undefined') {
        return `<p class="mb-0 small" style="white-space:pre-wrap;">${escapeHtml(markdown)}</p>`;
    }
    try {
        const raw = marked.parse(markdown, { breaks: true, gfm: true });
        return typeof DOMPurify !== 'undefined' ? DOMPurify.sanitize(raw) : raw;
    } catch {
        return `<p class="mb-0 small" style="white-space:pre-wrap;">${escapeHtml(markdown)}</p>`;
    }
}

/**
 * Display the animated typing indicator inside the chat messages container.
 */
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

/**
 * Remove the typing indicator from the chat messages container.
 */
function removeTypingIndicator() {
    document.getElementById('typingIndicator')?.remove();
}

/**
 * Send the current message (and optional file attachment) to the server.
 * Updates localStorage history with both the user message and the AI response.
 */
async function sendMessage() {
    const input = document.getElementById('messageInput');
    const message = input.value.trim();
    if (!message && !attachedFile) return;

    const model = document.getElementById('modelSelect')?.value ?? 'gpt-3.5-turbo';
    // Require a datasource — get from window state set when session was created
    const dataSourceId = window._activeChatDataSourceId || null;
    if (!dataSourceId) {
        appendMessage('assistant', 'Error: A datasource with AI Insights is required. Please start a new chat and select a data source.');
        return;
    }

    input.value = '';
    autoResizeTextarea(input);

    // Build display message (include filename if file attached)
    const displayMessage = attachedFile
        ? `${message}\n📎 ${attachedFile.name}`
        : message;

    appendMessage('user', displayMessage);
    showTypingIndicator();

    const sendBtn = document.getElementById('sendBtn');
    sendBtn.disabled = true;

    try {
        let bodyContent;
        const headers = {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken()
        };

        if (attachedFile) {
            const fileContent = await readFileAsText(attachedFile);
            bodyContent = JSON.stringify({
                message: message + '\n\n[Attached file: ' + attachedFile.name + ']\n' + fileContent,
                model,
                dataSourceId,
                sessionId: window._activeChatSessionId || null
            });
        } else {
            bodyContent = JSON.stringify({ message, model, dataSourceId, sessionId: window._activeChatSessionId || null });
        }

        clearAttachment();

        const response = await fetch('/Chat/Send', {
            method: 'POST',
            headers,
            body: bodyContent
        });

        removeTypingIndicator();

        if (!response.ok) {
            const err = await response.json();
            appendMessage('assistant', `Error: ${err.error || 'Something went wrong.'}`);
        } else {
            const data = await response.json();
            if (data.success) {
                // Structured data insights response
                const content = JSON.stringify(data);
                appendMessage('assistant', content);
            } else {
                appendMessage('assistant', data.content || data.error || 'No response.');
            }
        }
    } catch (e) {
        removeTypingIndicator();
        appendMessage('assistant', 'Connection error. Please check your network.');
    } finally {
        sendBtn.disabled = false;
        input.focus();
    }
}

/**
 * Handle keydown events on the message textarea.
 * Submits on Enter (without Shift) and auto-resizes on any key.
 * @param {KeyboardEvent} event - The keyboard event.
 */
function handleKeyDown(event) {
    if (event.key === 'Enter' && !event.shiftKey) {
        event.preventDefault();
        sendMessage();
    }
    autoResizeTextarea(event.target);
}

/**
 * Auto-resize a textarea element to fit its content, up to 200 px.
 * @param {HTMLTextAreaElement} el - The textarea element.
 */
function autoResizeTextarea(el) {
    el.style.height = 'auto';
    el.style.height = Math.min(el.scrollHeight, 200) + 'px';
}

/**
 * Clear the chat messages area and show the empty state placeholder.
 * Also resets the active localStorage session message history.
 */
function clearChat() {
    clearChatUI();
    if (typeof currentSessionId !== 'undefined' && currentSessionId) {
        const sessions = loadSessions();
        const s = sessions.find(x => x.id === currentSessionId);
        if (s) {
            s.messages = [];
            s.title = 'New Chat';
            saveSessions(sessions);
        }
        renderSidebar();
    }
}

/**
 * Use a suggestion chip as the message input and immediately send it.
 * @param {HTMLElement} btn - The suggestion button element.
 */
function useSuggestion(btn) {
    const input = document.getElementById('messageInput');
    input.value = btn.textContent.trim();
    sendMessage();
}

/**
 * Get the ASP.NET Core anti-forgery token value from the hidden form field.
 * @returns {string} The token string, or empty string if not found.
 */
function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
}

/**
 * Escape special HTML characters in a string to prevent XSS.
 * @param {string} text - Raw text to escape.
 * @returns {string} HTML-escaped string.
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.appendChild(document.createTextNode(text));
    return div.innerHTML;
}

/**
 * Build an HTML representation for a structured AI data-insights response object.
 * @param {Object} data - Parsed response with optional narrative, query, result, prompts, examples, chartData.
 * @returns {string} HTML string.
 */
function buildStructuredHtml(data) {
    let html = '';
    if (data.narrative) html += `<p class="mb-2 small" style="white-space:pre-wrap;">${escapeHtml(data.narrative)}</p>`;
    if (data.query) {
        html += `<details class="mb-2"><summary class="small text-muted" style="cursor:pointer;">Generated Query</summary>
            <pre class="mt-1 p-2 rounded small" style="background:#1e1e2e;color:#cdd6f4;overflow-x:auto;">${escapeHtml(data.query)}</pre>
            </details>`;
    }
    let hasTableData = false;
    if (data.result && Array.isArray(data.result) && data.result.length > 0) {
        hasTableData = true;
        const keys = Object.keys(data.result[0]);
        html += `<div class="table-responsive mb-2"><table class="table table-sm mb-0" style="font-size:0.78rem;">
            <thead><tr>${keys.map(k => `<th>${escapeHtml(k)}</th>`).join('')}</tr></thead>
            <tbody>${data.result.slice(0, MAX_TABLE_ROWS).map(r => `<tr>${keys.map(k => `<td>${escapeHtml(String(r[k] ?? ''))}</td>`).join('')}</tr>`).join('')}</tbody>
            </table></div>`;
        // Add "Generate Chart" button for tabular data
        html += `<button class="btn btn-sm mb-2" style="background:linear-gradient(135deg,#667eea,#764ba2);color:#fff;font-size:0.78rem;" data-result='${escapeHtml(JSON.stringify(data.result))}' onclick="openChartPanelFromBtn(this)"><i class="bi bi-bar-chart-fill me-1"></i>Generate Chart</button>`;
    }
    if (data.chartData && data.chartData.labels && data.chartData.datasets) {
        const chartId = 'chart-' + Math.random().toString(36).slice(2, 9);
        const safeData = encodeURIComponent(JSON.stringify(data.chartData));
        html += `<div class="mt-2 mb-2" style="position:relative;max-height:280px;">
            <canvas id="${chartId}" data-chart="${safeData}" style="max-height:260px;"></canvas>
        </div>`;
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

/**
 * Initialise any Chart.js charts whose canvas elements have a pending `data-chart` attribute.
 * Called after structured AI response bubbles are inserted into the DOM.
 */
function initPendingCharts() {
    if (typeof Chart === 'undefined') return;
    document.querySelectorAll('canvas[data-chart]').forEach(canvas => {
        try {
            const chartData = JSON.parse(decodeURIComponent(canvas.dataset.chart));
            const defaultColors = [
                'rgba(13,110,253,0.6)', 'rgba(102,16,242,0.6)', 'rgba(25,135,84,0.6)',
                'rgba(220,53,69,0.6)', 'rgba(255,193,7,0.6)', 'rgba(13,202,240,0.6)'
            ];
            new Chart(canvas, {
                type: chartData.chartType || 'bar',
                data: {
                    labels: chartData.labels,
                    datasets: chartData.datasets.map((ds, i) => ({
                        label: ds.label,
                        data: ds.data,
                        backgroundColor: ds.backgroundColor || defaultColors[i % defaultColors.length],
                        borderColor: ds.borderColor || defaultColors[i % defaultColors.length].replace('0.6', '1'),
                        borderWidth: 1,
                        fill: chartData.chartType === 'line' ? false : undefined
                    }))
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: true,
                    plugins: {
                        legend: { display: chartData.datasets.length > 1 }
                    }
                }
            });
            canvas.removeAttribute('data-chart');
        } catch (err) { console.warn('Failed to initialize chart:', err); }
    });
}

// ── File Attachment ───────────────────────────────────────────────────────

/**
 * Trigger the hidden file input dialog for attaching a file.
 */
function triggerFileAttach() {
    document.getElementById('fileInput')?.click();
}

/**
 * Handle file selection from the hidden file input.
 * Displays a chip/badge above the input area showing the selected filename.
 * @param {Event} event - The file input change event.
 */
function handleFileSelected(event) {
    const file = event.target.files?.[0];
    if (!file) return;
    attachedFile = file;

    const chip = document.getElementById('attachmentChip');
    if (chip) {
        chip.innerHTML = `<span class="badge bg-secondary me-1"><i class="bi bi-paperclip me-1"></i>${escapeHtml(file.name)}
            <button type="button" class="btn-close btn-close-white ms-1" style="font-size:0.6rem;" onclick="clearAttachment()"></button>
        </span>`;
        chip.style.display = 'block';
    }
    // Reset the input so the same file can be re-selected if needed
    event.target.value = '';
}

/**
 * Clear the currently attached file and hide the chip.
 */
function clearAttachment() {
    attachedFile = null;
    const chip = document.getElementById('attachmentChip');
    if (chip) { chip.innerHTML = ''; chip.style.display = 'none'; }
}

/**
 * Read a File object as plain text.
 * @param {File} file - The file to read.
 * @returns {Promise<string>} Resolves with the file content string.
 */
function readFileAsText(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = e => resolve(e.target.result);
        reader.onerror = () => reject(new Error('Failed to read file'));
        reader.readAsText(file);
    });
}

// ── PDF Export ────────────────────────────────────────────────────────────

/**
 * Export the current chat conversation as a styled PDF file.
 * Requires the html2pdf.js library to be loaded via CDN.
 * Downloads the PDF as "ChatPortal-Export-{date}.pdf".
 */
function exportAsPDF() {
    if (typeof html2pdf === 'undefined') {
        alert('PDF export is unavailable. Please refresh the page and try again.');
        return;
    }
    const messages = document.getElementById('chatMessages');
    if (!messages) return;

    const dateStr = new Date().toISOString().split('T')[0];
    const wrapper = document.createElement('div');
    wrapper.style.cssText = 'font-family:Arial,sans-serif;padding:20px;max-width:800px;';
    wrapper.innerHTML = `
        <div style="border-bottom:2px solid #0d6efd;padding-bottom:12px;margin-bottom:20px;">
            <h2 style="color:#0d6efd;margin:0;">ChatPortal</h2>
            <p style="color:#666;margin:4px 0 0;">Chat Export — ${new Date().toLocaleString()}</p>
        </div>
        ${messages.cloneNode(true).innerHTML}`;

    // Remove action buttons from export
    wrapper.querySelectorAll('button, .session-menu-btn').forEach(b => b.remove());

    html2pdf().set({
        margin: [10, 10, 10, 10],
        filename: `ChatPortal-Export-${dateStr}.pdf`,
        image: { type: 'jpeg', quality: 0.95 },
        html2canvas: { scale: 2, useCORS: true },
        jsPDF: { unit: 'mm', format: 'a4', orientation: 'portrait' }
    }).from(wrapper).save();
}

// ── Right Panel (Source References) ──────────────────────────────────────

let rightPanelOpen = false;

/**
 * Toggle the visibility of the right-hand source-references panel.
 */
function toggleRightPanel() {
    const panel = document.getElementById('rightPanel');
    if (!panel) return;
    rightPanelOpen = !rightPanelOpen;
    panel.classList.toggle('panel-open', rightPanelOpen);
    const btn = document.getElementById('togglePanelBtn');
    if (btn) btn.classList.toggle('active', rightPanelOpen);
}

/**
 * Update the right panel with source reference data from a structured AI response.
 * @param {Object} data - Structured response data containing optional query and result fields.
 */
function updateRightPanel(data) {
    const content = document.getElementById('rightPanelContent');
    if (!content) return;

    let html = '<div class="p-3">';
    html += '<h6 class="fw-bold mb-3 small text-uppercase text-muted">Source References</h6>';

    if (data.query) {
        html += `<div class="mb-3">
            <div class="small fw-semibold text-muted mb-1">Generated SQL</div>
            <pre class="p-2 rounded small" style="background:#1e1e2e;color:#cdd6f4;overflow-x:auto;font-size:0.75rem;">${escapeHtml(data.query)}</pre>
        </div>`;
    }

    if (data.result && Array.isArray(data.result) && data.result.length > 0) {
        html += `<div class="mb-3">
            <div class="small fw-semibold text-muted mb-1">Data Snippet (${data.result.length} rows)</div>
            <div class="table-responsive" style="font-size:0.72rem;">`;
        const keys = Object.keys(data.result[0]);
        html += `<table class="table table-sm table-dark mb-0">
            <thead><tr>${keys.map(k => `<th>${escapeHtml(k)}</th>`).join('')}</tr></thead>
            <tbody>${data.result.slice(0, 5).map(r =>
            `<tr>${keys.map(k => `<td>${escapeHtml(String(r[k] ?? ''))}</td>`).join('')}</tr>`
        ).join('')}</tbody></table></div></div>`;
    }

    html += '</div>';
    content.innerHTML = html;

    // Auto-open the panel when new data arrives
    if (!rightPanelOpen) toggleRightPanel();
}

// ── Voice Input ───────────────────────────────────────────────────────────

let recognition = null;
let isRecording = false;

/**
 * Toggle voice input recording using the Web Speech API.
 * Shows an alert if the API is unsupported by the current browser.
 */
function toggleMic() {
    if (!('SpeechRecognition' in window || 'webkitSpeechRecognition' in window)) {
        alert('Voice input is not supported in your browser. Try Chrome, Edge, or Safari.');
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

    recognition.onresult = e => {
        const transcript = e.results[0][0].transcript;
        const input = document.getElementById('messageInput');
        if (input) {
            input.value += (input.value ? ' ' : '') + transcript;
            autoResizeTextarea(input);
            input.focus();
        }
    };

    recognition.onerror = e => {
        console.warn('Speech recognition error:', e.error);
        stopMicUI();
    };

    recognition.onend = () => stopMicUI();
    recognition.start();
}

/**
 * Reset the microphone button UI to its default (non-recording) state.
 */
function stopMicUI() {
    isRecording = false;
    const btn = document.getElementById('micBtn');
    const icon = document.getElementById('micIcon');
    if (btn) { btn.classList.remove('text-danger'); btn.title = 'Voice input'; }
    if (icon) { icon.className = 'bi bi-mic-fill'; }
}

// ── Initialisation ────────────────────────────────────────────────────────

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

// ── Generate Chart from structured response data ──────────────────────────
function openChartPanelFromBtn(btn) {
    let resultData = [];
    try { resultData = JSON.parse(btn.getAttribute('data-result') || '[]'); } catch {}
    const labels = [], datasets = [];
    if (resultData.length > 0) {
        const keys = Object.keys(resultData[0]);
        if (keys.length >= 2) {
            resultData.forEach(r => labels.push(String(r[keys[0]] ?? '')));
            datasets.push({ label: keys[1], data: resultData.map(r => parseFloat(r[keys[1]]) || 0), backgroundColor: 'rgba(173,216,230,0.7)', borderColor: 'rgba(100,149,237,0.9)', borderWidth: 1 });
        }
    }
    if (typeof openChartPanel === 'function') {
        // Build fake bubble reference to trigger panel
        openChartPanel(btn.closest('.chat-bubble-ai'), labels, datasets);
    } else {
        // Fallback: directly open panel with data
        window._chartPanelLabels = labels;
        window._chartPanelDatasets = datasets;
        if (typeof confirmGenerateChart === 'function') {
            const chartType = prompt('Chart type (bar/pie/doughnut/radar/line):', 'bar') || 'bar';
            const title = prompt('Chart title:', 'Result Chart') || 'Result Chart';
            confirmGenerateChart(labels, datasets, chartType, title);
        }
    }
}
