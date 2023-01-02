## UnityStation

[TOCM]

[TOC]


----------

```properties
Bonjour, vous etes arriver ici, car vous souhaitez contribuer au developpement du tutoriel. félicitation ! (et merci)
vous trouverez ici les information utile vous fous facilité la vie dans votre tache.
```

**Bonne chance.**

### Lancement du projet pour DEV

[Markdown](https://docs.github.com/en/get-started/writing-on-github/getting-started-with-writing-and-formatting-on-github/basic-writing-and-formatting-syntax)

 - lisez ceci [Starting Contribution - UnityStation](https://unitystation.github.io/unitystation/contribution-guides/Starting-contribution/)

 - Installer unity


----------

### Credit , participant, organisation

Géré par l'équipe de developpement de Unionrolistes Liste des contributeur [Credit.md](/Credit.md) & [Licence.md](/LICENSE)

### But du projet / public cible

 ```properties
Actuellement tu as Unitystation, qui fait sa petite vie, update/bug/update/bug etc.
NOUS, qui travaillons sur un niveau tutoriel , un mode solo (dans un jeu multi) avec instruction et dialogues.

**à l'origine** le projet est concu pour n'etre qu'une brique de lego qu'il faut plug sur unitystation pour fonctionner.
sauf que en version  1.0.5 (le build dispo dans le readme) on as pas le temps, donc on a pris la version de aout, ont l'a bourré tous notre contenue dedans, mis deux bout de sctoch, et sa à fait un standalone "tutoriel"
mais un-maintenancable car trop de truc cassé ou manquant (genre la documenation que j'ai du reecrire)

actuellement, les devs de mon equipe n'arrive pas à faire quelque chose de viable avec "submodule" qui est la technologie pour empacté le tous 
donc en attendant, on a repris la méthode de la  1.0.5, on a fait un fork basé sur une ancienne version relativement fonctionnel
qu'on modifie allégrement pour faire tourné le niveau tutoriel. 
**edit** et je viens de me rendre compte que c'est pas du tous une version standalone car je vois dans l'historique, des commit de l'equipe principale, d'y a deux jour.

ils travail sur la listes des bug (genre le bot-guide qui reste coincé dans un mur, ou des dialogue manquant) 

PUIS feront l'update pour faire un standaone avec la version actuel (decembre22) (**oudated**)
et SI on y arrive on empacte seulement les element nouveau du tutoriel, pour en faire un submodule
que l'utilisateur unitystation pourra a loisir activé ou non
```


----------

### Installation

- Unity Hub [Hub](https://unity3d.com/get-unity/download] en version  2021.3.12f1 LTS

- Cloner unitystation-dev_2022_08_24 (Branche:Tutorial) [ForkMe](https://github.com/Unitystation-fork/unitystation-dev_2022_08_24)
 
- Lancer le projet unitystation-dev_2022_08_24/UnityProject

----------

### Mise à jour

pour l'ajout de nouveau code, vous devez cree une branche a partir des ticket issues, ce qui nous facilitera le suivie des commit / merge .

----------

### Usage

#### .

----------

Vidéo:

[https://youtu.be/krssJiDJLhY](https://youtu.be/krssJiDJLhY)

Une salle où un objet bloque le passage, Il faut passé par dessus une table (non en verre) tirer une caisse, en poussé une autre, frapper un objet pour le brisé, jeter un objet

le joueur dois se blesser avec un eclat de verre pour utilisé le medkit.

une salle sans electricité pour un exercice incendie. marcher (pour évité de glissé)

si le joueur meur, le scenario reprend a zero, et un message "try again," devrai s'afficher

----------

READ.ME : Thomas.K

This is a tutorial made for Unitystation.

Below is a resume to what I modify / added in development :

-   Added script "GUI_Tutorial" : when button pressed, go to tutorial and start hosting.

~ Modify "SubSceneManager" script : - (32-37) Added bool and map scene variable for tutorial - (51-60) Added if statement : replace normal map by tutorial map if goToTutorial is true

~ Modify "SubSceneManager.SceneList" script : - (93-105) Modify normal random map loading to have tutorial loading option with if-else statement

-   Added script "Tutorial" : manage tutorial zone with trigger enter (exemple : trigger on player spawn that make tutorial bot appear)
    
-   Added script "TutoPlayer" : Make player Rigidbody2D body type on Dynamic instead of Kinematic ONLY WHEN ON TUTORIAL SCENE, if not thte script remove itself
    

----------

Vidéo:

Tutorial Progress 02

[https://cdn.discordapp.com/attachments/967463756181430282/1004527747374260446/Tutorial_Progress_02.mp4](https://cdn.discordapp.com/attachments/967463756181430282/1004527747374260446/Tutorial_Progress_02.mp4)

Vidéo:

Tutorial Progress 03

[https://youtu.be/S36u4GSQ5Gw](https://youtu.be/S36u4GSQ5Gw)

----------

Projet [https://github.com/Unitystation-fork/UnityStation-Tutorial](https://github.com/Unitystation-fork/UnityStation-Tutorial)

version de dev 2022.08.24 last bugfix #9164
unity Version 2021.1.3

----------

INFO TEST JEU

-   4Gb Ram (8 recommended)
-   i5 (i7 recommended)
-   ~150-200Mb Hdd
-   1Gio GPU

----------


[TODO](https://github.com/orgs/Unitystation-fork/projects/1/views/4?visibleFields=%5B%22Title%22%2C%22Repository%22%2C%22Assignees%22%2C%22Status%22%5D](https://github.com/orgs/Unitystation-fork/projects/1/views/4?visibleFields=%5B%22Title%22%2C%22Labels%22%2C%22Assignees%22%2C%22Repository%22%2C%22Status%22%5D))


------

Salle obligatoire push-pull  [PushPull Video](https://youtu.be/krssJiDJLhY)


Salle exercice incendie [FireExercice Video](https://github.com/Unitystation-fork/UnityStation-Tutorial/blob/main/Images/2022-08-30-181759_1920x1080_scrot.png?raw=true)
