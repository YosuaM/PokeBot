# VPS + Docker Deployment Guide (QuestMasterBot / PokeBot)

This document explains how to deploy the Discord bot on a VPS using Docker.  
Goal: have the bot running 24/7 with persistent data (SQLite/files) and easy updates.

---

## 0. What you need

- A VPS (Ubuntu 22.04 LTS recommended).
- Public IP + SSH access (password or private key).
- Your bot repo on GitHub/GitLab.
- Discord bot token.

---

## 1. Connect to the VPS from macOS

### 1.1 Basic SSH (password)
```bash
ssh <user>@<VPS_IP>
```
Example:
```bash
ssh root@<VPS_IP_EXAMPLE>
```

### 1.2 SSH with private key (recommended)
1) Move key to `~/.ssh` and lock permissions:
```bash
mkdir -p ~/.ssh
mv ~/Downloads/my-vps-key.pem ~/.ssh/
chmod 400 ~/.ssh/my-vps-key.pem
```

2) Connect:
```bash
ssh -i ~/.ssh/my-vps-key.pem <user>@<VPS_IP>
```

### 1.3 Optional alias (`~/.ssh/config`)
```sshconfig
Host pokebot
  HostName <VPS_IP_EXAMPLE>
  User ubuntu
  IdentityFile ~/.ssh/my-vps-key.pem
  Port 22
```

Then:
```bash
ssh pokebot
```

---

## 2. First-time server setup (Ubuntu)

Run in the VPS:

```bash
sudo apt update && sudo apt upgrade -y
sudo apt install -y git ufw fail2ban

# Firewall
sudo ufw allow OpenSSH
sudo ufw enable

# Fail2ban
sudo systemctl enable --now fail2ban
```

---

## 3. Install Docker

```bash
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
newgrp docker
docker --version
docker info
```

If `docker info` shows daemon details, Docker is ready.

---

## 4. Clone the repo

```bash
cd ~
git clone <YOUR_REPO_URL>
cd <REPO_FOLDER>
```

Check structure:
```bash
ls
```

---

## 5. Create `.env` file (token)

In the repo root:

```bash
nano .env
```

Content:
```env
DISCORD_TOKEN=REAL_TOKEN_HERE
```

> **Never commit `.env`**. Add it to `.gitignore`.

---

## 6. Dockerfile (build + run .NET bot)

Put a `Dockerfile` in the repo root.

### Example for .NET 8 project in a subfolder
Adjust project path/name if needed.

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj first for faster restores
COPY ./Back/PokeBotDiscord/PokeBotDiscord/PokeBotDiscord.csproj ./Back/PokeBotDiscord/PokeBotDiscord/
RUN dotnet restore ./Back/PokeBotDiscord/PokeBotDiscord/PokeBotDiscord.csproj

# Copy the rest
COPY . .
RUN dotnet publish ./Back/PokeBotDiscord/PokeBotDiscord/PokeBotDiscord.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

COPY --from=build /app/publish .
ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "PokeBotDiscord.dll"]
```

Notes:
- If your project targets `net9.0`, either:
  - change csproj to `net8.0` (recommended for LTS), **or**
  - use `mcr.microsoft.com/dotnet/sdk:9.0` and `runtime:9.0`.

---

## 7. docker-compose.yml

Create `docker-compose.yml` in the repo root:

```yaml
services:
  pokebot:
    build: .
    container_name: pokebot
    restart: unless-stopped
    environment:
      - DISCORD_TOKEN=${DISCORD_TOKEN}
    volumes:
      # Persistent data (SQLite/files)
      - ./data:/app/data
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```

Create the data folder:
```bash
mkdir -p data
```

---

## 8. Build and run

```bash
docker compose up -d --build
docker ps
docker logs -f pokebot
```

You should see logs similar to:
- `Gateway: Connected`
- `Discord client is ready`
- `Slash commands registered globally`

---

## 9. Update the bot after pushing changes

Whenever you push new code to the repo:

```bash
cd <REPO_FOLDER>
git pull
docker compose up -d --build
```

---

## 10. Useful commands

Stop / start:
```bash
docker compose stop
docker compose start
```

Restart:
```bash
docker compose restart
```

Remove containers (keeps data):
```bash
docker compose down
```

Remove everything (also removes volumes):
```bash
docker compose down -v
```

Check container status:
```bash
docker ps -a
```

---

## 11. Backups (SQLite/files)

Because data lives in `./data`, you can back it up by copying or zipping.

Manual backup:
```bash
tar -czf backup_$(date +%F).tar.gz data
```

Daily cron backup example (3 AM):
```bash
mkdir -p backups
crontab -e
```

Add:
```cron
0 3 * * * cd /home/<user>/<REPO_FOLDER> && tar -czf backups/backup_$(date +\%F).tar.gz data
```

---

## 12. Troubleshooting

### 12.1 `no configuration file provided`
You are not in a folder with `docker-compose.yml`.
```bash
ls
```
Go to the correct folder or create `docker-compose.yml`.

### 12.2 `Cannot connect to the Docker daemon`
Docker not installed or the daemon is down.
```bash
docker info
sudo systemctl status docker
sudo systemctl start docker
```

### 12.3 `.NET SDK does not support targeting net9.0`
Your csproj targets net9.0 but Docker uses SDK 8.0.
Fix by:
- downgrading TargetFramework to net8.0, or
- changing Docker images to sdk/runtime 9.0.

### 12.4 Token missing
If you see:
`DISCORD_TOKEN variable is not set`
Create/verify `.env` in the same folder as compose:
```bash
cat .env
```

---

## 13. Security notes

- Keep the token out of git.
- Use a firewall (UFW).
- Consider disabling root SSH and using a normal user.
- Optional: change SSH port and/or enforce key-only login.

---

If you modify directory paths or project names, remember to update:
- Dockerfile `COPY` + `dotnet restore/publish` paths
- `ENTRYPOINT` dll name
