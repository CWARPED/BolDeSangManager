# Déploiement Docker — BolDeSangManager

## Image Docker Hub

L'image officielle est disponible sur Docker Hub : **`cwarp/boldesangmanager:latest`**

Architectures incluses dans le manifest : `linux/amd64` et `linux/arm64`.

---

## Architectures supportées

| Image | Cible |
|---|---|
| `linux/amd64` | Serveur dédié, VPS, PC Linux classique |
| `linux/arm64` | Raspberry Pi 4/5, Freebox Ultra (via VM), Apple Silicon |

---

## Démarrage rapide

```bash
# 1. Adapter la configuration dans docker-compose.yml (admin email/password, URL externe)

# 2. Lancer
docker compose up -d

# 3. Accéder à l'app
http://localhost:8080
```

L'application crée automatiquement la base de données SQLite et le premier compte Commissaire au démarrage.

La base de données est persistée dans le volume Docker nommé `boldesang-data` (monté sur `/data` dans le conteneur).

---

## Variables d'environnement

| Variable | Défaut | Description |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | SQLite `/data/boldesang.db` | Chemin de la base SQLite |
| `DataProtection__KeysPath` | `/data/DataProtection-Keys` | Dossier des clés de chiffrement des cookies de session (voir avertissement ci-dessous) |
| `BolDeSang__AdminEmail` | `commissaire@boldesang.fr` | Email du premier Commissaire |
| `BolDeSang__AdminPassword` | `Commissaire123!` | Mot de passe (≥ 8 chars, ≥ 1 chiffre) |
| `BolDeSang__AdminPseudo` | `Grand Commissaire` | Pseudo affiché |
| `BolDeSang__UrlExterne` | *(vide)* | URL publique pour les QR codes dans les PDFs |
| `ASPNETCORE_URLS` | `http://+:8080` | Port d'écoute |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Environnement |

> **Important :** `BolDeSang__AdminEmail` / `Password` / `Pseudo` ne sont lus qu'au premier démarrage (quand le compte n'existe pas encore). Pour changer le mot de passe après installation, utilisez l'interface admin.

> **URL externe :** Renseigner `BolDeSang__UrlExterne` dans `docker-compose.yml` OU la configurer dans Admin > Paramètres après connexion. Les deux méthodes sont équivalentes.

> ⚠️ **Clés DataProtection :** elles chiffrent les cookies de session et les jetons anti-CSRF. Le `Dockerfile` les place par défaut dans `/data/DataProtection-Keys`, **à l'intérieur du volume persistant** — c'est ce qu'il faut. Si vous redéfinissez `DataProtection__KeysPath` vers un chemin hors volume, les clés sont régénérées à chaque redémarrage : tous les utilisateurs sont déconnectés et les formulaires ouverts échouent avec « votre session a expiré ».

---

## Mettre à jour l'application

```bash
docker compose pull          # télécharge la nouvelle image depuis Docker Hub
docker compose up -d         # redémarre avec la nouvelle image
# Les migrations EF Core s'appliquent automatiquement au démarrage
```

---

## Reverse proxy HTTPS (nginx exemple)

L'application écoute en HTTP sur le port 8080 à l'intérieur du conteneur. Pour HTTPS, placez un reverse proxy devant :

```nginx
server {
    listen 443 ssl;
    server_name boldesang.monasso.fr;

    ssl_certificate     /etc/ssl/certs/monasso.crt;
    ssl_certificate_key /etc/ssl/private/monasso.key;

    location / {
        proxy_pass         http://localhost:8080;
        proxy_set_header   Host $host;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        # WebSocket pour Blazor Server
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection "upgrade";
        proxy_read_timeout 3600s;
    }
}
```

---

## Freebox Ultra

### Prérequis matériel
- Freebox Ultra avec **SSD NVMe M.2 2280, M-key, PCIe 3** (sans dissipateur thermique)
- Le SSD se formate via FreeboxOS > Partage de fichiers > Disques

### Limites VM sur Freebox Ultra
- Architecture : **arm64 uniquement** (EFI requis)
- RAM max : **2 Go** pour toutes les VMs combinées
- CPU max : 2 vCores

Pour une association avec peu d'utilisateurs simultanés, c'est suffisant (Blazor Server consomme ~150–300 Mo).

### Installer Docker sur la VM Freebox

La Freebox Ultra ne supporte pas Docker nativement. Il faut créer une VM :

1. Dans FreeboxOS, créer une VM **Debian 12 (Bookworm)** depuis les images pré-installées Free
   - RAM : 1536 Mo recommandé (laisser ~500 Mo pour l'OS Freebox)
   - CPU : 2 vCores
   - Disque : sur le SSD NVMe (20 Go minimum)

2. Se connecter à la VM et installer Docker :

```bash
# Méthode officielle Docker pour Debian arm64
sudo apt-get update
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/debian/gpg \
  -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc

echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] \
  https://download.docker.com/linux/debian \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
sudo usermod -aG docker $USER  # éviter sudo à chaque commande
```

3. Déployer BolDeSangManager :

```bash
# Copier docker-compose.yml sur la VM
scp docker-compose.yml user@vm-freebox:~/
# Adapter la configuration
nano docker-compose.yml
# Lancer
docker compose up -d
```

4. Configurer la redirection de port dans FreeboxOS :
   - FreeboxOS > Paramètres de la Freebox > Gestion des ports
   - Rediriger le port 80/443 (ou 8080) vers l'IP de la VM

---

## Raspberry Pi 4/5

```bash
# Sur le Pi (Raspberry Pi OS 64-bit ou Ubuntu Server 24.04 arm64)
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER

# Déployer
scp docker-compose.yml pi@raspberrypi:~/
ssh pi@raspberrypi
docker compose up -d
```

Recommandé : Raspberry Pi 4 avec 4 Go de RAM minimum (2 Go fonctionne mais c'est juste).

---

## Construire votre propre image multi-arch

Pour publier votre propre image sur Docker Hub ou GitHub Container Registry :

```bash
# Créer et activer un builder multi-arch
docker buildx create --name multiarch --bootstrap --use

# Construire et pousser
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -t votre-compte/boldesangmanager:latest \
  --push .

# Vérifier les architectures publiées
docker buildx imagetools inspect votre-compte/boldesangmanager:latest
```

> **Note :** Le build utilise la cross-compilation native .NET (`-a $TARGETARCH`). Pas besoin d'émulation QEMU — le build reste rapide même sur un PC amd64.

L'image officielle `cwarp/boldesangmanager:latest` est construite avec cette même méthode.
