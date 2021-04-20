# Joycon DecaMove Emulator
A very WIP DecaMove emulator for Joycons. 

I am working on removing the need for com0com, it already works by hooking directly into the service and bypassing the serial communication, however the software still needs to see the serial connection in order to work :(

## Installation

1. Install DecaHub.

2. Clone the github repository or download the zip archive and extract all files.

3. Connect Joycon to PC via Bluetooth.

4. Navigate to 'DecaMoveEmulator\bin\x64\Debug' and run DecaMoveEmulator.exe
If com0com is not installed, it will instruct you to install it. Once you get to "Choose components" ensure that CNCA0 <-> CNCB0 is UNCHECKED. Press enter to continue running once the installation is complete.

5. SteamVR should start automatically, launch any compatible game from DecaHub.


