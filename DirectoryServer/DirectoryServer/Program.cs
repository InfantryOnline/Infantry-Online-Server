/**
 * INFANTRY ONLINE v1.55 DIRECTORY SERVER
 * ======================================
 * 
 * Authors: Jovan
 *          Super-Man (Nebez)
 * 
 * Description:
 * 
 *   Provides an implementation of the Directory Server for version 1.55.
 *   
 *   For more information, see the Docs/DirServer folder.
 * 
 **/

namespace DirectoryServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Directory directory = new Directory();

            directory.Poll();
        }
    }
}
