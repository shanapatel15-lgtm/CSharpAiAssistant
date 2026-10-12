(() => {
    const form = document.getElementById("chat-form");
    const input = document.getElementById("message-input");
    const sendButton = document.getElementById("send-button");
    const chat = document.getElementById("chat");
    const welcome = document.getElementById("welcome");
    const modeLabel = document.getElementById("mode-label");
    const newChat = document.getElementById("new-chat");
    const documentInput = document.getElementById("document-file");
    const documentList = document.getElementById("document-list");
    const documentCount = document.getElementById("document-count");
    const documentStatus = document.getElementById("document-status");
    const history = [];
    let conversationId = null;

    function addMessage(role, content) {
        const row = document.createElement("div");
        row.className = `message ${role}`;
        const avatar = document.createElement("div");
        avatar.className = "avatar";
        avatar.textContent = role === "user" ? "●" : "✦";
        const body = document.createElement("div");
        body.className = "message-body";
        const meta = document.createElement("div");
        meta.className = "message-meta";
        meta.textContent = role === "user" ? "You" : "Orbit AI";
        const bubble = document.createElement("div");
        bubble.className = "bubble";
        bubble.textContent = content;
        body.append(meta, bubble);
        row.append(avatar, body);
        chat.append(row);
        row.scrollIntoView({ behavior: "smooth", block: "end" });
        return bubble;
    }

    async function sendMessage(text) {
        const message = text.trim();
        if (!message || sendButton.disabled) return;
        welcome.hidden = true;
        addMessage("user", message);
        history.push({ role: "user", content: message });
        input.value = "";
        input.style.height = "auto";
        sendButton.disabled = true;
        const typing = document.createElement("div");
        typing.className = "typing";
        typing.textContent = "Orbit AI is thinking…";
        chat.append(typing);

        try {
            const response = await fetch("/api/chat", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ message, history: history.slice(0, -1).slice(-12), conversationId })
            });
            const data = await response.json();
            typing.remove();
            if (!response.ok) {
                const detail = data.detail || data.title || "The request could not be completed.";
                addMessage("assistant", `Sorry, I couldn't complete that request. ${detail}`);
                return;
            }
            conversationId = data.conversationId || conversationId;
            addMessage("assistant", data.reply);
            history.push({ role: "assistant", content: data.reply });
            modeLabel.textContent = data.toolUsed ? "C# tool executed" : data.demoMode ? (data.knowledgeBaseUsed ? "Knowledge preview" : "Demo mode") : data.knowledgeBaseUsed ? "AI + knowledge base" : "AI connected";
        } catch {
            typing.remove();
            addMessage("assistant", "I couldn't reach the application server. Check that the project is running, then try again.");
        } finally {
            sendButton.disabled = false;
            input.focus();
        }
    }

    async function loadDocuments() {
        try {
            const response = await fetch("/api/knowledge");
            if (!response.ok) throw new Error("Could not load documents");
            const documents = await response.json();
            documentList.replaceChildren();
            documentCount.textContent = String(documents.length);
            if (documents.length === 0) { documentStatus.textContent = "No documents added yet."; return; }
            documentStatus.textContent = "Documents are searchable by the assistant.";
            documents.forEach(doc => {
                const item = document.createElement("li");
                const name = document.createElement("span");
                name.textContent = doc.fileName;
                name.title = `${doc.fileName} (${Math.round(doc.sizeBytes / 1024)} KB)`;
                const remove = document.createElement("button");
                remove.type = "button"; remove.className = "remove-document"; remove.textContent = "×";
                remove.setAttribute("aria-label", `Delete ${doc.fileName}`);
                remove.addEventListener("click", async () => {
                    if (!confirm(`Delete ${doc.fileName} from the knowledge base?`)) return;
                    const deleted = await fetch(`/api/knowledge/${encodeURIComponent(doc.fileName)}`, { method: "DELETE" });
                    if (deleted.ok) loadDocuments();
                    else documentStatus.textContent = "Could not delete that document.";
                });
                item.append(name, remove); documentList.append(item);
            });
        } catch { documentStatus.textContent = "Could not load the knowledge base. Is the app running?"; }
    }

    documentInput.addEventListener("change", async () => {
        const file = documentInput.files?.[0];
        if (!file) return;
        documentStatus.textContent = `Uploading ${file.name}…`;
        const data = new FormData(); data.append("file", file);
        try {
            const response = await fetch("/api/knowledge/upload", { method: "POST", body: data });
            const result = await response.json();
            if (!response.ok) throw new Error(result.error || "Upload failed.");
            documentStatus.textContent = `${result.fileName} added to the knowledge base.`;
            await loadDocuments();
        } catch (error) { documentStatus.textContent = error.message || "Upload failed."; }
        finally { documentInput.value = ""; }
    });

    form.addEventListener("submit", event => {
        event.preventDefault();
        sendMessage(input.value);
    });

    input.addEventListener("keydown", event => {
        if (event.key === "Enter" && !event.shiftKey) {
            event.preventDefault();
            form.requestSubmit();
        }
    });

    input.addEventListener("input", () => {
        input.style.height = "auto";
        input.style.height = `${Math.min(input.scrollHeight, 150)}px`;
    });

    document.querySelectorAll(".suggestion").forEach(button => {
        button.addEventListener("click", () => sendMessage(button.dataset.prompt || ""));
    });

    newChat.addEventListener("click", () => {
        chat.replaceChildren();
        history.length = 0;
        conversationId = null;
        welcome.hidden = false;
        modeLabel.textContent = "Ready";
        input.value = "";
        input.focus();
    });

    loadDocuments();
})();
