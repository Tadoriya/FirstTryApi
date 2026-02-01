# 🎮 Incremental Clicker – Backend ASP.NET Core

Backend d’un jeu incrémental multijoueur développé en **ASP.NET Core**, intégrant une **API REST**, de la **persistance serveur**, des **mécaniques de progression** et des **fonctionnalités temps réel via SignalR**.

Le projet met l’accent sur :

- une logique de jeu robuste côté serveur  
- une architecture claire et modulaire  
- des tests unitaires avec mesure de couverture  

---

## 📚 Table des matières

- Aperçu  
- Fonctionnalités  
- Architecture  
- Démarrage rapide  
- Configuration  
- Authentification  
- API & SignalR  
- Gameplay et progression  
- Base de données  
- Background services  
- Tests & couverture  
- Structure du projet  

---

## 🔍 Aperçu

Ce backend alimente un jeu incrémental dans lequel les joueurs :

- gagnent des points par clic  
- achètent des objets améliorant la production  
- bénéficient d’un revenu passif  
- battent des records visibles en temps réel  
- interagissent via un chat global  

Toutes les règles critiques sont **gérées côté serveur** afin de garantir la **cohérence**, la **sécurité** et l’équité entre les joueurs.

---

## ✨ Fonctionnalités principales

- 🔐 Authentification JWT  
- 🖱️ Progression par clic (avec rate limiting)  
- ♻️ Système de reset avec multiplicateurs  
- 🏆 Notifications de records en temps réel  
- 🛒 Inventaire avec limites de quantité  
- 💰 Revenu passif via background service  
- 💬 Chat global via SignalR  
- 👥 Compteur de joueurs connectés  
- 📡 Événements temps réel :
  - `ScoreUpdate`
  - `NewHighScore`
  - `PlayerReset`
- 🧪 Tests unitaires + couverture de code  

---

## 🏗️ Architecture

Architecture en couches avec responsabilités bien séparées :

- **Controllers** : exposition de l’API REST  
- **Services** : logique métier (Game, Inventory, User, PassiveIncome)  
- **Models** : entités et DTO  
- **SignalR Hubs** : communication temps réel  
- **Persistence** : EF Core + SQLite  
- **Background Services** : revenu passif  

SignalR est utilisé pour :

- le chat global  
- les notifications système  
- la synchronisation des scores en temps réel  

---

## 🚀 Démarrage rapide

### Prérequis

- .NET SDK **8.0+**
- CLI `dotnet`

### Build

```bash
dotnet restore
dotnet build
Lancer l’API
dotnet run --project FirstTryApi
API disponible sur :
👉 http://localhost:5000

⚙️ Configuration
Points importants :

🔑 JWT configuré dans :

Services/JwtService.cs

Program.cs

🌐 CORS autorisé pour :

http://localhost:*

https://csharp.nouvet.fr

🗄️ Base de données SQLite

fichier local géré par EF Core

📦 Seed des items depuis :

https://csharp.nouvet.fr/front10/items.json

🔐 Authentification
Endpoints
POST /api/User/Register

POST /api/User/Login

Utilisation
Récupérer le token JWT

Ajouter le header :

Authorization: Bearer <token>
Accéder aux routes protégées

📡 API & SignalR
API REST
/api/Game/* : progression, clics, reset, scores

/api/Inventory/* : items, achats

/api/User/* : utilisateurs

SignalR – ChatHub
Événements envoyés :

ReceiveMessage(user, message)

UpdateUserCount(count)

NewHighScore(username, score)

PlayerReset(username, score)

ScoreUpdate(score) (envoyé uniquement au joueur concerné)

Connexion :

/hub/chat
🎯 Gameplay et progression
Chaque clic augmente le score selon :

le multiplicateur

la valeur totale des objets

Les objets ont :

un prix

une quantité maximale

un bonus de production

Le reset :

consomme des points

augmente le multiplicateur

déclenche un événement SignalR

Les records sont :

mis en cache

notifiés uniquement lorsqu’un seuil est franchi

🗄️ Base de données
EF Core (Code First)

Entités principales :

User

Progression

Item

InventoryEntry

Migrations EF Core incluses

Seed automatique des items

🔄 Background services
PassiveIncomeService
Exécuté périodiquement

Ajoute +1 point à chaque joueur

Envoie ScoreUpdate uniquement aux joueurs connectés

Gère l’overflow (int.MaxValue)

🧪 Tests & couverture
Lancer les tests
dotnet test
Générer la couverture
dotnet test --collect:"XPlat Code Coverage"
Générer le rapport HTML
reportgenerator \
  -reports:TestResults/**/coverage.cobertura.xml \
  -targetdir:coveragereport \
  -reporttypes:Html
Rapport disponible ici :

coveragereport/index.html
Objectifs atteints :

GameService ≈ 100%

Tests sur services critiques

Validation des règles métier

📁 Structure du projet
Incremental_Clicker/
├── FirstTryApi/
│   ├── Controllers/
│   ├── Services/
│   ├── Hubs/
│   ├── Models/
│   ├── Middlewares/
│   └── Program.cs
├── FirstTryApi.Tests/
│   ├── GameServiceTests.cs
│   ├── PassiveIncomeServiceTests.cs
│   ├── JwtServiceTests.cs
│   └── …
├── coveragereport/
├── IncrementalGame.sln
└── README.md