# Incremental Clicker – Backend ASP.NET Core

Backend d’un jeu incrémental multijoueur développé en **ASP.NET Core**.  
Ce projet se concentre sur la **logique serveur**, la **sécurité**, la **gestion de l'état du jeu** et la **synchronisation temps réel** avec les joueurs.


---

## Présentation générale

Le jeu repose sur un principe simple :  
les joueurs cliquent pour gagner des points, améliorent leur progression, effectuent des resets pour devenir plus puissants et tentent de battre le **meilleur score global**.

Toutes les règles critiques sont implémentées **côté backend**, afin de garantir la cohérence du jeu et éviter toute triche.

---

## Fonctionnalités principales

### Authentification
- Authentification sécurisée via **JWT**
- Inscription et connexion des utilisateurs
- Routes protégées accessibles uniquement avec un token valide

### Gameplay
- Progression incrémentale par clic
- Calcul du score basé sur :
  - multiplicateur
  - valeur cumulée des objets
- Système de reset :
  - coût dynamique
  - remise à zéro du score
  - augmentation du multiplicateur
- Achat des items
- Gestion de l'overflow (`int.MaxValue`)

### Scores
- **BestScore individuel** enregistré pour chaque joueur
- **BestScore global** (meilleur score parmi tous les joueurs) que j'ai pas reussi a afficher
- Mise à jour du BestScore lors :
  - des clics
  - des resets
- Cache serveur pour éviter le spam de notifications

### Temps réel (SignalR)
- Chat global
- Compteur de joueurs connectés
- Événements temps réel :
  - `ScoreUpdate` (envoyé uniquement au joueur concerné)
  - `NewHighScore`
  - `PlayerReset`
- Synchronisation instantanée des scores

### Revenu passif
- Service en arrière-plan actif
- Ajout périodique de points aux joueurs
- Envoi ciblé des mises à jour uniquement aux joueurs connectés

### Tests
- Tests unitaires sur les services principaux
- Couverture élevée du `GameService`
- Validation des règles métier et des cas d'erreur

---

## Problème connu : affichage du BestScore côté frontend

### État actuel

- L'endpoint `GET /api/Game/BestScore` fonctionne correctement.
- Le backend retourne une réponse JSON valide, par exemple :

json{ "userId": 3, "bestScore": 106 }
Les données sont :

- correctement calculées

- correctement stockées en base

- correctement renvoyées par l'API

- Les événements SignalR (NewHighScore) sont bien émis et reçus.

### Problème
Malgré cela :

- Le BestScore ne s'affiche pas dans l'interface frontend.

- Le frontend reçoit bien la réponse JSON.

## Conclusion sur le problème
- Le dysfonctionnement est exclusivement lié à l'affichage frontend.
- J'ai essayeé de respecter les consignes, fournir les bonnes données et déclencher les bons événements.