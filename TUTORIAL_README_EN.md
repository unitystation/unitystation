![](https://badgen.net/badge/Side/Projet/blue?icon=github) ![](https://img.shields.io/badge/TestedOn-Ubuntu18.04-orange) ![](https://img.shields.io/badge/TestedOn-Windws10-blue) 
 ===[![forthebadge cc-by](https://licensebuttons.net/l/by-nc-sa/4.0/88x31.png)](https://creativecommons.org/licenses/by/4.0) [![](https://img.shields.io/badge/Discord-7289DA?style=for-the-badge&logo=discord&logoColor=white)](https://discord.gg/tyJX8dx) 

## Solo Tutorial - The Basics

[TOCM]

[TOC]

```Markdown
Solo Tutorial - The Basics
├── Languages used
├── Project description
├── Credit, participant, organisation
├── Target audience
├── Purpose of the project
│	└── Technical details
│		├── Tutorial-bot 
│		└── Admin mode
├── Installation
│ 	├── Currently
│		└── Project rendered
├── Update
├── Usage
└── Contribution / dev
    └── ToDo

```

-------------

### Languages used

C# Unity

XML

-------------

  

### Project description

This project aims to create an add-on module for Unitystation, to teach the basics to new players discovering the game.

-------------

### Credit, participant, organization

Managed by the Unionrolistes development team

List of contributors -- [Credit.md](/Credit.md) & [License.md](/LICENSE)

-------------

### Target audience

The project is aimed at beginner players

discovering Unitystaiton.

  

### Goal of the project

- Learn how to move
- Dressing up
- Use of ID and PDA, and navigation light
- Using left and right hand
- Inventory management
- Common keyboard shortcuts
- Equipping an oxygen mask and knowing how to activate/deactivate it
- Repair and survive a hull breach
- Shooting, pushing
- Move over a table
- (Optional) Buy items from vending machines
- (Optional) Use the jukebox
- (Optional) Use and reload a gun
    - firearm
    - energy gun
- (Optional) Understand the wounding system, and basic healing (applying damage to players)
- (Optional) Plant a seed to learn the mechanics of a craft (botanic basics)
- (Optional) Use a fire extinguisher to put out a fire, and force a door without electricity
- Recognize and navigate an evacuation shuttle
 

#### Technical details

The tutorial will be composed of several rooms, mandatory or optional, that the player can explore at his own pace.
 
The Tutorial-bot will accompany and guide the player.

This is an NPC whose function is to follow the player and to recite a dialogue in the game chat when the player is in a specific place.

##### Tutorial-bot ![alt text](https://raw.githubusercontent.com/Unitystation-fork/UnityStation-Tutorial/main/Assets/Textures/Bot/Attention-Front/attention-front-1.gif)

The graphic charter is thus defined:

A bot whose asset (sprite) will be 32x32, representing a floating screen.
The graphic charter is thus defined:

A bot whose asset (sprite) will be 32x32, representing a floating screen.

The screen will have two possible displays:

- a "neutral" screen with the Nanostrasen logo
- an "interaction" screen with an exclamation mark (to attract attention)

Later on, the screen may have cracks or be turned off to show its health status (optional).

Its base will be an anti-gravity float (so suspension animation). It will therefore need a face, a back, two profiles to give the illusion of a 3D entity

 
Its dialogues are in XML format, thus facilitating localisation in the following languages

- English
- French
- Russian
- German

##### Admin mode

Although the game is theroquely self-hosted in local
The display of the admin mode interface and the console are **disabled**, but it is possible that they are disabled all the time (to be checked).
--Check if the tutorial can be done in co-op.

-------------

### Installation
#### Currently

 1. Create a Unitystation folder in your games folder.
 2. Download the test build in .zip format Version [Windows](https://mega.nz/file/ttkRRQya#_KBNU_OqKd7jDkEqPcdYlQT1EixCwMXfpD7_WYjYgSo) or [Linux](https://mega.nz/file/V0llFJ5A#BpL7vBYsQ9B-vadHhEAZvYdjsg9pNl_qkDmKGthBnHY)
 3. Unzip the archive, make it executable if needed.
 4. Paste and unzip the archive, you should see the build to run

  
#### Rendered project

(See [Install]([https://github.com/Unitystation-fork/Unitystation-WikiV2/blob/main/docs/1_HowToInstallGame/1_HowInstall.FR.md](https://github.com/Unitystation-fork/Unitystation-WikiV2/blob/main/docs/1_HowToInstallGame/1_HowInstall.FR.md)) Station Hub)

Download a build,

Go to the "install" tab

Execute this one

  

-------------

### Update
Currently, updates to the tutorial are done by downloading and overwriting the old folder with the new one.
Once the project is rendered, updates will be handled by StationHub directly.

-------------

### Usage

Once the build is executed (Unitystation executable):

Go to the **[Tutorial]** menu

Select the language

--- Currently Russian does not work

Enjoy your exclusive gaming experience!

  

To exit the tutorial, head to the shuttle to the south of the station. On the right side you will find a console. By stepping on it, the tutorial ends.

  

(idea: menu that offers other tutorial scenarios departments)

---
### how to contribute - Players
you can give us your feedback, positive and negative (as constructive as possible please) by opening an issue


### how to contribute - DEV

[DEV.MD](https://github.com/Unitystation-fork/UnityStation-Tutorial/blob/main/Dev.md)


  

-------------

#### prototype

there is a gameplay video for version 1.0.5 [Video Prototo 1.0.5](https://youtu.be/SM2RSpfiJys)
