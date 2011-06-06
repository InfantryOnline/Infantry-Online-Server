using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Network;
using InfServer.Game;

using Assets;

namespace InfServer.Protocol
{	/// <summary>
	/// Provides a series of helpful mathematical functions
	/// </summary>
	public partial class Helpers
	{	///////////////////////////////////////////////////
		// Member Functions
		//////////////////////////////////////////////////
		/// <summary>
		/// Performs a simple square-range check
		/// </summary>
		static public bool isInRange(int range, ObjectState state1, ObjectState state2)
		{	//Is it?
			if (Math.Abs(state1.positionX - state2.positionX) > range)
				return false;
			if (Math.Abs(state1.positionY - state2.positionY) > range)
				return false;
			return true;
		}

		/// <summary>
		/// Performs a simple square-range check
		/// </summary>
		static public bool isInRange(int range, int x1, int y1, int x2, int y2)
		{	//Is it?
			if (Math.Abs(x1 - x2) > range)
				return false;
			if (Math.Abs(y1 - y2) > range)
				return false;
			return true;
		}

		/// <summary>
		/// Chooses a random location within a specified area
		/// </summary>
		static public void randomPositionInArea(Arena arena, int range, ref short posX, ref short posY)
		{	//Redirect
			randomPositionInArea(arena, ref posX, ref posY, (short)range, (short)range);
		}

		/// <summary>
		/// Calculates the angle between the first and second point
		/// </summary>
		static public void randomPositionInArea(Arena arena, ref short posX, ref short posY, short width, short height)
		{	//Define our upper and lower bounds
			int lowerX = posX - (width / 2);
			int higherX = posX + (width / 2);
			int lowerY = posY - (height / 2);
			int higherY = posY + (height / 2);

			//Clamp within the map coordinates
			int mapWidth = (arena._server._assets.Level.Width - 1) * 16;
			int mapHeight = (arena._server._assets.Level.Height - 1) * 16;

			lowerX = Math.Min(Math.Max(0, lowerX), mapWidth);
			higherX = Math.Min(Math.Max(0, higherX), mapWidth);
			lowerY = Math.Min(Math.Max(0, lowerY), mapHeight);
			higherY = Math.Min(Math.Max(0, higherY), mapHeight);

			//Randomly generate some coordinates!
			posX = (short)arena._rand.Next(lowerX, higherX);
			posY = (short)arena._rand.Next(lowerY, higherY);
		}

		/// <summary>
		/// Calculates the distance between two angles
		/// </summary>
		static public double calculateDifferenceInAngles(double angle1, double angle2)
		{
			double diff1 = angle2 - angle1;
			double diff2 = diff1 - 240;
			if (diff2 != (diff2 % 240))
			{
				diff2 %= 240;
				diff2 = 240 + diff2;
			}

			if (Math.Abs(diff1) < Math.Abs(diff2))
				return diff1;
			else
				return diff2;
		}

		/// <summary>
		/// Calculates the angle between the first and second point
		/// </summary>
		static public double calculateDegreesBetweenPoints(double x1, double y1, double x2, double y2)
		{
			double radians = Math.Atan2(-x2 - (-x1), y2 - y1);
			double degrees = radians * 180 / Math.PI;

			degrees = degrees / 1.5;
			if (degrees < 0)
				degrees = 240 + degrees;

			return degrees;
		}

		#region Distance functions
		/// <summary>
		/// Calculates the distance between two object states
		/// </summary>
		static public double distanceTo(Player a, Vehicle b)
		{
			int abx = a._state.positionX - b._state.positionX;
			int aby = a._state.positionY - b._state.positionY;
			return Math.Sqrt((abx * abx) + (aby * aby));
		}

		/// <summary>
		/// Calculates the distance between two object states
		/// </summary>
		static public double distanceTo(Vehicle a, Player b)
		{
			int abx = a._state.positionX - b._state.positionX;
			int aby = a._state.positionY - b._state.positionY;
			return Math.Sqrt((abx * abx) + (aby * aby));
		}

		/// <summary>
		/// Calculates the distance between two object states
		/// </summary>
		static public double distanceTo(Vehicle a, Vehicle b)
		{
			int abx = a._state.positionX - b._state.positionX;
			int aby = a._state.positionY - b._state.positionY;
			return Math.Sqrt((abx * abx) + (aby * aby));
		}

		/// <summary>
		/// Calculates the distance between two object states
		/// </summary>
		static public double distanceTo(ObjectState a, ObjectState b)
		{
			int abx = a.positionX - b.positionX;
			int aby = a.positionY - b.positionY;
			return Math.Sqrt((abx * abx) + (aby * aby));
		}

		/// <summary>
		/// Calculates the squared distance between two object states
		/// </summary>
		static public double distanceSquaredTo(ObjectState a, ObjectState b)
		{
			int abx = a.positionX - b.positionX;
			int aby = a.positionY - b.positionY;
			return (abx * abx) + (aby * aby);
		}
		#endregion

