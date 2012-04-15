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
	{
		/// <summary>
		/// Keeps track and provides easy access of all lio data
		/// </summary>
		public class Lio
		{	// Member variables
			///////////////////////////////////////////////////
			private AssetManager _manager;
			private Dictionary<int, List<LioInfo.WarpField>> _warpGroups;


			///////////////////////////////////////////////////
			// Properties
			///////////////////////////////////////////////////
			/// <summary>		
			/// Returns the list of switch objects
			/// </summary>
			public IEnumerable<LioInfo.Switch> Switches
			{
				get
				{
					return _manager._idToLio.Values.OfType<LioInfo.Switch>();
				}
			}

            /// <summary>
            /// Returns the list of door objects
            /// </summary>
            public IEnumerable<LioInfo.Door> Doors
            {
                get
                {
                    return _manager._idToLio.Values.OfType<LioInfo.Door>();
                }
            }

			/// <summary>		
			/// Returns the list of flag objects
			/// </summary>
			public IEnumerable<LioInfo.Flag> Flags
			{
				get
				{
					return _manager._idToLio.Values.OfType<LioInfo.Flag>();
				}
			}

			/// <summary>		
			/// Returns the list of flag objects
			/// </summary>
			public IEnumerable<LioInfo.Hide> Hides
			{
				get
				{
					return _manager._idToLio.Values.OfType<LioInfo.Hide>();
				}
			}


			///////////////////////////////////////////////////
			// Member Functions
			///////////////////////////////////////////////////
			/// <summary>		
			/// Resolves all Lio information
			/// </summary>
			public Lio(AssetManager manager)
			{
				_manager = manager;

				//Resolve all warp groups
				_warpGroups = new Dictionary<int, List<LioInfo.WarpField>>();
				IEnumerable<LioInfo.WarpField> warps = _manager._idToLio.Values.OfType<LioInfo.WarpField>();

				//Sort them
				foreach (LioInfo.WarpField warp in warps)
				{
					List<LioInfo.WarpField> warpGroup;

					if (!_warpGroups.TryGetValue(warp.WarpFieldData.WarpGroup, out warpGroup))
					{	//Create a new group
						warpGroup = new List<LioInfo.WarpField>();
						_warpGroups.Add(warp.WarpFieldData.WarpGroup, warpGroup);
					}

					warpGroup.Add(warp);
				}
			}

			/// <summary>
			/// Gets the list of warps in a particular group
			/// </summary>		
			public List<LioInfo.WarpField> getWarpGroupByID(int id)
			{	//TODO: Implement -1 all-warp
				List<LioInfo.WarpField> warps;

				if (!_warpGroups.TryGetValue(id, out warps))
					return null;

				return warps;
			}
		}
	}
}
