using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Tektosyne;
using Tektosyne.Geometry;
using Tektosyne.Windows;
using Hexkit.Scenario;

namespace Hexkit.Editor {

    using ImageFilePair = KeyValuePair<String, ImageFile>;

    /// <summary>
    /// Checks all bitmap tiles defined by the scenario for missing and duplicate definitions.
    /// </summary>
    /// <remarks>
    /// <b>TileChecker</b> is an experimental class that is not currently used by Hexkit. The image
    /// file parameters for DeBray Bailey's tile set is hardcoded into the class, and all output is
    /// written to the default <see cref="Trace"/> listener.</remarks>

    public class TileChecker {
        #region Private Fields

        // collection of all references to all tiles
        private readonly List<UniqueTile> _tiles = new List<UniqueTile>();

        #endregion
        #region Check

        /// <summary>
        /// Checks all bitmap tiles defined by the scenario for missing and duplicate definitions.
        /// </summary>

        public void Check() {
            CollectTiles();
            CollectFrames();
        }

        #endregion
        #region Private Methods
        #region CollectFrames

        /// <summary>
        /// Associates all <see cref="ImageFrame"/> objects with the corresponding <see
        /// cref="UniqueTile"/> objecs.</summary>

        private void CollectFrames() {

            // iterate over all frames defined for any image
            foreach (EntityImage image in MasterSection.Instance.Images.Collection.Values)
                foreach (ImageFrame frame in image.Frames) {

                    // check against all frames defined for any tile
                    foreach (UniqueTile tile in this._tiles)
                        foreach (IndexFrame indexFrame in tile.FileFrames) {
                            ImageFrame tileFrame = indexFrame.Frame;

                            if (tileFrame.Source.Key == frame.Source.Key &&
                                tileFrame.Bounds == frame.Bounds) {
                                tile.Images.Add(image);
                                goto nextFrame;
                            }
                        }

                nextFrame:
                    continue;
                }

            Trace.WriteLine("\nCollecting Frames\n-----------------\n");

            foreach (UniqueTile tile in this._tiles) {
                if (tile.Images.Count == 0) {
                    Trace.Write("Unreferenced: ");

                    foreach (IndexFrame indexFrame in tile.FileFrames)
                        Trace.Write(String.Format(CultureInfo.InvariantCulture,
                            "{0} #{1}, ", indexFrame.Frame.Source.Key, indexFrame.Index));

                    Trace.WriteLine("");
                }
                else if (tile.Images.Count > 1) {
                    Trace.Write("Multi-referenced: ");

                    foreach (EntityImage image in tile.Images)
                        Trace.Write(String.Format(CultureInfo.InvariantCulture, "{0}, ", image.Id));

                    Trace.WriteLine("");
                }
            }
        }

        #endregion
        #region CollectTiles

        /// <summary>
        /// Associates all non-empty <see cref="IndexFrame"/> objects with the corresponding <see
        /// cref="UniqueTile"/> objects.</summary>

