if ((Get-Item -Path (Get-Location)).Name -ne "ArbsSaveEditor") {
    Write-Error "Not inside solution root directory!"
    exit 1
}

# Build projects
msbuild -p:Configuration=Release
if ($LASTEXITCODE -ne 0) {
    exit 2
}

# Create ArbsSaveEditor.exe
$ase = "ArbsSaveEditor.exe"
Set-Location -Path "AseCore/bin/Release/"
if (-not ($?)) {
    exit 3
}
mkbundle -o "$ase" --simple --static --no-config --no-machine-config AseCore.exe
if ($LASTEXITCODE -ne 0) {
    exit 4
}

# Create AseNameExtractor.exe
$ase_name_extractor = "NameExtractor.exe"
Set-Location -Path "../../../AseNameExtractor/bin/Release/"
if (-not ($?)) {
    exit 5
}
mkbundle -o "$ase_name_extractor" --simple --static --no-config --no-machine-config AseNameExtractor.exe
if ($LASTEXITCODE -ne 0) {
    exit 6
}

Set-Location -Path "../../../"

# Move files to build directory
if (-not (Test-Path "build")) {
    New-Item -ItemType Directory -Path "build"
}
Move-Item -Path "AseCore/bin/Release/$ase" -Destination "build/" -Force
if (-not ($?)) {
    exit 8
}
Move-Item -Path "AseNameExtractor/bin/Release/$ase_name_extractor" -Destination "build/" -Force
if (-not ($?)) {
    exit 9
}
Copy-Item -Path "AseCore/bin/Release/Dictionaries" -Destination "build/" -Force -Recurse
if (-not ($?)) {
    exit 10
}

# Create archive
Set-Location -Path "build/"
$archive_name = "ASE-Windows-$(Get-Date -Format yyyy-MM-dd).zip"
Compress-Archive -Path "$ase", "$ase_name_extractor", "Dictionaries" -DestinationPath "$archive_name" -Force
if (-not ($?)) {
    exit 12
}
Write-Host "`nCreated archive $archive_name`n"

$answer = Read-Host -Prompt "Delete build files? (y/N)"
if ($answer -ne "Y" -and $answer -ne "y") {
    Set-Location -Path ..
    exit
}
Remove-Item -Path "$ase", "$ase_name_extractor", "Dictionaries" -Force -Recurse

Set-Location -Path ..
