// ChatPortal - Browser-based Conversation History (localStorage)
// Manages all chat sessions stored locally in the browser.

const STORAGE_KEY = 'chatportal_sessions';

/**
 * Generate a UUID v4 string for unique session/message identification.
 * @returns {string} A UUID v4 string.
 */
function generateUUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
        const r = Math.random() * 16 | 0;
        return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
    });
}

/**
 * Load all sessions from localStorage.
 * @returns {Array} Array of session objects.
 */
function loadSessions() {
    try {
        const raw = localStorage.getItem(STORAGE_KEY);
        return raw ? JSON.parse(raw) : [];
    } catch {
        return [];
    }
}

/**
 * Save all sessions to localStorage.
 * @param {Array} sessions - Array of session objects to persist.
 */
function saveSessions(sessions) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(sessions));
}

/**
 * Create a new chat session and persist it.
 * @param {string} [title] - Optional initial title. Defaults to "New Chat".
 * @returns {Object} The newly created session object.
 */
function createSession(title = 'New Chat') {
    const sessions = loadSessions();
    const session = {
        id: generateUUID(),
        title,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        messages: []
    };
    sessions.unshift(session);
    saveSessions(sessions);
    return session;
}

/**
 * Retrieve a single session by its ID.
 * @param {string} id - The session UUID.
 * @returns {Object|null} The session object, or null if not found.
 */
function getSession(id) {
    return loadSessions().find(s => s.id === id) ?? null;
}

/**
 * Add a message to an existing session. If the session title is still
 * "New Chat", auto-generate a title from the first user message.
 * @param {string} sessionId - The session UUID.
 * @param {string} role - "user" or "assistant".
 * @param {string} content - Message content.
 */
function addMessageToSession(sessionId, role, content) {
    const sessions = loadSessions();
    const session = sessions.find(s => s.id === sessionId);
    if (!session) return;

    session.messages.push({ role, content, timestamp: new Date().toISOString() });
    session.updatedAt = new Date().toISOString();

    // Auto-generate title from the first user message
    if (session.title === 'New Chat' && role === 'user') {
        session.title = content.slice(0, 40) + (content.length > 40 ? '…' : '');
    }

    saveSessions(sessions);
}

/**
 * Rename a session by its ID.
 * @param {string} sessionId - The session UUID.
 * @param {string} newTitle - The new title string.
 */
function renameSession(sessionId, newTitle) {
    const sessions = loadSessions();
    const session = sessions.find(s => s.id === sessionId);
    if (session) {
        session.title = newTitle.trim() || 'New Chat';
        session.updatedAt = new Date().toISOString();
        saveSessions(sessions);
    }
}

/**
 * Delete a session from localStorage.
 * @param {string} sessionId - The session UUID.
 */
function deleteSession(sessionId) {
    const sessions = loadSessions().filter(s => s.id !== sessionId);
    saveSessions(sessions);
}

/**
 * Search sessions by title (case-insensitive).
 * @param {string} query - Search string.
 * @returns {Array} Filtered array of sessions whose titles match the query.
 */
function searchSessions(query) {
    const q = query.toLowerCase().trim();
    if (!q) return loadSessions();
    return loadSessions().filter(s => s.title.toLowerCase().includes(q));
}

// ── Sidebar Rendering ─────────────────────────────────────────────────────

let currentSessionId = null;

/**
 * Format an ISO date string as a human-readable relative or absolute label.
 * @param {string} iso - ISO date string.
 * @returns {string} Formatted date label.
 */
function formatSessionDate(iso) {
    const d = new Date(iso);
    const now = new Date();
    const diffMs = now - d;
    const diffDays = Math.floor(diffMs / 86400000);
    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 7) return `${diffDays}d ago`;
    return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}

/**
 * Render the sidebar session list, optionally filtered by a search query.
 * @param {string} [filter] - Optional search filter string.
 */
