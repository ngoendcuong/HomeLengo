// ===== SignalR connection =====
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .build();

// ===== LocalStorage keys =====
const CHAT_KEY = "hl_chat_history_v1";
const CHAT_OPEN_KEY = "hl_chat_open_v1";

// ===== Helpers =====
function appendMessage(isUser, message, skipSave = false) {
    const chatBody = document.getElementById("chatBody");
    if (!chatBody) return;

    const div = document.createElement("div");
    div.className = isUser ? "hl-msg hl-msg--user" : "hl-msg hl-msg--bot";
    div.textContent = message;
    chatBody.appendChild(div);
    chatBody.scrollTop = chatBody.scrollHeight;

    if (!skipSave) {
        saveChat(isUser ? "user" : "bot", message);
    }
}

// ===== Storage =====
function saveChat(role, message) {
    try {
        const arr = JSON.parse(localStorage.getItem(CHAT_KEY) || "[]");
        arr.push({ role, message, t: Date.now() });

        // giới hạn 200 tin nhắn gần nhất
        const limited = arr.slice(-200);

        localStorage.setItem(CHAT_KEY, JSON.stringify(limited));
    } catch (e) {
        console.error("saveChat error:", e);
    }
}

function loadChat() {
    try {
        const arr = JSON.parse(localStorage.getItem(CHAT_KEY) || "[]");
        for (const m of arr) {
            // khi load lại: chỉ hiển thị, không save lại
            appendMessage(m.role === "user", m.message, true);
        }
    } catch (e) {
        console.error("loadChat error:", e);
    }
}

// (Tuỳ chọn) Xóa lịch sử chat nếu bạn muốn gắn vào nút "Clear"
function clearChat() {
    localStorage.removeItem(CHAT_KEY);
    const chatBody = document.getElementById("chatBody");
    if (chatBody) chatBody.innerHTML = "";
}

// ===== Popup open/close persistence (tuỳ chọn) =====
function setChatOpen(isOpen) {
    try {
        localStorage.setItem(CHAT_OPEN_KEY, isOpen ? "1" : "0");
    } catch { }
}

function applyChatOpenState() {
    const popup = document.getElementById("chatPopup");
    if (!popup) return;

    const isOpen = localStorage.getItem(CHAT_OPEN_KEY) === "1";
    popup.style.display = isOpen ? "block" : "none";
}

// ===== Receive from server =====
connection.on("ReceiveMessage", (user, message) => {
    appendMessage(user === "Bạn", message);
});

// ===== Start connection =====
connection.start().catch(err => console.error("SignalR start error:", err));

// ===== Send =====
function sendMessage() {
    const input = document.getElementById("msgInput");
    if (!input) return;

    const text = (input.value || "").trim();
    if (!text) return;

    appendMessage(true, text);
    connection.invoke("SendMessageToBot", text).catch(err => console.error("invoke error:", err));
    input.value = "";
}

// ===== Toggle =====
function toggleChat() {
    const popup = document.getElementById("chatPopup");
    if (!popup) return;

    const isOpen = popup.style.display === "block";
    popup.style.display = isOpen ? "none" : "block";
    setChatOpen(!isOpen);
}

function handleEnter(e) {
    if (e.key === "Enter") sendMessage();
}

// ===== Init on every page load =====
document.addEventListener("DOMContentLoaded", () => {
    loadChat();
    applyChatOpenState();
});
