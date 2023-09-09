Thanks for buying the Beat'Em Up Game template 3D! I hope the contents of this package will give you a great boost in developing your own Beat 'Em Up game.
Check out my Youtube channel for tutorials and more information about the product. (www.youtube.com/channel/UC9QvPxEcK2XUvaRT2kdpLhg/videos)
If you have any questions, visit my website at: www.osarion.com/assetstore, or send me an email at: info@osarion.com.

UPDATE HISTORY
---
Update 1.4
- ADDED Main Menu and implemented Basic UI menu flow
- ADDED Level Selection Screen
- ADDED Training scene where you can test moves and see input information
- ADDED Run functionality on double tap (direction keys)
- ADDED Run attacks (punch & kick)
- ADDED New input manager, you can now add new input actions in the inspector
- ADDED The name and player portrait in the HUD can now be set on the player prefab (Health System component)
- ADDED SpawnItemOnDestroy. Add this script to an enemy if you want this enemy to spawn a specific item when the enemy is defeated.
- CHANGED The player now jumps only once when the jump button is pressed and held down.
- FIXED Bug where the player got stuck in his fallDown animation when colliding in mid-air
- FIXED The Enemy healthbar is now hidden at start
- FIXED Bug where the player direction was different than it's graphic representation. This occurred when an opposite direction key was tapped shortly after an attack.

Update 1.3
- FIXED a bug in playermovement.cs where the player could get strange jittery movement when going left/right or when turning during a jump.
- ADDED code that turns projectiles towards their travel direction.

Update 1.2
- ADDED a character selection screen
- ADDED a new playable character
- ADDED a PlayerSpawnPoint to the level
- ADDED a 'gun pickup' and 'baseball bat' pickup
- ADDED 'EnemyNames.txt' to the project, so you can edit/add enemy names by editing the text file
- MOVED the MaxAttackers variable from 'Tools/GameSettings' to 'EnemyWaveManager'. So you can change the amount of enemies attacking simultaneously per level
- ADDED support for characters that have multiple mesh renderers
- ADDED option for enemy attacks to override player defence
- REMOVED linked components section in playerMovement and playerCombat. The code will now try to find these components automatically
- ADDED support for root motion animation

Update 1.1
- Switched player and enemy character to a Humanoid rig

Update 1.0 
- First Release