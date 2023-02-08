# Quick Start - Host your own Zone Server in minutes

This process is intended for zone developers who wish to quickly get up and running with an existing zone
and make modifications to it, or use the zone as the basis of new zones they make.

You can find existing zone packs we have in this folder: https://assets.freeinfantry.com/dev-packs/.
Simply pick one, download it and extract it anywhere. By default, the server will run on address `127.0.0.1` port `1337`.

You are almost done. Now you just need to tell your client to show your server in the zone listing file,
which means disconnecting from our directory server. Here is how:

1. Launch the Infantry executable (Infantry.exe - do not run the Launcher), go to View > Options, and clear the fields containing "infdir1.aaerox.com" and "infdir2.aaerox.com".  This will prevent the FI server from overwriting your local zone list file in the next step. Restore these fields, or run the Launcher, to connect back to the FI server to play on the official server. ~~You can also move the Infantry directory to a separate directory for testing.~~
2. Close the Infantry executable, and modify `Infantry.lst` accordingly before launching the Infantry executable (Infantry.exe), giving it an entry for your server. By default, the IP is 127.0.0.1 and the port is 1337. **Reminder:** You can find these settings in your `bin\server.xml` file. Here is an example line:
   `"Test Zone","127.0.0.1",1337,1,0,"The test zone's description.",50,0`
   **Note:** You will need to run the application you edit this file with as an administrator to make changes to this file on Windows 10 or newer.
3. Re-launch the Infantry executable. You should now see your modified zone list. Use the `*auth [password]` command to grant mod powers in your test zone. If you do not have a password set, simply add `<managerPassword value="" />` under the `<server>` section in server.xml.
