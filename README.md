# Game Development Project for COMP4990 - Project Management: Techniques and Tools

# Game Name: The Western Trail (working name)

This game development project simulates a survival environment where the user must survive from a starting 
location to a goal location, with branching paths they can choose along the way. Depending on the chosen path, the 
environment, dangers, and distance travelled will change, allowing for replay value. Additional features supporting 
the replay value are player-made characters which will be stored in a database and different modes such as standard 
difficulty, custom characters, and hard mode. The game will also support up to four different save files for the 
player.

Players must carefully manage their resources and possibly their team of up to 3 AI-controlled companions while 
surviving three major phases that iterate during the game – a travel phase where resource consumption and events 
may occur during travel from A to B, a combat phase where the player must survive for x amount of time against 
enemy AI before being able to escape, and a rest phase where the player can heal up their team and manage 
resources. If the player fails to manage their resources and survive combat sections, they will not make it to the 
finish area. Otherwise, the player’s final score and game statistics are displayed. In all phases, every time a major 
action is done, the game is autosaved to lock in the player’s choice.

The purpose is to deliver a polished game from design specifications and building it into a fun game with replay 
value for end-users, a regular goal for most game development projects. With a single developer, the project will be 
done during 2023, where milestones will be set throughout the year to have designs and components of the game 
done by a certain time, and the final deadline is by the end of the year to present the final product. This project’s 
success will be measured by interest in the final product, how efficient it can run, the replay value, and how 
immersive the game environment is to end-users.

# Documentation
* Button click sound from Dr. Scott Goodwin's COMP3770-2021F Game Design, Development, and Tools class
* Basic game development concepts from Dr. Scott Goodwin's COMP3770-2021F Game Design, Development, and Tools class
* Game AI concepts from Dr. Akram Vasighizaker's COMP4770-2023W Artificial Intelligence for Games class
* Player model visuals from Dr. Akram Vasighizaker's COMP4770-2023W Artificial Intelligence for Games class
* Camera control done with Cinemachine Package
* Gun sounds from https://mixkit.co/free-sound-effects/gun/ and https://mixkit.co/free-sound-effects/laser/
* Alert sound from https://mixkit.co/free-sound-effects/buzzer/
* Event popup sound from https://mixkit.co/free-sound-effects/notification/?page=2
* Physical attack sound from https://mixkit.co/free-sound-effects/sword/
* Mutant hurt sound from https://mixkit.co/free-sound-effects/monster/
* Combat win and pickup sound effect from https://mixkit.co/free-sound-effects/correct/
* Damage sound from https://assetstore.unity.com/packages/audio/sound-fx/free-sound-effects-pack-155776
* Crosshairs from https://www.kenney.nl/assets/crosshair-pack
* Map from https://www.freeusandworldmaps.com/html/USAandCanada/USPrintableOutline.html
* Main Menu BGM (Once and For All) by Onoychenkomusic from https://pixabay.com/music/search/genre/main%20title/
* Rest/Travel BGM (Reflected Light) by SergePavkinMusic from https://pixabay.com/music/search/genre/main%20title/
* Combat BGM (Are You Afraid of the Dark) by Ivan Luzan from https://pixabay.com/music/search/genre/main%20title/
* Wall Texture by PamNawi from https://opengameart.org/content/handpainted-stone-wall-textures
* Tartan Textures by Luke.RUSTLTD from https://opengameart.org/content/25-tartan-patterns
* Metal Textures by josepharaoh99 from https://opengameart.org/content/metal-from-frying-pan
* Glass Texture by Fupi from https://opengameart.org/content/shiny-window-pane
* Slime (Grass) Texture by LuminousDragonGames from https://opengameart.org/content/10-seamless-grass-textures-that-are-2048-x-2048
* Planks and Stone Texture by profpatonildo from https://opengameart.org/content/cartoon-outdoor-tileable-textures
* Package for implementing SQLCipher: https://github.com/netpyoung/SqlCipher4Unity3D

### SQLite's Licnese

``` license
All of the code and documentation in SQLite has been dedicated to the public domain by 
the authors. All code authors, and representatives of the companies they work for, have
 signed affidavits dedicating their contributions to the public domain and originals of 
 those signed affidavits are stored in a firesafe at the main offices of Hwaci. Anyone 
 is free to copy, modify, publish, use, compile, sell, or distribute the original SQLite
  code, either in source code form or as a compiled binary, for any purpose, commercial 
  or non-commercial, and by any means.

The previous paragraph applies to the deliverable code and documentation in SQLite - 
those parts of the SQLite library that you actually bundle and ship with a larger 
application. Some scripts used as part of the build process (for example the "configure"
 scripts generated by autoconf) might fall under other open-source licenses. Nothing 
 from these build scripts ever reaches the final deliverable SQLite library, however, 
 and so the licenses associated with those scripts should not be a factor in assessing 
 your rights to copy and use the SQLite library.

All of the deliverable code in SQLite has been written from scratch. No code has been 
taken from other projects or from the open internet. Every line of code can be traced 
back to its original author, and all of those authors have public domain dedications on 
file. So the SQLite code base is clean and is uncontaminated with licensed code from 
other projects.
```

### SQLCipher's Licnese

``` license
Copyright (c) 2008-2012 Zetetic LLC
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
    * Redistributions of source code must retain the above copyright
      notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
      notice, this list of conditions and the following disclaimer in the
      documentation and/or other materials provided with the distribution.
    * Neither the name of the ZETETIC LLC nor the
      names of its contributors may be used to endorse or promote products
      derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY ZETETIC LLC ''AS IS'' AND ANY
EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL ZETETIC LLC BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
```