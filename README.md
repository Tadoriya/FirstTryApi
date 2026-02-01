# Incremental Clicker – Backend ASP.NET Core

Backend d’un jeu incrémental multijoueur développé en ASP.NET Core.  
Le projet gère toute la logique de jeu côté serveur : progression, scores, records et
communication temps réel.

---

## Présentation générale

Ce backend alimente un jeu incrémental dans lequel les joueurs :

- gagnent des points en cliquant
- améliorent leur progression grâce à des multiplicateurs
- réinitialisent leur progression pour devenir plus puissants
- battent des records visibles par tous
- interagissent via un chat en temps réel
- achat des items qui augmentent le ClickValue

L’ensemble des règles du jeu est contrôlé côté serveur afin d’assurer
la cohérence, la sécurité et l’équité entre les joueurs.

---

## Fonctionnalités principales

- Authentification sécurisée par **JWT**
- API REST pour les actions de jeu (clics, progression, reset)
- Gestion de la progression et des multiplicateurs
- Notifications de records en temps réel
- Chat global avec **SignalR**
- Compteur de joueurs connectés
- Achat des items
- Revenu passif exécuté côté serveur
- Événements temps réel ciblés :
  - mise à jour du score
  - nouveau record
  - reset de progression

---

## Temps réel avec SignalR

SignalR est utilisé pour :

- le chat entre joueurs
- les messages système
- l’affichage des nouveaux records
- la synchronisation du score en direct
- la notification des resets

Chaque joueur reçoit uniquement les événements qui le concernent
lorsque cela est nécessaire.

---

## Sécurité et authentification

- Authentification basée sur des **tokens JWT**
- Routes protégées côté serveur
- Identification des joueurs via les claims du token
- Protection contre l’abus des clics

---

## Persistance et logique serveur

- Données persistées côté serveur
- Progression stockée en base de données
- Calculs sensibles effectués uniquement côté backend
- Cache serveur pour la gestion des records

---

## Tests

- Tests unitaires sur les services critiques
- Validation complète de la logique de jeu
- Mesure de couverture de code
- Objectif : fiabilité et robustesse du backend

---

## Structure du projet

- Controllers : API REST
- Services : logique métier du jeu
- Models : entités et DTO
- SignalR Hubs : communication temps réel
- Background services : revenu passif
- Tests unitaires dédiés

---

## Contributeur

**Taha AIT AHMED OUAAL(F2)**
