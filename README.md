# Incremental Clicker – Backend ASP.NET Core

Backend d’un jeu incrémental multijoueur développé en ASP.NET Core.
Il expose une API REST, gère la persistance serveur, la progression des joueurs
et les fonctionnalités temps réel via SignalR.

---

## Aperçu

Ce projet fournit le backend d’un jeu incrémental où les joueurs :

- gagnent des points par clic
- achètent des objets améliorant la production
- bénéficient d’un revenu passif
- battent des records visibles en temps réel
- communiquent via un chat global

Toute la logique critique est gérée côté serveur afin de garantir
cohérence, sécurité et équité.

---

## Fonctionnalités

- Authentification JWT
- Progression par clic avec rate limiting
- Système de reset avec multiplicateurs
- Records globaux en temps réel
- Inventaire avec limites de quantité
- Revenu passif via background service
- Chat global SignalR
- Compteur de joueurs connectés
- Événements temps réel :
  - ScoreUpdate
  - NewHighScore
  - PlayerReset

---

## Architecture

Architecture en couches avec responsabilités bien séparées :

- Controllers : API REST
- Services : logique métier (Game, Inventory, User, PassiveIncome)
- Models : entités et DTO
- SignalR Hubs : communication temps réel
- Persistence : EF Core + SQLite
- Background Services : revenu passif

SignalR est utilisé pour :
- le chat
- les notifications système
- la synchronisation des scores en temps réel

---

## Démarrage rapide

### Prérequis

- .NET SDK 8.0 ou supérieur
- CLI dotnet

### Build

dotnet restore  
dotnet build  

### Lancer l’API

dotnet run --project FirstTryApi  

API accessible sur :  
http://localhost:5000

---

## Configuration

Points importants :

- JWT configuré dans :
  - Services/JwtService.cs
  - Program.cs

- CORS autorisé pour :
  - http://localhost:*
  - https://csharp.nouvet.fr

- Base de données SQLite gérée par EF Core

- Seed des items depuis :
  - https://csharp.nouvet.fr/front10/items.json

---

## Authentification

Endpoints disponibles :

- POST /api/User/Register
- POST /api/User/Login

Utilisation :

1. Récupérer le token JWT
2. Ajouter le header :

Authorization: Bearer <token>

3. Accéder aux routes protégées

---

## API & SignalR

### API REST

- /api/Game/* : progression, clics, reset, score global
- /api/Inventory/* : items, achats
- /api/User/* : utilisateurs

### SignalR – ChatHub

Événements envoyés :

- ReceiveMessage(user, message)
- UpdateUserCount(count)
- NewHighScore(username, score)
- PlayerReset(username, score)
- ScoreUpdate(score) (envoyé uniquement au joueur concerné)

Connexion SignalR :

/hub/chat

---

## Gameplay et progression

- Chaque clic augmente le score selon :
  - le multiplicateur
  - la valeur totale des objets

- Les objets ont :
  - un prix
  - une quantité maximale
  - un bonus de production

- Le reset :
  - consomme des points
  - augmente le multiplicateur
  - déclenche un événement SignalR

- Les records sont :
  - mis en cache côté serveur
  - notifiés uniquement lorsqu’un seuil est franchi

---

## Base de données

- EF Core (Code First)
- Entités principales :
  - User
  - Progression
  - Item
  - InventoryEntry

- Migrations EF Core incluses
- Seed automatique des items

---

## Background service

### PassiveIncomeService

- Exécuté périodiquement
- Ajoute +1 point à chaque joueur
- Envoie ScoreUpdate uniquement aux joueurs connectés
- Gère les dépassements (int.MaxValue)

---

## Tests & couverture

### Lancer les tests

dotnet test

### Générer la couverture

dotnet test --collect:"XPlat Code Coverage"

### Générer le rapport HTML

reportgenerator -reports:TestResults/**/coverage.cobertura.xml -targetdir:coveragereport -reporttypes:Html

Rapport disponible dans :

coveragereport/index.html

Objectifs atteints :

- GameService proche de 100 %
- Tests sur services critiques
- Validation complète des règles métier

---

## Structure du projet

Incremental_Clicker  
├── FirstTryApi  
│   ├── Controllers  
│   ├── Services  
│   ├── Hubs  
│   ├── Models  
│   ├── Middlewares  
│   └── Program.cs  
├── FirstTryApi.Tests  
│   ├── GameServiceTests.cs  
│   ├── PassiveIncomeServiceTests.cs  
│   ├── JwtServiceTests.cs  
│   └── ...  
├── coveragereport  
├── IncrementalGame.sln  
└── README.md  

---

## Contributeur

Taha AIT AHMED OUAAL(F2)
