using System;
using System.Diagnostics;

using Tektosyne;
using Tektosyne.Geometry;

namespace Hexkit.World {

    /// <summary>
    /// Provides a weak reference to a <see cref="Site"/>.</summary>
    /// <remarks><para>
    /// <b>SiteReference</b> encapsulates the unique map coordinates of a <see cref="Site"/> object,
    /// together with a weak reference to the object itself.
    /// </para><para>
    /// Use <b>SiteReference</b> instances rather than direct references to identify map sites
    /// across different deep copies of an underlying <see cref="WorldState"/>. Such copies are
    /// created by computer player algorithms and by interactive game replays. Weak references allow
    /// the garbage collector to delete <b>Site</b> instances that belong to obsolete
    /// <b>WorldState</b> copies.</para></remarks>

    public struct SiteReference {
        #region SiteReference(Site)

        /// <overloads>
        /// Initializes a new instance of the <see cref="SiteReference"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="SiteReference"/> class with the specified
        /// <see cref="Site"/>.</summary>
        /// <param name="site">
        /// The initial value for the <see cref="Value"/> property.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="site"/> is a null reference.</exception>
        /// <remarks>
        /// The <see cref="Location"/> property is set to the <see cref="Site.Location"/> component
        /// of the specified <paramref name="site"/>.</remarks>

        public SiteReference(Site site) {
            if (site == null)
                ThrowHelper.ThrowArgumentNullException("site");

            this._location = site.Location;
            this._reference = new WeakReference(site, false);
        }

        #endregion
        #region SiteReference(PointI)

        /// <summary>
        /// Initializes a new instance of the <see cref="SiteReference"/> class with the specified
        /// map location.</summary>
        /// <param name="location">
        /// The initial value for the <see cref="Location"/> property.</param>
        /// <remarks>
        /// The <see cref="Value"/> property is set to a null reference.</remarks>

        public SiteReference(PointI location) {
            this._location = location;
            this._reference = null;
        }

        #endregion
        #region SiteReference(Tag)

        /// <summary>
        /// Initializes a new instance of the <see cref="SiteReference"/> class with invalid values.
        /// </summary>
        /// <param name="tag">
        /// A dummy parameter to identify this constructor.</param>
        /// <remarks>
        /// Please refer to <see cref="Invalid"/> for details.</remarks>

        private SiteReference(Tag tag) {
            this._location = Site.InvalidLocation;
            this._reference = null;
        }

        #endregion
        #region Private Fields

        // dummy enumeration for private constructor
        private enum Tag { Default }

        // property backers
        private readonly PointI _location;
        private WeakReference _reference;

        #endregion
        #region Invalid

        /// <summary>
        /// Represents an invalid <see cref="SiteReference"/>.</summary>
        /// <remarks>
        /// <b>Invalid</b> holds a <see cref="SiteReference"/> whose <see cref="Location"/> is <see
        /// cref="Site.InvalidLocation"/> and whose <see cref="Value"/> is a null reference.
        /// </remarks>

        public static readonly SiteReference Invalid = new SiteReference(Tag.Default);

        #endregion
        #region Location

        /// <summary>
        /// Gets the map coordinates of the wrapped <see cref="Site"/>.</summary>
        /// <value>
        /// The <see cref="Site.Location"/> component of the wrapped <see cref="Value"/>. The
        /// default is <see cref="PointI.Empty"/>.</value>
        /// <remarks>
        /// This property never changes once the instance has been constructed.</remarks>

        public PointI Location {
            [DebuggerStepThrough]
            get { return this._location; }
        }

        #endregion
        #region Value

        /// <summary>
        /// Gets or sets the wrapped <see cref="Site"/>.</summary>
        /// <value>
        /// The <see cref="Site"/> wrapped by this <see cref="SiteReference"/> instance. The default
        /// is a null reference.</value>
        /// <exception cref="ArgumentException">
        /// The property is set, and <see cref="Location"/> does not equal the <see
        /// cref="Site.Location"/> component of the new value.</exception>
        /// <exception cref="ArgumentNullException">
        /// The property is set to a null reference.</exception>
        /// <remarks>
        /// <b>Value</b> returns a null reference if the wrapped <see cref="Site"/> has been
        /// garbage-collected.</remarks>

        public Site Value {
            [DebuggerStepThrough]
            get {
                if (this._reference == null)
                    return null;

                // Target is null after garbage collection
                return (Site) this._reference.Target;
            }
            set {
                if (value == null)
                    ThrowHelper.ThrowArgumentNullException("value");

                if (value.Location != Location)
                    ThrowHelper.ThrowArgumentExceptionWithFormat(
                        "value", Tektosyne.Strings.ArgumentPropertyInvalid, "Location");

                this._reference = new WeakReference(value, false);
            }
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="SiteReference"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="String"/> containing the value of the <see cref="Location"/> property, with
        /// each coordinate formatted to three digits, separated by commas and surrounded by
        /// parentheses.</returns>
        /// <remarks>
        /// <b>ToString</b> returns the result of <see cref="Site.Format"/> for the current value of
        /// the <see cref="Location"/> property.</remarks>

        public override string ToString() {
            return Site.Format(Location);
        }

        #endregion
    }
}
