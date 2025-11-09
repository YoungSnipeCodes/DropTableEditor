# Drop Table Editor

Edit ROSE Online drop tables.
Also has a handy search feature.

![Drop Table Editor Screenshot](Screenshot.png)

## Needed in launch folder

- DropTableEditor.exe.config
- DropTableEditor.exe

## Usage

1. Browse to your ROSE Online 3DDATA folder
2. Click "Open Drop Table Editor"
3. Search and edit drop tables as needed
4. Click Save

## Requirements

- .NET Framework 3.5+
- ROSE Online 3DDATA folder with STB files

## Build

msbuild DropTableEditor.sln /p:Configuration=Release /t:Build

## Thanks
Thanks to Brett for the AruaMapEditor