        private void CollectTiles() {
            this._tiles.Clear();

            // fixed values for DeBray Bailey's tileset
            SizeI frameSize = new SizeI(24, 35);
            PointI offset = new PointI(2, 1);
            PointI spacing = new PointI(3, 3);
            const int columns = 20;

            Trace.WriteLine("\nCollecting Tiles\n----------------");
            foreach (ImageFile file in MasterSection.Instance.Images.ImageFiles.Values) {

                // skip river & road files that were designed for hexagons
                if (file.Id == "file-rivers" || file.Id == "file-roads")
                    continue;

                // compute number of tile rows
                WriteableBitmap bitmap = file.Bitmap;
                int rows = (bitmap.PixelHeight - offset.Y) / (frameSize.Height + spacing.Y) + 1;
                Trace.WriteLine(String.Format(CultureInfo.InvariantCulture,
                    "\nFile {0} contains {1} tile rows.", file.Id, rows));

                for (int y = 0; y < rows; y++) {
                    // sanity check for excessive row count
                    int frameTop = offset.Y + y * (frameSize.Height + spacing.Y);
                    if (frameTop + frameSize.Height > bitmap.Height)
                        continue;

                    for (int x = 0; x < columns; x++) {
                        // sanity check for excessive column count
                        int frameLeft = offset.X + x * (frameSize.Width + spacing.X);
                        if (frameLeft + frameSize.Width > bitmap.Width)
                            continue;

                        // create ImageFrame with current source & bounds
                        ImageFrame frame = new ImageFrame();
                        frame.Bounds = new RectI(
                            frameLeft, frameTop, frameSize.Width, frameSize.Height);
                        frame.Source = new ImageFilePair(file.Id, file);

                        // create IndexFrame with current frame & index
                        int index = y * columns + x;
                        IndexFrame indexFrame = new IndexFrame(frame, index);

                        // check for empty bitmap tile
                        if (IsTileEmpty(frame)) {
                            Trace.WriteLine(String.Format(CultureInfo.InvariantCulture,
                                "Empty tile: {0} #{1}", file.Id, index));
                            continue;
                        }

                        // check for duplicate bitmap tiles
                        foreach (UniqueTile oldTile in this._tiles) {
                            IndexFrame oldFrame = oldTile.FileFrames[0];
                            if (AreTilesEqual(frame, oldFrame.Frame)) {

                                Trace.WriteLine(String.Format(CultureInfo.InvariantCulture,
                                    "Duplicates: {0} #{1} = {2} #{3}", file.Id, index,
                                    oldFrame.Frame.Source.Key, oldFrame.Index));

                                oldTile.FileFrames.Add(indexFrame);
                                goto nextTile;
                            }
                        }

                        // add a new unique bitmap tile
                        UniqueTile tile = new UniqueTile();
                        tile.FileFrames.Add(indexFrame);
                        this._tiles.Add(tile);

                    nextTile:
                        continue;
                    }
                }
            }
        }

        #endregion
        #region AreTilesEqual

        /// <summary>
        /// Indicates whether the two specified <see cref="ImageFrame"/> objects define identical
        /// bitmap tiles.</summary>
        /// <param name="firstFrame">
        /// The first <see cref="ImageFrame"/> to compare.</param>
        /// <param name="secondFrame">
        /// The second <see cref="ImageFrame"/> to compare.</param>
        /// <returns>
        /// <c>true</c> if all pixels in the specified <paramref name="firstFrame"/> contain the
        /// same <see cref="Color"/> values as the corresponding pixels in the specified <paramref
        /// name="secondFrame"/>; otherwise, <c>false</c>.</returns>

        private static bool AreTilesEqual(ImageFrame firstFrame, ImageFrame secondFrame) {

            WriteableBitmap bitmap1 = firstFrame.Source.Value.Bitmap;
            WriteableBitmap bitmap2 = secondFrame.Source.Value.Bitmap;
            RectI bounds1 = firstFrame.Bounds, bounds2 = secondFrame.Bounds;

            if (bounds1.Size != bounds2.Size)
                return false;

            for (int x = 0; x < bounds1.Width; x++)
                for (int y = 0; y < bounds1.Height; y++)
                    if (bitmap1.GetPixel(x + bounds1.X, y + bounds1.Y) !=
                        bitmap2.GetPixel(x + bounds2.X, y + bounds2.Y))
                        return false;

            return true;
        }

        #endregion
        #region IsTileEmpty

        /// <summary>
        /// Indicates whether the specified <see cref="ImageFrame"/> defines an empty bitmap tile.
        /// </summary>
        /// <param name="frame">
        /// The <see cref="ImageFrame"/> to examine.</param>
        /// <returns>
        /// <c>true</c> if all pixels in the specified <paramref name="frame"/> contain the same
        /// <see cref="Color"/> value; otherwise, <c>false</c>.</returns>

