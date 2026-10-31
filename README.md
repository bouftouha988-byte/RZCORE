# RzCore (site + serveur + scanner)

## Dossiers
- `WEBSITE/`  site (index.html = landing + login, dashboard.html = dashboard) au design RzCore-red
- `server/`   serveur Node 18+ (sans npm install) : Discord OAuth, PIN, rapports
- `client/`   scanner RzCore.exe (RzCore.cs, build.bat)

## 1. Discord
discord.com/developers > OAuth2 > redirect `http://localhost:3000/auth/discord/callback` (ton https en prod).

## 2. Serveur
    cd server
    set DISCORD_CLIENT_ID=...
    set DISCORD_CLIENT_SECRET=...
    set SESSION_SECRET=texte-long-aleatoire
    set BASE_URL=http://localhost:3000
    node server.js

## 3. Scanner
1. Mets ton URL https publique dans `SERVER` en haut de `client/RzCore.cs`.
2. Lance `client/build.bat` (Windows). Il crée RzCore.exe et le copie dans `server/public/`.
(Le RzCore.exe fourni est compilé avec SERVER=http://localhost:3000 : pour tests locaux seulement.)

## Ce que le scanner lit (affiché au joueur, il doit accepter)
Noms de fichiers de tous les disques (jamais le contenu), Corbeille, journal de suppression Windows (USN),
processus, Recent, Prefetch, traces d'exécution (registre), historique Chrome/Edge/Brave/Opera/Firefox, cache DNS,
logs Windows (journaux effacés, drivers installés depuis un dossier utilisateur, alertes et arrêt de Defender),
réglages de sécurité (exclusions Defender, test-signing, blocklist de drivers, drivers chargés).
Seules les correspondances suspectes sont envoyées, automatiquement à la fin du scan, au dashboard.
Le joueur ne voit pas la liste dans l'exe : il voit seulement "Scan complete / Report sent".

## Flux
Dashboard > New scan > PIN > le joueur lance RzCore.exe (admin), entre le PIN, accepte > scan > rapport envoyé et visible dans le dashboard.
Listes de mots-clés : tableaux `R` (fichiers) et `WB` (web/DNS) en haut de RzCore.cs.

## Plans, rôles et accès (nouveau)
- Un seul plan donne accès à **tous les jeux** (FiveM, RedM, CS:GO, Roblox, DayZ, Rust, RageMP).
- Prix affichés sur le site : cherche `PRICES` dans `WEBSITE/index.html` (1 mois 9,99 €, 2 mois 16,99 €, 1 an 49,99 €, lifetime 99,99 €). Ce sont des valeurs par défaut, change-les.
- Pas de paiement automatique : le client paie, envoie son ID Discord (visible sur sa page Account), et l'owner active son plan dans le dashboard > **Members**.
- Rôles : `owner` (variable `OWNER_ID`), `founder` (mêmes droits que l'owner, sans expiration), `partner`, `user`. Durées : 1 mois, 2 mois, 1 an, lifetime.
- L'owner peut retirer un founder. Un founder **ne peut pas** retirer l'owner (mets `FOUNDER_CAN_REMOVE_OWNER=1` si tu veux que ce soit permis).
- Sans plan actif, un compte ne peut pas créer de PIN. `REQUIRE_PLAN=0` désactive cette règle.
- Un ID Discord peut être ajouté avant que la personne se connecte : son rôle l'attend.

## Score de confiance du joueur
Dans un rapport : tu saisis l'ID Discord du joueur, puis **Mark as cheater**. Son score part de 100 % et baisse de 5 % à chaque rapport marqué (un seul clic par rapport). Rien ne baisse automatiquement.

## PC timeline
Chaque rapport affiche la date d'installation de Windows (alerte si moins de 14 jours), les derniers démarrages du PC et les programmes désinstallés.

## Détection ajoutée
CS:GO / CS2 (Aimware, Neverlose, Gamesense...), exécuteurs Roblox (Synapse X, Fluxus, JJSploit...), signatures de cheats dans les exécutables des dossiers utilisateur (même renommés), mémoire du navigateur et du service DNS, fichiers aux noms suspects (à vérifier à la main), pilotes .sys dans un dossier utilisateur.
Rien n'a été testé sur un vrai Windows : lance un scan et dis ce qui sort.
