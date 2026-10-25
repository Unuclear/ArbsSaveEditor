# ASE – Save editor for Animal Revolt Battle Simulator


A command-line save editor for ARBS.

With ASE, you can edit the stats of unit prefabs and unlock all FPS Maze skins.



## Build

Clone/download the repository and run the respective build script.


### Requirements

Building requires you to have [Mono](https://www.mono-project.com/download/) and
[MKBundle](https://www.mono-project.com/docs/tools+libraries/tools/mkbundle/) installed.



## Usage

You need to have ARBS installed!

1. Open a terminal window.

2. If the dictionaries are outdated, run `ase-name-extractor`.
This should (re)create the directory `Dictionaries` with two entries. The save editor needs those to work.

  - If this fails, it could not find ARBS. Rerun with: `--game-dir path/to/ARBS`.

  - You only need to do this if ARBS recently added new units in an update.

3. Execute the save editor (in a terminal) with `ase`.

  - If ARBS is not in the default location, rerun with: `--game-dir path/to/ARBS`.
  
  - If the game saves are not in the default location, rerun with: `--data-dir path/to/data/dir`.

4. Just navigate the numerical menu.
