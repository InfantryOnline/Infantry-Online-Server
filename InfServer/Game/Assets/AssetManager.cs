using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Assets;

using InfServer.Protocol;

namespace InfServer.Game
{
	/// <summary>
	/// Keeps track of files that a zone uses and their checksums
	/// </summary>
	public partial class AssetManager
	{	// Member variables
		///////////////////////////////////////////////////
		static private AssetManager _singleton;

		private List<AssetInfo> _assetList;
		private List<string> _bloList;
		

		private SortedDictionary<string, ItemInfo> _nameToItem;
		private SortedDictionary<string, SkillInfo> _nameToSkill;
	
		private SortedDictionary<int, ItemInfo> _idToItem;
		private SortedDictionary<int, SkillInfo> _idToSkill;
		private SortedDictionary<int, VehInfo> _idToVehicle;
		private SortedDictionary<int, LioInfo> _idToLio;

		public bool bUseBlobs = true;

		public Cache AssetCache
		{
			get;
			set;
		}

		public Lio Lios
		{
			get;
			set;
		}

		public LvlInfo Level
		{
			get;
			set;
		}

		static public AssetManager Manager
		{
			get
			{
				return _singleton;
			}
		}

		///////////////////////////////////////////////////
		// Member Functions
		///////////////////////////////////////////////////
		/// <summary>
		/// Loads all game assets
		/// </summary>		
		public bool load(CfgInfo zoneConf, string configFilename)
		{
			_singleton = this;

			_assetList = new List<AssetInfo>();
	
			//Add the game config
			addAssetData(AssetFileFactory.CreateFromFile<AssetFile>(configFilename));

			//Load shit up
			ItemFile itms = AssetFileFactory.CreateFromFile<ItemFile>(zoneConf.level.itmFile);
			LioFile lios = AssetFileFactory.CreateFromFile<LioFile>(zoneConf.level.lioFile);
			SkillFile rpgs = AssetFileFactory.CreateFromFile<SkillFile>(zoneConf.level.rpgFile);
			VehicleFile vehs = AssetFileFactory.CreateFromFile<VehicleFile>(zoneConf.level.vehFile);
			LevelFile lvl = AssetFileFactory.CreateFromFile<LevelFile>(zoneConf.level.lvlFile);

			if (itms == null || lios == null || rpgs == null || vehs == null || lvl == null)
			{	//Missing a core file
				foreach (string missing in AssetFileFactory._missingFiles)
					Log.write(TLog.Error, "Missing file: {0}", missing);
				return false;
			}

			Level = lvl.Data;

			//Save checksums to send to clients
			addAssetData(itms);
			addAssetData(lios);
			addAssetData(rpgs);
			addAssetData(vehs);
			addAssetData(lvl);

			//Make item maps
			_idToItem = new SortedDictionary<int, ItemInfo>();
			_nameToItem = new SortedDictionary<string, ItemInfo>();
			foreach (ItemInfo it in itms.Data)
				_idToItem.Add(it.id, it);
			foreach (ItemInfo it in itms.Data)
				if (!_nameToItem.ContainsKey(it.name.ToLower()))
					_nameToItem.Add(it.name.ToLower(), it);

			//Make skill maps
			_idToSkill = new SortedDictionary<int, SkillInfo>();
			_nameToSkill = new SortedDictionary<string, SkillInfo>();
			foreach (SkillInfo it in rpgs.Data)
				_idToSkill.Add(it.SkillId, it);
			foreach (SkillInfo it in rpgs.Data)
				if (!_nameToSkill.ContainsKey(it.Name.ToLower()))
					_nameToSkill.Add(it.Name.ToLower(), it);

			//Make lio map
			_idToLio = new SortedDictionary<int, LioInfo>();
			foreach (LioInfo it in lios.Data)
				_idToLio.Add(it.GeneralData.Id, it);

			//Load blo files
			_bloList = new List<string>();

			foreach (string file in ItemInfo.BlobsToLoad)
				loadBloFile(file + ".blo");
			foreach (LvlInfo.BlobReference blob in Level.ObjectBlobs)
				loadBloFile(blob.FileName);
			foreach (LvlInfo.BlobReference blob in Level.FloorBlobs)
				loadBloFile(blob.FileName);

            //Make vehicle map
            _idToVehicle = new SortedDictionary<int, VehInfo>();
            foreach (VehInfo it in vehs.Data)
            {
                _idToVehicle.Add(it.Id, it);
                foreach (string blo in it.blofiles)
                    loadBloFile(blo + ".blo");
            }
			foreach (string blo in _bloList)
			{	//Attempt to load it
				BloFile blof = AssetFileFactory.CreateFromFile<BloFile>(blo);
				if (blof != null)
					addAssetData(blof);
			}

			//Initialize our lio data
			Lios = new Lio(this);

            //Load news file
            AssetFile nws = AssetFileFactory.CreateFromFile<AssetFile>(zoneConf.level.nwsFile);
            if (nws != null)
                addAssetData(nws);

            //Load the helpMenu files
            for (int i = 0; i <= zoneConf.helpMenu.links.Count - 1; i++)
            {             
                AssetFile helpMenu = AssetFileFactory.CreateFromFile<AssetFile>(zoneConf.helpMenu.links[i].name);
                if (helpMenu != null)
                    addAssetData(helpMenu);
            }

			//Load everything else
			loadAdditionalAssets();

			//Are we missing files?
			if (AssetFileFactory.IsIncomplete)
			{	//Missing a core file
				foreach (string missing in AssetFileFactory._missingFiles)
					Log.write(TLog.Error, "Missing file: {0}", missing);
				return false;
			}

			//Cache all our assets
			AssetCache = new Cache(this);
			AssetCache.prepareCache(_assetList);
			return true;
		}

