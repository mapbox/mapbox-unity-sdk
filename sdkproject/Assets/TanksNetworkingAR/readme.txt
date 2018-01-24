This is the training day project Tanks ported to use the new Unity Multiplayer

It use a slightly modified version of the StandardAsset Lobby.
See StandardAssets/Network folder for more information on it.

Input
=====

Keyboard Mapping
----------------
Player 1:
	Forward = W
	Backward = S
	Rotate Left = A
	Rotate Right = D
	Fire = Space

Player 2 :
	Directional Arrow to move & turn
	Keypad Enter to fire

Pad (Xbox 360/One pad)
----------------------
  Forward = right trigger
  Backward = left trigger
  Fire = A
  Rotation = left/right on left stick

In Lobby
--------
Pressing Forward will cycle colors
Pressing Fire will get the player ready

Pressing Fire for the second player will add it to the lobby (allowing to do a
2 on one machine against two on another or 2-1-1)

Matchmaker
==========
To use the mathcmaker, you will need to create a project on your Unity Services
account (see "Windows->Unity Services" in the editor)

Misc. Notes
===========
The modification to the standard asset lobby are mainly graphical, but there is
one code modification : in LobbyPlayer, we added a check for "Fire2" to add a
second player locally, allowing 2 player on the same machine
