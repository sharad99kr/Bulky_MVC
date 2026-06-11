(() => {
    "use strict";

    const STORAGE_KEY = "readify_conversation_id";

    //------State------
    let conversationId = localStorage.getItem(STORAGE_KEY) ?? null;
    let busy = false;

    //------DOM refs------
    const widget = document.getElementById("chat-widget");
    if (!widget)
        return; //guard - widget not on this page

    const toggle = document.getElementById("chat-toggle");
    const newBtn = document.getElementById("chat-new");
    const body = document.getElementById("chat-body");
    const messages = document.getElementById("chat-messages");
    const input = document.getElementById("chat-input");
    const sendBtn = document.getElementById("chat-send");
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";
    console.log("CSRF token:", token);

    if (!toggle || !newBtn || !body || !messages || !input || !sendBtn) {
        console.error("Chat widget: required elements are missing.");
        return;
    }

    //------On load: rehydrate prior conversation------
    loadHistory();

    //------Toggle open/close------
    toggle.addEventListener("click", () => {
        const open = body.style.display !== "none";
        body.style.display = open ? "none" : "flex";
        toggle.textContent = open ? "▸" : "▾";
    });

    //------New chat ------
    newBtn.addEventListener("click", () => {
        conversationId = null;
        localStorage.removeItem(STORAGE_KEY);
        messages.innerHTML = "";
        appendMessage("assistant", "Hello! I can help you find books or check your order status. " +
            "What are you looking for?");
    });

    //------Send on Enter ------
    input.addEventListener("keydown", event => {
        if (event.key === "Enter" && !event.shiftKey) {
            event.preventDefault();
            send();
        }
    })
    sendBtn.addEventListener("click", send);

    //------ Core send function  ------
    async function send() {
        const text = input.value.trim();
        if (!text || busy)
            return;

        busy = true;
        input.value = "";
        sendBtn.disabled = true;

        appendMessage("user", text);
        const thinking = appendMessage("assistant", "...");

        try {

            const res = await fetch("/Chat/Send", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token
                },
                body: JSON.stringify({ message: text, conversationId })
            });

            if (res.redirected || res.url.includes("/Account/Login")) {
                thinking.querySelector("p").textContent = "Please log in to use chat";
                return;
            }
        

            if (!res.ok) {
                throw new Error(`HTTP ${ res.status }`);
            }

            const data = await res.json();

            //Store the conversationId returned by the server
            conversationId = data.conversationId;

            localStorage.setItem(STORAGE_KEY, conversationId);
            thinking.querySelector("p").textContent = data.message;

            if (data.warningMessage) {
                const warn = document.createElement("p");
                warn.className = "chat-warning";
                warn.textContent = "⚠" + data.warningMessage;
                thinking.appendChild(warn);
            }

            if (data.fallbackUsed) {
                thinking.querySelector("p").classList.add("chat-fallback");
            }

        } catch (err) {

            thinking.querySelector("p").textContent =
                "Something went wrong. Please try again.";
            console.error("[Chat]", err);

        } finally {

            busy = false;
            sendBtn.disabled = false;
            input.focus();
            scrollToBottom();

        }
    }

    //------ Helpers  ------
    function appendMessage(role, text) {
        const div = document.createElement("div");
        div.className = `chat-message ${role}`;
        const p = document.createElement("p");
        p.textContent = text;
        div.appendChild(p);
        messages.appendChild(div);
        scrollToBottom();
        return div;
    }

    //------ Helpers  ------
    function scrollToBottom() {
        messages.scrollTop = messages.scrollHeight;
    }

    //------ Load prior history on page open ------
    async function loadHistory() {

        if (!conversationId) {
            appendMessage("assistant",
                "Hello! I can help you find books or check your order status. " +
                "What are you looking for?");
            return;
        }

        try {

            const res = await fetch(
                `/Chat/History?conversationId=${conversationId}`);

            if (!res.ok) {
                startFresh();
                return;
            }

            const turns = await res.json();
            if (!turns.length) {
                startFresh();
                return;
            }

            turns.forEach(t => appendMessage(t.role, t.content));

        } catch {
            startFresh();
        }
    }

    function startFresh() {
        conversationId = null;
        localStorage.removeItem(STORAGE_KEY);
        appendMessage("assistant",
            "Hello! I can help you find books or check your order status. " +
            "What are you looking for?");
    }


})();