function renderSidebar(filter = '') {
    const list = document.getElementById('sessionList');
    if (!list) return;
    const sessions = filter ? searchSessions(filter) : loadSessions();

    list.innerHTML = sessions.length === 0
        ? `<div class="text-muted small text-center py-3">No conversations yet</div>`
        : sessions.map(s => buildSessionItemHtml(s)).join('');

    // Attach event listeners after rendering
    list.querySelectorAll('.session-item').forEach(el => {
        const sid = el.dataset.sessionId;

        el.addEventListener('click', e => {
            if (e.target.closest('.session-menu-btn') || e.target.closest('.session-context-menu')) return;
            activateSession(sid);
        });

        el.addEventListener('contextmenu', e => {
            e.preventDefault();
            showContextMenu(e.clientX, e.clientY, sid);
        });

        el.querySelector('.session-menu-btn')?.addEventListener('click', e => {
            e.stopPropagation();
            const rect = e.currentTarget.getBoundingClientRect();
            showContextMenu(rect.left, rect.bottom, sid);
        });
    });
}

/**
 * Build HTML string for a single session item in the sidebar.
 * @param {Object} session - Session object with id, title, updatedAt.
 * @returns {string} HTML string.
 */
function buildSessionItemHtml(session) {
    const isActive = session.id === currentSessionId;
    return `
        <div class="session-item d-flex align-items-center gap-2 p-2 rounded-3 mb-1 position-relative ${isActive ? 'session-active' : ''}"
             data-session-id="${session.id}" style="cursor:pointer;">
            <i class="bi bi-chat-left text-muted small flex-shrink-0"></i>
            <div class="overflow-hidden flex-grow-1">
                <div class="small text-truncate fw-medium session-title">${escapeHtml(session.title)}</div>
                <div class="text-muted" style="font-size:0.7rem;">${formatSessionDate(session.updatedAt)}</div>
            </div>
            <button class="btn btn-sm session-menu-btn text-muted opacity-0 p-0 px-1 border-0" title="Options" tabindex="-1">
                <i class="bi bi-three-dots-vertical" style="font-size:0.75rem;"></i>
            </button>
        </div>`;
}

// Show menu button on hover
document.addEventListener('mouseover', e => {
    const item = e.target.closest('.session-item');
    if (item) item.querySelector('.session-menu-btn')?.classList.remove('opacity-0');
});
document.addEventListener('mouseout', e => {
    const item = e.target.closest('.session-item');
    if (item && !item.matches(':hover')) item.querySelector('.session-menu-btn')?.classList.add('opacity-0');
});

// ── Context Menu ──────────────────────────────────────────────────────────

let contextMenuTarget = null;

/**
 * Display the floating context menu for a session item.
 * @param {number} x - Horizontal position (pixels from left).
 * @param {number} y - Vertical position (pixels from top).
 * @param {string} sessionId - The session UUID this menu applies to.
 */
function showContextMenu(x, y, sessionId) {
    removeContextMenu();
    contextMenuTarget = sessionId;
    const menu = document.createElement('div');
    menu.id = 'sessionContextMenu';
    menu.className = 'session-context-menu shadow-lg rounded-3 py-1';
    menu.style.cssText = `position:fixed;left:${x}px;top:${y}px;z-index:9999;min-width:140px;background:#1e2130;border:1px solid rgba(255,255,255,0.1);`;
    menu.innerHTML = `
        <button class="dropdown-item text-light small px-3 py-2" onclick="startRename('${sessionId}')">
            <i class="bi bi-pencil me-2"></i>Rename
        </button>
        <div class="dropdown-divider border-secondary my-1"></div>
        <button class="dropdown-item text-danger small px-3 py-2" onclick="confirmDeleteSession('${sessionId}')">
            <i class="bi bi-trash3 me-2"></i>Delete
        </button>`;
    document.body.appendChild(menu);

    // Close on outside click
    setTimeout(() => document.addEventListener('click', removeContextMenu, { once: true }), 50);
}

/**
 * Remove the floating context menu from the DOM.
 */
function removeContextMenu() {
    document.getElementById('sessionContextMenu')?.remove();
    contextMenuTarget = null;
}

/**
 * Begin inline rename for a session in the sidebar.
 * @param {string} sessionId - The session UUID to rename.
 */