		/// <summary>
		/// Attempts to load an additional blo file
		/// </summary>		
		public void loadBloFile(string filename)
		{	//Should we be loading blo files?
			if (!bUseBlobs)
				return;

			//None == nothing
			if (filename == null || filename == "")
				return;

			string lower = filename.ToLower();
			if (lower == "none.blo")
				return;

            if (lower == ".blo")
                return;

            if (lower == "nosound.blo")
                return;

			//Make sure we don't already have this blo file
			if (_bloList.Contains(lower))
				return;

			_bloList.Add(lower);
		}

        /// <summary>
        /// Loads additional assets specified by assets.xml
        /// </summary>		
        public void loadAdditionalAssets()
        {	//Load up our assets config file
            ConfigSetting cfg = new Xmlconfig("assets.xml", false).Settings;
            foreach (ConfigSetting asset in cfg.GetNamedChildren("asset"))
            {	//Insert it into the asset list
                AssetFile assetf = AssetFileFactory.CreateFromGlobalFile<AssetFile>(asset.Value);

                if (assetf != null)
                    addAssetData(assetf);
            }
        }

		/// <summary>
		/// Gets the ItemInfo of a particular ID
		/// </summary>		
		public ItemInfo getItemByID(int id)
		{
			ItemInfo item;

			if (!_idToItem.TryGetValue(id, out item))
				return null;

			return item;
		}

		/// <summary>
		/// Gets the ItemInfo of a particular name
		/// </summary>		
		public ItemInfo getItemByName(string name)
		{
			ItemInfo item;

			if (!_nameToItem.TryGetValue(name.ToLower(), out item))
				return null;

			return item;
		}

		/// <summary>
		/// Gets the SkillInfo of a particular ID
		/// </summary>		
		public SkillInfo getSkillByID(int id)
		{
			SkillInfo skill;

			if (!_idToSkill.TryGetValue(id, out skill))
				return null;

			return skill;

		}
		/// <summary>
		/// Gets the SkillInfo of a particular name
		/// </summary>		
		public SkillInfo getSkillByName(string name)
		{
			SkillInfo skill;

			if (!_nameToSkill.TryGetValue(name.ToLower(), out skill))
				return null;

			return skill;
		}

		/// <summary>
		/// Gets the VehInfo of a particular ID
		/// </summary>		
		public VehInfo getVehicleByID(int id)
		{
			VehInfo veh;

			if (!_idToVehicle.TryGetValue(id, out veh))
				return null;
			return veh;
		}

		/// <summary>
		/// Gets the LioInfo of a particular ID
		/// </summary>		
		public LioInfo getLioByID(int id)
		{
			LioInfo lio;

			if (!_idToLio.TryGetValue(id, out lio))
				return null;
			return lio;
		}

		/// <summary>
		/// Loads in an asset file and saves the checksum
		/// </summary>		
		private void addAssetData(AbstractAsset abstractAsset)
		{
		    AssetInfo newAsset = new AssetInfo(abstractAsset.Filename, abstractAsset.Checksum);

            if (_assetList.Contains(newAsset) == false)
			_assetList.Add(newAsset);
		}

		/// <summary>
		/// Gets the loaded asset list to send to a client
		/// </summary>		
		public List<AssetInfo> getAssetList()
		{
			return _assetList;
		}

	}
}
