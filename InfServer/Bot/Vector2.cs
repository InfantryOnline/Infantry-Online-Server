using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InfServer.Game;
using InfServer.Protocol;

using Assets;

namespace InfServer.Bots
{
	public class Vector2
	{
		public double x, y;
		public Vector2()
		{
			x = 0;
			y = 0;
		}
		public Vector2(short x, short y)
		{
			this.x = x;
			this.y = y;
		}
		public Vector2(double x, double y)
		{
			this.x = x;
			this.y = y;
		}
		public double magnitude()
		{
			return Math.Sqrt((x * x) + (y * y));
		}
		public void setMagnitude(int magnitude)
		{
			double difference = magnitude / this.magnitude();
			this.x *= difference;
			this.y *= difference;
		}
		public void normalise()
		{
			double dist = Math.Sqrt((x * x) + (y * y));
			x *= (1.0 / dist);
			y *= (1.0 / dist);
		}
		public void multiply(double multiplier)
		{
			x *= multiplier;
			y *= multiplier;
		}
		public static Vector2 createUnitVector(double direction)
		{
			direction *= 1.5;
			direction = direction * (Math.PI / 180);
			double x = Math.Sin(direction);
			double y = -Math.Cos(direction);
			return new Vector2(x, y);
		}

	}
}