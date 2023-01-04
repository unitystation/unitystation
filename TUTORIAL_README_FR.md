![](https://badgen.net/badge/Side/Projet/blue?icon=github) ![](https://img.shields.io/badge/TestedOn-Ubuntu18.04-orange) ![](https://img.shields.io/badge/TestedOn-Windws10-blue) 
 ===
 [![forthebadge cc-by](https://licensebuttons.net/l/by-nc-sa/4.0/88x31.png)](https://creativecommons.org/licenses/by/4.0) [![](https://img.shields.io/badge/Discord-7289DA?style=for-the-badge&logo=discord&logoColor=white)](https://discord.gg/tyJX8dx) 

## Tutoriel Solo - Les bases

[TOCM]

[TOC]

Tutoriel Solo - Les bases  
├── [Languages utilisés](#languages-utilisés)  
├── [Description du projet](#description-du-projet)  
├── [Credit , participant, organisation](#credit--participant-organisation)  
├── [Public cible](#public-cible)  
├── [But du projet](#but-du-projet)
│....└── [Détails techniques](#détails-techniques)  
│....├── [Tutoriel-bot](#tutoriel-bot)  
│....└── [Mode admin](#mode-admin)  
├── [Installation](#installation)  
│....├── [Actuellement](#actuellement)  
│........└── [Projet rendu](#projet-rendu)  
├── [Mise à jour](#mise-à-jour)  
├── [Usage](#usage)  
└── [Contribution / dev](#contribution--dev)  
....└── [ToDo](#todo)

-------------

### Languages utilisés

C# Unity

XML

-------------

  

### Description du projet

Ce projet a pour but  de crée un module complementaire pour Unitystation, afin d'apprendre les bases aux nouveaux joueurs découvrant le jeu.

-------------

### Credit , participant, organisation

Géré par l'équipe de developpement de Unionrolistes

Liste des contributeurs -- [Credit.md](/Credit.md) & [Licence.md](/LICENSE)

-------------

### Public cible

Le projet a pour cible les joueurs débutants

découvrant Unitystaiton.

  

### But du projet

-   Apprendre comment se déplacer
-   Se vêtir
-   Usage d'un ID et d'un PDA, ainsi que de la lumière de navigation
-   Utiliser main gauche et main droite
-   Gestion de l'inventaire
-   Raccourcis clavier usuels
-   Equiper un masque à oxygène et savoir l'activer/désactiver
-   Réparer une brèche dans la coque et y survivre
-   Tirer, pousser
-   Se déplacer par dessus une table
-   (Optionnel) Acheter des objets dans les distributeurs
-   (Optionnel) Utiliser le jukebox
-   (Optionnel) Utiliser et recharger une arme
    - à feu
    - à énergie
-   (Optionnel) Comprendre le système de blessures, et de soins basiques (appliquer au joueurs un dégât)
-   (Optionnel) Savoir planter une graine pour se familiariser avec les mécaniques d'un métier (botanic basics)
-   (Optionnel) Utiliser un extincteur pour éteindre un feu, et forcer une porte sans électricité
-   Reconnaître une navette d'évacuation et s'y diriger
 

#### Détails techniques

Le tutoriel sera composé de plusieurs salles, obligatoires ou facultatives, que le joueur pourra explorer à son rythme.
 
Pour l'accompagner et le guider, sera présent le Tutorial-bot.

C'est un NPC qui a pour fonction de suivre le joueur et de réciter un dialogue dans le tchat du jeu lorsque le joueur est à un endroit spécifique.

##### Tutoriel-bot ![alt text](https://raw.githubusercontent.com/Unitystation-fork/UnityStation-Tutorial/main/Assets/Textures/Bot/Attention-Front/attention-front-1.gif)

La charte graphique est ainsi définie:

Un bot dont l’asset (sprite) fera 32x32, représentant un écran flottant.

L’écran aura deux affichages possibles :

-   un écran « neutre » avec le logo Nanostrasen
-   un écran « interaction » avec un point d’exclamation (pour attirer l’attention)

Il est possible que, plus tard, l’écran aie des fissures ou soit éteint pour afficher son état de santé  (facultatif).

Sa base sera un flotteur antigravité (donc animation de suspension). Il faudra donc une face, un dos, deux profils pour donner l’illusion d’une entité 3D

 
Ses dialogues sont au format XML, facilitant ainsi la localisation dans les langues suivantes :

-   Anglais
-   Français
-   Russe
-   Allemand

##### Mode admin

Bien que le jeu soit theroquiement self-hosted en local
L'affichage de l'interface en mode admin et de la console sont **désactivées**, mais il est possible qu'elles le soient tout le temps (à verifier).
--verifier la possibilité ou non de faire le tutoriel en coop.

-------------

### Installation
#### Actuellement

 1. Créez un dossier Unitystation dans votre dossier contenant vos jeux.
 2. Téléchargez le build test au format .zip Version [Windows](https://mega.nz/file/ttkRRQya#_KBNU_OqKd7jDkEqPcdYlQT1EixCwMXfpD7_WYjYgSo) ou [Linux](https://mega.nz/file/V0llFJ5A#BpL7vBYsQ9B-vadHhEAZvYdjsg9pNl_qkDmKGthBnHY)
 3. Decomprésser l'archive, rendez la executable si besoin.
 4. Collez et décompressez l'archive, vous devriez voir le build à executer

  
#### Projet rendu

(Voir [Installer]([https://github.com/Unitystation-fork/Unitystation-WikiV2/blob/main/docs/1_HowToInstallGame/1_HowInstall.FR.md](https://github.com/Unitystation-fork/Unitystation-WikiV2/blob/main/docs/1_HowToInstallGame/1_HowInstall.FR.md)) Station Hub)

Téléchargez un build ,

Allez dans l'onglet "installation"

Executez celui-ci

  

-------------

### Mise à jour
Actuellement les Mise à jour du tutoriel se feront par le telechargement et l'ecrasement de l'ancien dossier par le nouveau.
une fois le projet rendu, les update seront géré par StationHub directement.

-------------

### Usage

Une fois le build executé (executable Unitystation) :

Allez dans le menu [color=#26B260][Tutoriel][/color]

Sélectionnez la langue

--- Actuellement le russe ne fonctionne pas

Profitez de votre experience de jeu exclusive !

  

Pour quitter le tutoriel, dirigez-vous vers la navette au sud de la station. Sur le côté droit, vous trouverez une console. En marchant dessus, le tutoriel se termine.

  

(idée : menu qui propose d'autres scenarios de tutoriels départements)

---
### how to contribue - Players
vous pouvez nous faire vos retour, positif et negatif (aussi construit que possible svp) en ouvrant une issue


### how to contribue - DEV

[DEV.MD](https://github.com/Unitystation-fork/UnityStation-Tutorial/blob/main/Dev.md)


  

-------------

#### prototype

il existe une video de gameplay pour la version 1.0.5   [Video Prototo 1.0.5](https://youtu.be/SM2RSpfiJys)



