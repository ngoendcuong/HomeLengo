const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .build();

function appendMessage(isUser, message) {
    const chatBody = document.getElementById("chatBody");
    const div = document.createElement("div");
    div.className = isUser ? "hl-msg hl-msg--user" : "hl-msg hl-msg--bot";
    div.textContent = message;
    chatBody.appendChild(div);
    chatBody.scrollTop = chatBody.scrollHeight;
}

connection.on("ReceiveMessage", (user, message) => {
    appendMessage(user === "Bạn", message);
});

connection.start().catch(err => console.error(err));

function sendMessage() {
    const input = document.getElementById("msgInput");
    const text = input.value.trim();
    if (!text) return;

    appendMessage(true, text);
    connection.invoke("SendMessageToBot", text).catch(err => console.error(err));
    input.value = "";
}

function toggleChat() {
    const popup = document.getElementById("chatPopup");
    popup.style.display = popup.style.display === "block" ? "none" : "block";
}

function handleEnter(e) {
    if (e.key === "Enter") sendMessage();
}