function startRename(sessionId) {
    removeContextMenu();
    const item = document.querySelector(`.session-item[data-session-id="${sessionId}"]`);
    if (!item) return;
    const titleEl = item.querySelector('.session-title');
    const currentTitle = getSession(sessionId)?.title ?? '';

    const input = document.createElement('input');
    input.type = 'text';
    input.value = currentTitle;
    input.className = 'form-control form-control-sm p-1 border-0 shadow-none';
    input.style.cssText = 'background:rgba(255,255,255,0.08);color:#fff;font-size:0.8rem;';

    titleEl.replaceWith(input);
    input.focus();
    input.select();

    const commit = () => {
        const newTitle = input.value.trim() || 'New Chat';
        renameSession(sessionId, newTitle);
        renderSidebar(document.getElementById('sessionSearch')?.value ?? '');
    };

    input.addEventListener('blur', commit);
    input.addEventListener('keydown', e => {
        if (e.key === 'Enter') { e.preventDefault(); commit(); }
        if (e.key === 'Escape') { renderSidebar(); }
    });
}

/**
 * Confirm and delete a session, prompting the user first.
 * @param {string} sessionId - The session UUID to delete.
 */
function confirmDeleteSession(sessionId) {
    removeContextMenu();
    if (!confirm('Delete this conversation? This cannot be undone.')) return;
    deleteSession(sessionId);
    if (currentSessionId === sessionId) {
        currentSessionId = null;
        clearChatUI();
    }
    renderSidebar(document.getElementById('sessionSearch')?.value ?? '');
}

// ── Session Activation ────────────────────────────────────────────────────

/**
 * Activate a session: load its messages into the chat area and highlight it.
 * @param {string} sessionId - The session UUID to activate.
 */
function activateSession(sessionId) {
    currentSessionId = sessionId;
    renderSidebar(document.getElementById('sessionSearch')?.value ?? '');
    const session = getSession(sessionId);
    clearChatUI();
    if (session && session.messages.length > 0) {
        session.messages.forEach(msg => appendMessage(msg.role, msg.content, false));
    } else {
        showEmptyState();
    }
}

/**
 * Clear the chat message area and optionally show the empty state placeholder.
 */
function clearChatUI() {
    const container = document.getElementById('chatMessages');
    if (container) container.innerHTML = '';
    showEmptyState();
}

/**
 * Show the empty-state placeholder inside the chat messages container.
 */
function showEmptyState() {
    const container = document.getElementById('chatMessages');
    if (!container) return;
    // Only show if there are no messages
    if (container.querySelectorAll('.message-item').length === 0) {
        container.innerHTML = `
            <div class="text-center text-muted py-5" id="emptyState">
                <i class="bi bi-chat-dots fs-1 mb-3 d-block opacity-25"></i>
                <h5>Start a conversation</h5>
                <p class="small">Ask me anything — code, writing, analysis, or just chat!</p>
                <div class="row g-2 mt-3 justify-content-center">
                    <div class="col-auto"><button class="btn btn-outline-secondary btn-sm suggestion-btn" onclick="useSuggestion(this)">Write a Python function to sort a list</button></div>
                    <div class="col-auto"><button class="btn btn-outline-secondary btn-sm suggestion-btn" onclick="useSuggestion(this)">Explain quantum computing simply</button></div>
                    <div class="col-auto"><button class="btn btn-outline-secondary btn-sm suggestion-btn" onclick="useSuggestion(this)">Help me write a cover letter</button></div>
                    <div class="col-auto"><button class="btn btn-outline-secondary btn-sm suggestion-btn" onclick="useSuggestion(this)">Debug this JavaScript code</button></div>
                </div>
            </div>`;
    }
}

/**
 * Create a new session and activate it in the UI.
 */
function newChat() {
    const session = createSession();
    currentSessionId = session.id;
    renderSidebar();
    clearChatUI();
}

// ── Search ────────────────────────────────────────────────────────────────

// Wire up the search input once DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    renderSidebar();
    // If no session, create one
    if (!currentSessionId) {
        const sessions = loadSessions();
        if (sessions.length > 0) {
            activateSession(sessions[0].id);
        } else {
            newChat();
        }
    }

    const searchInput = document.getElementById('sessionSearch');
    if (searchInput) {
        searchInput.addEventListener('input', () => renderSidebar(searchInput.value));
    }
});