        private static bool IsTileEmpty(ImageFrame frame) {

            WriteableBitmap bitmap = frame.Source.Value.Bitmap;
            RectI bounds = frame.Bounds;
            Color color = bitmap.GetPixel(bounds.X, bounds.Y);

            for (int x = bounds.Left; x < bounds.Right; x++)
                for (int y = bounds.Top; y < bounds.Bottom; y++)
                    if (color != bitmap.GetPixel(x, y))
                        return false;

            return true;
        }

        #endregion
        #endregion
        #region Struct IndexFrame

        /// <summary>
        /// Associates an <see cref="ImageFrame"/> with its tile index in the containing <see
        /// cref="ImageFile"/>.</summary>

        private struct IndexFrame {
            #region Internal Constructor

            /// <summary>
            /// Initializes a new instance of the <see cref="IndexFrame"/> class with the specified
            /// image frame and tile index.</summary>
            /// <param name="frame">
            /// The initial value for the <see cref="Frame"/> property.</param>
            /// <param name="index">
            /// The initial value for the <see cref="Index"/> property.</param>
            /// <exception cref="ArgumentNullException">
            /// <paramref name="frame"/> is a null reference.</exception>
            /// <exception cref="ArgumentOutOfRangeException">
            /// <paramref name="index"/> is less than zero.</exception>

            internal IndexFrame(ImageFrame frame, int index) {
                if (frame == null)
                    ThrowHelper.ThrowArgumentNullException("frame");
                if (index < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(
                        "index", index, Tektosyne.Strings.ArgumentNegative);

                this._frame = frame;
                this._index = index;
            }

            #endregion
            #region Private Fields

            // property backers
            private readonly ImageFrame _frame;
            private readonly int _index;

            #endregion
            #region Frame

            /// <summary>
            /// Gets the <see cref="ImageFrame"/> with the associated <see cref="Index"/>.</summary>
            /// <value>
            /// The <see cref="ImageFrame"/> with the associated <see cref="Index"/>.</value>

            internal ImageFrame Frame {
                [DebuggerStepThrough]
                get { return this._frame; }
            }

            #endregion
            #region Index

            /// <summary>
            /// Gets the tile index of the associated <see cref="Frame"/>.</summary>
            /// <value>
            /// The tile index of the associated <see cref="Frame"/>.</value>

            internal int Index {
                [DebuggerStepThrough]
                get { return this._index; }
            }

            #endregion
        }

        #endregion
        #region Class UniqueTile

        /// <summary>
        /// Associates all distinct <see cref="ImageFrame"/> objects that define the same bitmap
        /// tile with all <see cref="EntityImage"/> objects that reference any one of them.</summary>

        private class UniqueTile {
            #region Private Fields

            // property backers
            private readonly List<IndexFrame> _fileFrames = new List<IndexFrame>(2);
            private readonly List<EntityImage> _images = new List<EntityImage>(2);

            #endregion
            #region FileFrames

            /// <summary>
            /// Gets a list of all distinct <see cref="IndexFrame"/> objects that define the <see
            /// cref="UniqueTile"/>.</summary>
            /// <value>
            /// A <see cref="List{T}"/> of all distinct <see cref="IndexFrame"/> objects that define
            /// the <see cref="UniqueTile"/>. The default is an empty collection.</value>

            internal List<IndexFrame> FileFrames {
                [DebuggerStepThrough]
                get { return this._fileFrames; }
            }

            #endregion
            #region Images

            /// <summary>
            /// Gets a list of all distinct <see cref="EntityImage"/> objects that reference the
            /// <see cref="UniqueTile"/>.</summary>
            /// <value>
            /// A <see cref="List{T}"/> of all distinct <see cref="EntityImage"/> objects that
            /// contain the <see cref="UniqueTile"/>. The default is an empty collection.</value>

            internal List<EntityImage> Images {
                [DebuggerStepThrough]
                get { return this._images; }
            }

            #endregion
        }

        #endregion
    }
}
