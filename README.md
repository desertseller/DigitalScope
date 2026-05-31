<div align="center">
   <h1>DigitalScope</h1>
</div>

A lightweight real time screen magnifier and crosshair overlay for Windows that lets you zoom in on the center of your screen and display customizable crosshairs

<p align="center">
   <img src="assets/DSDemo.gif" alt="FluxTranslator" width="900">
</p>

## Features

- Real time screen magnifier overlay 
- Adjustable zoom level and window size
- Crosshair overlay with customizable styles, size, color, opacity, gap, and thickness
- Click through window, so it does not block mouse input
- Hotkey toggle for quick on/off

## Installation

### Requirements
- .NET 8 SDK or later
- Windows 11

### Installation
1. Go to the [Releases](https://github.com/desertseller/digitalscope/releases).
2. Download `DigitalScope_Setup.exe`.
3. Run the installer and follow the instructions.
4. Launch the app via the Desktop shortcut or Start Menu.

> [!WARNING]
> #### Windows SmartScreen
> Because this project is free and open source the installer does not come with a digital certificate. Windows may display a SmartScreen message when you first run it.

### Build from source

Building locally ensures the exe has a unique binary signature.

1. Go to the [Releases](https://github.com/PawelKawka/DigitalScope/releases) or clone this repo.
2. Download the latest source code.
3. Open a PowerShell in the repository.
4. Run script:
```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```
5. The compiled output lands in the `build\` folder. Run `DigitalScope.exe` from there.

You can customize the output directory:
```powershell
-OutputDir "set path" 
```

> [!WARNING]
>**Author takes no responsibility for any game bans, account suspensions or other consequences** that may result from using this software while playing online games.  
>Some anticheats perform broad behavioural analysis and may flag or react to overlay tools even if they do not interact with game memory.

## About
- Developed by Pawel Kawka.
- Open Source and free to use.