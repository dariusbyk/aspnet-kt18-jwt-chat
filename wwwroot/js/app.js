let token = sessionStorage.getItem("chatToken");
let connection = null;
const account = document.getElementById("account");
const chat = document.getElementById("chat");
const errorText = document.getElementById("error");
const statusText = document.getElementById("status");
const messages = document.getElementById("messages");
const sendButton = document.getElementById("sendButton");
const messageInput = document.getElementById("message");

async function authorize(action, nameId, passwordId) {
    errorText.textContent = "";
    try {
        const response = await fetch("/api/auth/" + action, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                username: document.getElementById(nameId).value,
                password: document.getElementById(passwordId).value
            })
        });
        const data = await response.json();
        if (!response.ok) {
            if (data.message) {
                errorText.textContent = data.message;
            } else if (data.errors) {
                errorText.textContent = Object.values(data.errors).flat().join(" ");
            } else {
                errorText.textContent = "Не удалось выполнить запрос.";
            }
            return;
        }
        token = data.token;
        sessionStorage.setItem("chatToken", token);
        document.getElementById("loginForm").reset();
        document.getElementById("registerForm").reset();
        await openChat();
    } catch (error) {
        errorText.textContent = "Не удалось связаться с сервером.";
    }
}

document.getElementById("loginForm").addEventListener("submit", async function (event) {
    event.preventDefault();
    await authorize("login", "loginName", "loginPassword");
});
document.getElementById("registerForm").addEventListener("submit", async function (event) {
    event.preventDefault();
    await authorize("register", "registerName", "registerPassword");
});

async function logout() {
    token = null;
    sessionStorage.removeItem("chatToken");
    if (connection !== null) {
        await connection.stop();
        connection = null;
    }
    account.hidden = false;
    chat.hidden = true;
    messages.textContent = "";
    messageInput.value = "";
    sendButton.disabled = true;
}
document.getElementById("logout").addEventListener("click", logout);

async function openChat() {
    const response = await fetch("/api/auth/me", {
        headers: { "Authorization": "Bearer " + token }
    });
    if (!response.ok) {
        await logout();
        errorText.textContent = "Войдите снова: токен недействителен или истёк.";
        return;
    }
    const user = await response.json();
    document.getElementById("username").textContent = user.username;
    account.hidden = true;
    chat.hidden = false;
    statusText.textContent = "Подключение...";
    if (connection !== null) {
        await connection.stop();
    }
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/chathub", { accessTokenFactory: function () { return token; } })
        .withAutomaticReconnect()
        .build();
    connection.on("ReceiveMessage", function (username, message) {
        const item = document.createElement("li");
        item.textContent = username + ": " + message;
        messages.appendChild(item);
    });
    connection.onreconnecting(function () {
        sendButton.disabled = true;
        statusText.textContent = "Восстановление соединения...";
    });
    connection.onreconnected(function () {
        sendButton.disabled = false;
        statusText.textContent = "Подключено";
    });
    connection.onclose(function () {
        sendButton.disabled = true;
        statusText.textContent = "Соединение закрыто. Обновите страницу или войдите снова.";
    });
    try {
        await connection.start();
        sendButton.disabled = false;
        statusText.textContent = "Подключено";
    } catch (error) {
        statusText.textContent = "Не удалось подключиться. Выйдите и войдите снова.";
    }
}

document.getElementById("chatForm").addEventListener("submit", async function (event) {
    event.preventDefault();
    const message = messageInput.value.trim();
    if (message === "" || connection === null) {
        return;
    }
    sendButton.disabled = true;
    try {
        await connection.invoke("SendMessage", message);
        messageInput.value = "";
        statusText.textContent = "Подключено";
    } catch (error) {
        statusText.textContent = "Не удалось отправить сообщение.";
    }
    sendButton.disabled = connection === null || connection.state !== signalR.HubConnectionState.Connected;
});

async function start() {
    if (token) {
        try {
            await openChat();
        } catch (error) {
            errorText.textContent = "Не удалось связаться с сервером. Обновите страницу.";
        }
    }
}
start();
