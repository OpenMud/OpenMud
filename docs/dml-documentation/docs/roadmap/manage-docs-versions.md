---
sidebar_position: 1
---

# OpenMud Goals

OpenMud has the following primary goals:
* Open Source Game Engine
* Simple and approachable for non-developers
* Enable cohesive and rapid collaboration
* Implement enough of the Byond Framework necessary to ease the ability to port existing games to an open source alternative
* Expand support for Isometric Games
* Providing a managed solution for hosting OpenMud games

# OpenMud - Out of Scope

OpenMud attemps to serve as an approachable game engine. The following is out of scope:

1. OpenMud will not support any interface / GUI declarations originating from Byond Games, or does the OpenMud project attempt to have any explicit GUI features. GUI is a complex & solved problem. We encourage users to modify the open-source OpenMud web client to create cohesive user-interfaces, or the open-source Terminal Client (for classic ascii mud games).

2. Development Tools are currently out of scope for OpenMud. OpenMud does not provide a full-fledged development environment, or special tools for building games. Users are encouraged to:
    1. Use Visual Studio Code when writing Dream Maker Language scripts. Please follow the guide "Setting up Your Development Environment"
    2. Use DreamMaker from Byond. Largely, projects built in DreamMaker will compile fine in OpenMud
    3. Use aseprite for graphical assets. While OpenMud supports compiling DMI resource files, it is encouraged to use open-source alternatives such as aseprite
