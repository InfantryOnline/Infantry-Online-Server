using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using InfServer.Protocol;
using InfServer.Game;

namespace InfServer.Logic
{	// Logic_LoginAssets Class
	/// Deals with distributing assets during the login process
	///////////////////////////////////////////////////////
	class Logic_LoginAssets
	{
		/// <summary>
		/// Triggered when the client is attempting to enter the game and sends his security reply
		/// </summary>
		static public void Handle_CS_RequestUpdate(CS_RequestUpdate pkt, Player player)
		{	//If the player isn't logged in, ignore
			if (!player._bLoggedIn)
			{	//Log and abort
				Log.write(TLog.Warning, "Player {0} tried to send asset request while not logged in.", player);
				player.disconnect();
				return;
			}

			//Delegate it to another thread so we don't spend ages caching
			LogClient logger = Log.getCurrent();

			ThreadPool.QueueUserWorkItem(
				delegate(object state)
				{
					using (LogAssume.Assume(logger))
					{	//Prepare a list of cached assets
						List<SC_AssetUpdateInfo.AssetUpdate> cache = new List<SC_AssetUpdateInfo.AssetUpdate>();

						foreach (CS_RequestUpdate.Update update in pkt.updates)
						{	//Find the matching cached asset
							AssetManager.Cache.CachedAsset cached = player._server._assets.AssetCache[update.filename];
							if (cached == null)
							{	//Strange
								Log.write(TLog.Warning, "Unable to find cache for requested asset '{0}'", update.filename);
								continue;
							}

							//Good, add it to the list
							SC_AssetUpdateInfo.AssetUpdate assetinfo = new SC_AssetUpdateInfo.AssetUpdate();

							assetinfo.filename = update.filename;
							assetinfo.compressedLength = cached.data.Length;

							cache.Add(assetinfo);
						}

						//Send off our info list!
						SC_AssetUpdateInfo info = new SC_AssetUpdateInfo();
						info.updates = cache;

						player._client.sendReliable(info);
					}
				}
			);
		}

		/// <summary>
		/// Triggered when the client is attempting to enter the game and sends his security reply
		/// </summary>
		static public void Handle_CS_StartUpdate(CS_StartUpdate pkt, Player player)
		{	//If the player isn't logged in, ignore
			if (!player._bLoggedIn)
			{	//Log and abort
				Log.write(TLog.Warning, "Player {0} sent asset send request while not logged in.", player);
				player.disconnect();
				return;
			}

			//Find the matching cached asset
			AssetManager.Cache.CachedAsset cached = player._server._assets.AssetCache[pkt.filename];
			if (cached == null)
			{	//Strange
				Log.write(TLog.Warning, "Unable to find cache for requested asset '{0}'", pkt.filename);
				return;
			}

			//Prepare a packet
			SC_AssetUpdate update = new SC_AssetUpdate();

			update.compressedData = cached.data;
			update.uncompressedLen = cached.uncompressedSize;
			update.filename = System.IO.Path.GetFileName(cached.filepath);

			player._client.sendReliable(update);
		}

		/// <summary>
		/// Registers all handlers
		/// </summary>
		[Logic.RegistryFunc]
		static public void Register()
		{
			CS_RequestUpdate.Handlers += Handle_CS_RequestUpdate;
			CS_StartUpdate.Handlers += Handle_CS_StartUpdate;
		}
	}
}
