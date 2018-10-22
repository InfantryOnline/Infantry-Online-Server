Free Infantry - Launching a Stand-Alone Server
==============================================


Introduction
------------

This short guide outlines how to get the latest version of the server from the SourceForge repository, 
set the server settings, and run the server with your own zone.

It is intended for server programmers, rather than zone developers or players, but it's written for everyone in mind.


Prerequisites
------------

You will need the following software installed before proceeding:

### 1. Tortoise SVN

Firstly, [download TortoiseSVN](http://tortoisesvn.net/downloads.html). A restart may be required
to complete the installation.

### 2. Visual C# Express 2010 --or-- Visual Studio 2010 (At Least)

Finally, [download Visual C# Express 2010](http://www.microsoft.com/express/Downloads/#2010-Visual-CS).
Alternatively, if you already have Visual Studio 2010, you may use that instead.


Obtaining the Latest Server Source Code
---------------------------------------

Using TortoiseSVN, the most up-to-date code for the server can be obtained from our SourceForge repository.

1. Create a new folder where you wish the code to download.
2. Right-click on the folder and click on the SVN Checkout option.
3. In the opened dialog, set the URL of the Repository to the following: `https://infserver.svn.sourceforge.net/svnroot/infserver`
4. Make sure that the Checkout Depth option is set to Fully recursive, and that Revision is set to HEAD revision. Click OK.

The dialog will inform you once all the files have been downloaded.

Building the Server Executable
------------------------------

Now that you have the source code on your machine, it is time to build it into an executable.

1. Navigate into the folder where you downloaded the source code in the previous step.
2. In the trunk folder, double-click on the InfServer.sln file to open up Visual C# Express 2010, or Visual Studio 2010.
3. From the menu, select Build and then select Build Solution. Alternatively, press F6. This will build the server.

If you have any build errors at this stage, they will be shown to you. Make sure you have no build errors before continuing to the
next step.


Assets
------

The server requires that the trunk/bin/assets folder have all the assets necessary for the zone. This includes:

 * blo
 * lvl
 * lio
 * veh
 * cfg
 * release.txt

Any files missing will be echoed back to you when you first start the server.


Preparing server.xml
--------------------

A previous step has created the InfServer executable located in the trunk/bin folder. Before you can run the server, the
primary configuration file server.xml must be properly edited.

1. Open server.xml with your favourite text editor. Look at the &lt;server&gt; portion of the file.
2. Make sure <zoneConfig value= "" /> is set to the cfg file located in your assets folder (eg. Twin Peaks configuration file is ctf1.cfg)
3. Make sure <gameType value= "" /> is set to the proper game type for your zone. There are a limited number of Game Types:

 * GameType_CTF
 * GameType_CTF_OvD
 * GameType_NoMansLand
 * GameType_ZombieZone
 
You can see all the Game Types available by navigating to the trunk/bin/scripts/GameTypes/ folder.
Also they have to be case sensitive, so type exactly how its named.

4. Make sure <connectionDelay value="" /> is set to 0. This will disable the database connection unless you are using a database, if so
disreguard this step.

Launching the Server
--------------------

Finally, the time has come to launch the server.

1. Click on InfServer
2. Note any missing files; you will need to provide these in the trunk/bin/assets folder prior to continuing.
3. Ignore the connection failure with the database server; this is a stand-alone server that does not require a database.
4. Your server is ready when the console prints "Starting server..."

Infantry Client
---------------

Time to play!

1. Launch the Infantry executable (Infantry.exe), go to View > Options, and clear the fields containing "infdir1.aaerox.com" 
and "infdir2.aaerox.com".  This will prevent the FI server from overwriting your local zone list file.  Restore these fields to connect 
back to the FI server to play.  You can also move the Infantry directory to a separate directory for testing.
2. Close the Infantry executable, and modify Infantry.lst accordingly before launching the Infantry executable (Infantry.exe), giving it 
an entry for your server. By default, the IP is 127.0.0.1 and port is 1337.
3. Re-launch the Infantry executable. You should now see your modified zone list. Use the *auth [password] command to grant mod powers 
in your test zone. If you do not have a password set, simply add <managerPassword value="" /> under the <server> section in server.xml.

Have fun!