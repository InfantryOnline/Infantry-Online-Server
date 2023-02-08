# Infantry-Online-Server
Free Infantry Server Emulator

# Free Infantry

---

Infantry Online pits you against hundreds of players in real-time, fast paced tactical combat. Use momentum to your advantage and to your enemies detriment as your bullets and explosive shrapnel ricochet. Plan your attack from the shadows and ambush your enemy!

If you want to just get in on the action and play, visit http://www.freeinfantry.com/ and download the game client to play.

If you wish to take an existing zone and either make changes to it or improve upon it, or make a new zone entirely, then we have a separate guide that will get you started immediately with very few extra steps.

This repository is intended for an audience that wishes to know how the internals of the server work, and whoever wishes to help out with new bugs or features. The main output of the codebase here is the `InfServer.exe` file which powers all the zone servers that we run in our production environments.


## Prerequisites

You will need the following software installed before proceeding:

1. Windows 7+ (Infantry is Windows only)
2. [Visual Studio Community](https://visualstudio.microsoft.com/vs/community/) (or Visual Studio 2019+)
3. OPTIONAL [GitHub Desktop](https://desktop.github.com/) or any other preferred Git tools

 - If you want to contribute back to the project with code, you'll need to use a Git tool to submit pull request patches. If you're unfamiliar with Git, please first read through [Git Immersion](http://gitimmersion.com/).


## Download the Source Code

1. Create a new folder where you want the code and your test server to reside.
2. Either checkout (if you'll be contributing) - or download - the [master branch source code](https://github.com/InfantryOnline/Infantry-Online-Server).

## Building the Server

Now that you have the source code on your machine, it is time to build it into an executable.

1. Navigate into the folder where you downloaded the source code in the previous step.
2. In the trunk folder, double-click on the InfServer.sln file to open up your Visual Studio instance.
3. From the menu, select Build and then select Build Solution. This will build the server.

If you have any build errors at this stage, they will be shown to you. Make sure you have no build errors before continuing to the next step.

## Zone Assets


The server requires that the **`bin/assets`** folder (which you must create yourself, this is where you place all of your singular zone's files) have all the associated files necessary for the zone. Such files might include:

 - blo
 - cfg
 - itm
 - lio
 - lvl
 - nws
 - rpg
 - veh
 - release.txt
 - ... and any other files listed in the **cfg**'s `[Level]` and `[HelpMenu]` sections ...

Any files missing will be echoed back to you when you attempt to start the server. For original asset files that might not already be included you can use our [Zone-Assets](https://github.com/InfantryOnline/Zone-Assets) repository to try and find what you may need.


## Preparing server.xml

A previous step has created the InfServer executable located in the `bin` folder. Before you can run the server, the primary configuration file, `bin\server.xml`, must be properly edited. We'll cover the minimally required settings to get the server to run.

1. Open `server.xml` with your favourite text editor. Look at the `<server>` portion of the file.
2. Make sure `<zoneConfig value="" />` is set to the cfg file located in your assets folder (eg. Twin Peaks configuration file might be `ctf1.cfg`) - the assets folder is where zone-specific files are expected to be stored. By default, the zoneConfig will be set to a value of `"dodgeball.cfg"`.
3. Make sure `<gameType value="" />` is set to the proper game type for your zone. There are a limited number of Game Types, such as (non-exhaustive):

 - `GameType_CTF`
 - `GameType_CTF_OvD`
 - `GameType_NoMansLand`
 - `GameType_ZombieZone`

You can see all the Game Types available by navigating to the `bin\scripts\GameTypes\` folder.
Also they have to be case sensitive, so be sure to type the value exactly as it's named. _If a GameType does not exist in the **`bin\scripts.xml`** file_ (NOTE: scripts.xml, not server.xml), _it must be manually added. Use the other included GameTypes in the file as examples._ Game Type scripts control the logical flow of a zone, if you are creating a new zone or require specific scenarios to play out it is highly likely you will need to create a custom Game Type.

4. Make sure `<connectionDelay value="" />` is set to 0 (default of 2000). This will disable the database connection and allow for a "Stand Alone" or offline server mode, perfect for testing.

## Launching the Server

Finally, the time has come to launch the server.

1. Click on **`"bin\InfServer.exe"`**
2. Note any missing files; you will need to provide these in the **`bin/assets`** folder prior to continuing.
3. Your server is ready when the console prints "Server started.." and there are no other errors.
4. You should see a warning message: "Skipping database server connection, server is in stand-alone mode.."


## Infantry Client

Time to play!

1. Launch the Infantry executable (Infantry.exe - do not run the Launcher), go to View > Options, and clear the fields containing "infdir1.aaerox.com" and "infdir2.aaerox.com".  This will prevent the FI server from overwriting your local zone list file in the next step. Restore these fields, or run the Launcher, to connect back to the FI server to play on the official server. ~~You can also move the Infantry directory to a separate directory for testing.~~
2. Close the Infantry executable, and modify Infantry.lst accordingly before launching the Infantry executable (Infantry.exe), giving it an entry for your server. By default, the IP is 127.0.0.1 and the port is 1337. **Reminder:** You can find these settings in your `bin\server.xml` file. Here is an example line:
   `"Test Zone","127.0.0.1",1337,1,0,"The test zone's description.",50,0`
   **Note:** You will need to run the application you edit this file with as an administrator to make changes to this file on Windows 10 or newer.
3. Re-launch the Infantry executable. You should now see your modified zone list. Use the *auth [password] command to grant mod powers in your test zone. If you do not have a password set, simply add <managerPassword value="" /> under the <server> section in server.xml.

Have fun!
