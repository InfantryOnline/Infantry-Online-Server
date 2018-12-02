using System.Collections;
using System.Collections.Generic;

namespace InfMapEditor.Rendering.Spatial
{
    /// <summary>
    /// A  grid is a simple spatial partitioner that uses a 2D array. It has O(1) search and O(1) insertion time.
    /// </summary>
    /// <remarks>
    /// Note: Everything is dealt in Grid Coordinates. Use the PixelsToGridCoordinates/GridCoordinatesToPixels
    /// Note: to convert between the two.
    /// </remarks>
    internal class Grid
    {
        /// <summary>
        /// The unit of a grid is a cell. Each cell can have an object placed inside of it, accessible through the Object property.
        /// </summary>
        public class GridCell
        {
            /// <summary>
            /// Gets or sets the x-coordinate of this cell in grid coordinates.
            /// </summary>
            public int X { get; set; }

            /// <summary>
            /// Gets or sets the y-coordinate of this cell in grid coordinates.
            /// </summary>
            public int Y { get; set; }

            /// <summary>
            /// Gets or sets the object inside of this cell.
            /// </summary>
            public CellData Data { get; set; }
        }

        /// <summary>
        /// GridRange permits accessing a subset of the grid by iterating over the cells within the range.
        /// </summary>
        internal class GridRange : IEnumerable<GridCell>
        {
            public GridRange(int x0, int y0, int x1, int y1, int stepX, int stepY, Grid grid)
            {
                this.x0 = x0;
                this.y0 = y0;
                this.x1 = x1;
                this.y1 = y1;
                this.grid = grid;
                this.stepX = stepX;
                this.stepY = stepY;

                if (x0 > x1)
                    this.stepX = -stepX;

                if (y0 > y1)
                    this.stepY = -stepY;
            }

            /// <summary>
            /// Returns the iterator for the range.
            /// </summary>
            /// <returns></returns>
            IEnumerator<GridCell> IEnumerable<GridCell>.GetEnumerator()
            {
                for (var x = x0; x != x1; x += stepX)
                {
                    for (var y = y0; y != y1; y += stepY)
                    {
                        yield return grid.cells[y][x];
                    }
                }
            }

            /// <summary>
            /// Returns the iterator for this range.
            /// </summary>
            /// <returns></returns>
            public IEnumerator GetEnumerator()
            {
                for (var x = x0; x != x1; x += stepX)
                {
                    for (var y = y0; y != y1; y += stepY)
                    {
                        yield return grid.cells[y][x];
                    }
                }
            }

            private readonly int x0, y0, x1, y1;
            private readonly int stepX, stepY;
            private readonly Grid grid;
        }

        /// <summary>
        /// Number of pixels that span one square cell.
        /// </summary>
        public static int PixelsPerCell = 16;

        /// <summary>
        /// Gets the width of this grid.
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Gets the height of this grid.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// Returns the (x,y) grid coordinates given an (x,y) in pixels.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>(x,y) in an array, or null if outside of the grid bounds</returns>
        /// <remarks>
        /// Note: The result will be snapped to the nearest cell.
        /// </remarks>
        public int[] PixelsToGridCoordinates(int x, int y)
        {
            int outX = x/PixelsPerCell;
            int outY = y/PixelsPerCell;

            if (outX < 0 || outY < 0 || outX > Width || outY > Height)
                return null;

            return new[] {outX, outY};
        }

        /// <summary>
        /// Returns the (x,y) pixel coordinates given an (x,y) in grid coordinates.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>(x,y) in an array, or null if outside of the grid bounds</returns>
        /// <remarks>
        /// Note: The result will be a multiple of the cell size.
        /// </remarks>
        public int[] GridCoordinatesToPixels(int x, int y)
        {
            if (x < 0 || y < 0 || x > Width || y > Height)
                return null;

            return new[] {x*PixelsPerCell, y*PixelsPerCell};
        }

        /// <summary>
        /// Creates a new grid.
        /// </summary>
        /// <param name="width">Total width of the grid</param>
        /// <param name="height">Total height of the grid</param>
        public Grid(int width, int height)
        {
            cells = new List<List<GridCell>>();
            Width = width;
            Height = height;

            for(var y = 0; y < height; y++)
            {
                cells.Add(new List<GridCell>());

                for(var x = 0; x < width; x++)
                {
                    cells[y].Add(new GridCell());

                    cells[y][x].Data = null;
                    cells[y][x].Y = y;
                    cells[y][x].X = x;
                }
            }
        }

        /// <summary>
        /// Insert an object into the cell at grid coordinates (x,y).
        /// </summary>
        /// <returns>The object that was there before; or null if no previous object existed.</returns>
        public CellData Insert(CellData data, int x, int y)
        {
            CellData oldData = cells[y][x].Data;

            cells[y][x].Data = data;

            return oldData;
        }

        /// <summary>
        /// Return an object located in the cell at grid coordinates (x,y).
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public CellData Get(int x, int y)
        {
            return cells[y][x].Data;
        }

        /// <summary>
        /// Returns all the objects within the given range, given in grid coordinates.
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="stepX"></param>
        /// <param name="stepY"></param>
        /// <returns></returns>
        public GridRange GetRange(int x0, int y0, int x1, int y1, int stepX = 1, int stepY = 1)
        {
            return new GridRange(x0, y0, x1, y1, stepX, stepY, this);
        }

        /// <summary>
        /// Returns all the objects within the given range, given in two pairs of grid coordinates.
        /// </summary>
        /// <param name="begin">(x,y) of the starting cell.</param>
        /// <param name="end">(x,y) of the ending cell.</param>
        /// <param name="stepX"></param>
        /// <param name="stepY"></param>
        /// <returns></returns>
        public GridRange GetRange(int[] begin, int[] end, int stepX = 1, int stepY = 1)
        {
            return GetRange(begin[0], begin[1], end[0], end[1], stepX, stepY);
        }

        /// <summary>
        /// All the cells of this grid.
        /// </summary>
        private readonly List<List<GridCell>> cells;
    }
}
