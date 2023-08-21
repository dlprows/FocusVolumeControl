# Focus Volume Control Plugin

A plugin for the Stream Deck+ to control the volume of the focused application.

## Description

This Stream Deck plugin utilizes the Stream Deck+ encoders and screen to allow you to control the volume of the focused application.
Application volume is changed with the windows volume mixer.

Unlike faders or potentiometers, the encoders of the Stream Deck+ spin infinitely in either direction. Which means when you change your focused application, you don't have to worry about desynchronization with the current app.
The screen updates to show the name/icon of the app so that you can always know what you're about to change.

![Focus volume control plugin preview](previews/1-preview.png?raw=true)

## Developing

build the solution with visual studio
download the [stream deck distribution tool](https://docs.elgato.com/sdk/plugins/packaging) to `src/FocusVolumeControl/`
run `install.bat <debug | release>`

to debug, attach to the FocusVolumeControl running process


There is also a secondary sound browser project which can be used for viewing information about processes and how the algorithm matches them to volume mixers


## License

This project is licensed under the MIT License - see the LICENSE file for detiails

## Acknowledgements

Inspiration, code snippets, etc.
* [BinRaider's streamdeck-tools](https://github.com/BarRaider/streamdeck-tools)
* [Deej](https://github.com/omriharel/deej)
* [Stream Deck Developer Guide](https://docs.elgato.com/sdk/plugins/getting-started)
* [CoreAudio](https://github.com/morphx666/CoreAudio)

Inspiration
* [PCPanel](https://www.getpcpanel.com/)
