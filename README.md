# DeepNestPort (Dxf Fork)

This  fork was created to Add Dxf import and export support from the commandline.

Now using IXMilia Dxf Under (Apache License 2.0)

DeepNest C# Port (https://github.com/Jack000/Deepnest)

**Project progress: 80%**

<img src="imgs/img1.png"/>

## Compiling minkowski.dll
1. Replace <boost_1.62_path> with your real BOOST (1.62) path in compile.bat

Example:
```
cl /Ox ..... -I "C:\boost_1_62_0" /LD minkowski.cc
```
2. Run compile.bat using Developer Command Prompt for Visual Studio
3. Copy minkowski.dll to DeepNestPort.exe folder

## Recent changes in this forked repo:
1. Upgrade all projects to .NET 5 to support cross platform compilation (Tested in Windows and Linux). The GUI project can only be used in Windows.
2. Added Cmake to easily compile Minkowski on different platforms
3. Support importing/exporting DXF files directly from the command line (imported dxf geometry is overly simplified at the moment)

## Installation Instructions (Linux):
1. Install [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
2. Install [Cmake](https://cmake.org/download/)
3. Install [Boost](https://www.boost.org/users/download/), preferably use `sudo apt install libboost-all-dev`
4. `cd bird-nest`
5. `dotnet build ./BirdNestCLI/BirdNestCLI.csproj --runtime ubuntu.20.04-x64 --configuration Release` Replace ubuntu.20.04-x64 with the specific linux distribution.
6. `cmake .`
7. `make`
8. `cp Minkowski/libMinkowski.so BirdNestCLI/bin/Release/net5.0/ubuntu.20.04-x64/libMinkowski.so`
