# **Creating the Discord Bot**

This page explains how to create the Discord bot used by **QuestMasterBot**, configure its permissions, enable required intents, and invite it to your server.

---

## 🧩 **1. Create the Discord Application**

1. Open the Discord Developer Portal:
   [https://discord.com/developers/applications](https://discord.com/developers/applications)
2. Click **New Application**.
3. Enter a name (e.g., `QuestMasterBot`).
4. Click **Create**.

This creates the application container for your bot.

---

## 🤖 **2. Add a Bot User**

1. Inside the application, open the **Bot** tab.
2. Click **Add Bot**.
3. Confirm with **Yes, do it!**

Your bot now has:

* its own username
* a token
* permissions you can configure

---

## 🔑 **3. Get the Bot Token**

1. Still in the **Bot** tab, find the **Token** section.
2. Click **Reset Token** (if required).
3. Click **Copy**.

> ⚠️ **Never share or commit your token.**
> If leaked, reset it immediately.

---

## 🔒 **4. Enable Required Gateway Intents**

QuestMasterBot needs certain Discord intents to work properly.

Go to **Bot → Privileged Gateway Intents** and enable:

* **MESSAGE CONTENT INTENT** ✔ (Required for reading slash command data)
* **SERVER MEMBERS INTENT** (Optional but recommended)
* **PRESENCE INTENT** (Optional)

Then click **Save Changes**.

---

## 🔗 **5. Generate the Invite Link**

Open:

**OAuth2 → URL Generator**

### Under **SCOPES**, enable:

* `bot`
* `applications.commands`

### Under **BOT PERMISSIONS**, enable:

At minimum:

* `Send Messages`
* `Embed Links`
* `Use Slash Commands`

(Optional depending on features you add later):

* `Read Message History`
* `Manage Messages`

### Copy the generated URL

Paste it into your browser and select the server where you want the bot installed.

Click **Authorize**.

---

## ✔️ **6. Verify Installation**

After inviting the bot, it should appear in your server’s member list.

It will remain **offline** until your .NET application is running.

Once you run the bot, it should show **online**, ready to respond to commands.
