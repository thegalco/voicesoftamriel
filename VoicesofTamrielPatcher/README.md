# Voices of Tamriel Patcher

A Synthesis patcher for Skyrim Special Edition that pseudo-randomly replaces vanilla NPC voices with voices from the "Voices of Tamriel" mod.

## Requirements

- Skyrim Special Edition
- [Synthesis](https://github.com/Mutagen-Modding/Synthesis) patcher framework
- [Voices of Tamriel](https://www.nexusmods.com/skyrimspecialedition/mods/156750) mod installed and loaded

## Installation

1. Add the patcher to Synthesis
2. Ensure "VoicesOfTamriel.esp" is enabled in your load order before running
3. Configure settings as desired
4. Run the patcher

## Configuration

The patcher provides three configurable settings:

- **Skip Unique NPCs** (default: true): Prevents unique NPCs from having their voices changed
- **Use Randomization** (default: true): STRONGLY recommend to use this. When enabled, VOT voices are randomly applied. When disabled, all matching NPCs get VOT voices
- **Randomization Chance** (default: 50%): The chance that a matching NPC will receive a VOT voice when randomization is enabled