		/// <summary>
		/// Try to shoot the target if he's walking in a straight line.
		/// 
		/// Comes from solving (for theta) the system of equations
		/// R t = X + V t
		/// 
		/// giving
		/// r t cos \theta = x + v_x t
		/// r t sin \theta = y + v_y t
		/// 
		/// where r is projectile speed, and X and V are the player's initial position
		/// and velocity relative to the turret.
		/// </summary>	
		/// <param name="projSpeed">Speed of the projectile, measured in pixels per tick</param>
		static public byte computeLeadFireAngle(ObjectState state, ObjectState target, double projSpeed)
		{
			int now = Environment.TickCount;
			double ticksElapsed = (now - target.lastUpdate) / 10.0d;

			//Get the relative position of target at (x_1, x_2) where turret is at 0,0
			double x_1 = target.positionX - state.positionX;
			double x_2 = (target.positionY - state.positionY) / 0.7d;		//The Y coordinates are smaller than x, hence 0.7

			// Convert velocity (thousandths of pixels per tick) to pixels per tick
			double v_1 = target.velocityX / 1000.0d;
			double v_2 = target.velocityY / 1000.0d;

			// How many units of 10 ms have elapsed since the player last updated?
			// TODO: take into account player's ping since the lastUpdate time was when the server received it
			x_1 += v_1 * ticksElapsed;
			x_2 += v_2 * ticksElapsed;

			// Don't fire if the guy is on top of the turret
			if (x_1 == 0 && x_2 == 0)
				return (byte)calculateDegreesBetweenPoints(target.positionX, target.positionY, state.positionX, state.positionY);

			// Muzzle velocity = thousandths of pixels per tick, convert to pixels per tick
			double r = projSpeed;

			double a = ((v_1 * v_1) + (v_2 * v_2)) - (r * r);
			double b = 2 * ((x_1 * v_1) + (x_2 * v_2));
			double c = (x_1 * x_1) + (x_2 * x_2);

			double d = (b * b) - (4 * a * c);

			// Don't sqrt if discriminant is negative
			if (d < 0)
				return (byte)calculateDegreesBetweenPoints(target.positionX, target.positionY, state.positionX, state.positionY);

			double discQuotient = Math.Sqrt(d) / (2.0 * a);
			double fracFront = -b / (2.0 * a);

			// As above, t = units of pixels per tick (very small)
			double t0 = fracFront + discQuotient;
			double t1 = fracFront - discQuotient;
			double t;

			// Pick smallest positive root, if any
			if (t0 < 0 && t1 < 0)
				return (byte)calculateDegreesBetweenPoints(target.positionX, target.positionY, state.positionX, state.positionY);
			else if (t0 > 0 && t1 > 0)
				t = t0 < t1 ? t0 : t1;
			else
				t = t0 > 0 ? t0 : t1;

			double theta = Math.Atan2(x_2 + v_2 * t, x_1 + v_1 * t);

			double infdegrees = Math.Round(theta * (120 / Math.PI)) + 120 + 180;
			infdegrees = infdegrees % 240;
			return (byte)infdegrees;
		}

		/// <summary>
		/// Returns a list of tiles that intersect with the line segment going from
		/// (x0, y0) to (x1, y1).
		/// 
		/// Uses Bresenhem's Line Algorithm to get the tiles.
		/// http://en.wikipedia.org/wiki/Bresenham's_line_algorithm
		/// </summary>
		/// <param name="x0">x-component of one vertex</param>
		/// <param name="y0">y-component of one vertex</param>
		/// <param name="x1">x-component of the other vertex</param>
		/// <param name="y1">y-component of the other vertex</param>
		/// <returns>All tiles that intersect with the line segment</returns>
		static public List<LvlInfo.Tile> calcBresenhems(Arena arena, short x0, short y0, short x1, short y1)
		{	//Make sure we're dealing with just tile coordinates
			x0 /= 16;
			y0 /= 16;
			x1 /= 16;
			y1 /= 16;

			List<LvlInfo.Tile> tiles = new List<LvlInfo.Tile>();

			int dx = Math.Abs(x1 - x0);
			int dy = Math.Abs(y1 - y0);

			// Slope
			int sx = x0 < x1 ? 1 : -1;
			int sy = y0 < y1 ? 1 : -1;

			int err = dx - dy;

			while (true)
			{
				tiles.Add(arena._tiles[(y0 * arena._levelWidth) + x0]);

				if (x0 == x1 && y0 == y1)
					break;

				int e2 = 2 * err;

				if (e2 > -dy)
				{
					err -= dy;
					x0 += (short)sx;
				}
				if (e2 < dx)
				{
					err += dx;
					y0 += (short)sy;
				}
			}

			return tiles;
		}

		/// <summary>
		/// Returns a list of tiles that intersect with the line segment going from
		/// (x0, y0) to (x1, y1).
		/// 
		/// Uses Bresenhem's Line Algorithm to get the tiles.
		/// http://en.wikipedia.org/wiki/Bresenham's_line_algorithm
		/// </summary>
		/// <param name="x0">x-component of one vertex</param>
		/// <param name="y0">y-component of one vertex</param>
		/// <param name="x1">x-component of the other vertex</param>
		/// <param name="y1">y-component of the other vertex</param>
		/// <returns>All tiles that intersect with the line segment</returns>
		static public bool calcBresenhemsPredicate(Arena arena, short x0, short y0, short x1, short y1, Predicate<LvlInfo.Tile> predicate)
		{	//Make sure we're dealing with just tile coordinates
			x0 /= 16;
			y0 /= 16;
			x1 /= 16;
			y1 /= 16;

			int dx = Math.Abs(x1 - x0);
			int dy = Math.Abs(y1 - y0);

			// Slope
			int sx = x0 < x1 ? 1 : -1;
			int sy = y0 < y1 ? 1 : -1;

			int err = dx - dy;

			while (true)
			{
				if (!predicate(arena._tiles[(y0 * arena._levelWidth) + x0]))
					return false;

				if (x0 == x1 && y0 == y1)
					break;

				int e2 = 2 * err;

				if (e2 > -dy)
				{
					err -= dy;
					x0 += (short)sx;
				}
				if (e2 < dx)
				{
					err += dx;
					y0 += (short)sy;
				}
			}

			return true;
		}
	}
}
