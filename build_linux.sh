#!/usr/bin/env sh

[ "$(basename "$(pwd)")" != "ArbsSaveEditor" ] && >&2 echo "Not inside solution root directory!" && exit 1

# Build projects
msbuild -p:Configuration=Release || exit 2

# Create ase executable
ase="ase"
cd AseCore/bin/Release/ || exit 3
mkbundle -o "$ase" --simple --static --no-config --no-machine-config AseCore.exe || exit 4

# Create ase-name-extractor executable
ase_name_extractor="ase-name-extractor"
cd ../../../AseNameExtractor/bin/Release/ || exit 5
mkbundle -o "$ase_name_extractor" --simple --static --no-config --no-machine-config AseNameExtractor.exe || exit 6

cd ../../../ || exit 7

# Move files to build directory
mkdir -p build
mv "AseCore/bin/Release/$ase" build/ || exit 8
mv "AseNameExtractor/bin/Release/$ase_name_extractor" build/ || exit 9
cp -r AseCore/bin/Release/Dictionaries build/ || exit 10

# Create archive
archive_name="ASE-Linux-$(date -I).tar.gz"
tar -czf "build/$archive_name" "build/$ase" "build/$ase_name_extractor" "build/Dictionaries/" || exit 12
printf '\nCreated archive %s\n' "$archive_name"
unset archive_name

printf '\nDelete build files? (y/N) : '
read -r answer
[ "$answer" != "Y" ] && [ "$answer" != "y" ] && cd ../ && exit
rm -R "build/$ase" "build/$ase_name_extractor" "build/Dictionaries/"
unset answer
