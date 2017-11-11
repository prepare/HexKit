using System;
using System.Collections.Generic;
using System.Xml;

using Tektosyne;
using Tektosyne.Geometry;
using Hexkit.Global;

namespace Hexkit.World.Commands {

    /// <summary>
    /// Represents a command that operates on entities and a target site.</summary>
    /// <remarks><para>
    /// <b>EntitiesTargetCommand</b> adds a valid <see cref="Command.Target"/> command parameter to
    /// the functionality provided by the <see cref="EntitiesCommand"/> class.
    /// </para><para>
    /// The <see cref="SiteReference.Location"/> component of <b>Target</b> holds the value of the
    /// "target" XML element. The <see cref="SiteReference.Value"/> component holds the
    /// corresponding <see cref="Site"/> once <see cref="EntitiesTargetCommand.Validate"/> has
    /// succeeded.
    /// </para><para>
    /// Derived classes should add the remaining members and any other functionality that is
    /// required for specific commands.
    /// </para><para>
    /// <b>EntitiesTargetCommand</b> corresponds to the complex XML type "entitiesTargetCommand"
    /// defined in <see cref="FilePaths.SessionSchema"/>.</para></remarks>

    public abstract class EntitiesTargetCommand: EntitiesCommand {
        #region EntitiesTargetCommand()

        /// <overloads>
        /// Initializes a new instance of the <see cref="EntitiesTargetCommand"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="EntitiesTargetCommand"/> class with default
        /// properties.</summary>

        protected EntitiesTargetCommand(): base() { }

        #endregion
        #region EntitiesTargetCommand(Faction, IList<Entity>, PointI)

        /// <summary>
        /// Initializes a new instance of the <see cref="EntitiesTargetCommand"/> class with the
        /// specified <see cref="Faction"/>, entities, and target location.</summary>
        /// <param name="faction">
        /// The initial value for the <see cref="Command.Faction"/> property.</param>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> whose <see cref="Entity"/> elements provide the initial value
        /// for the <see cref="Command.Entities"/> property.</param>
        /// <param name="target">
        /// The initial value for the <b>Location</b> component of the <see cref="Command.Target"/>
        /// property.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="entities"/> contains a null reference.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="entities"/> is a null reference or an empty collection.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="target"/> specifies one or two negative coordinates.</exception>

        protected EntitiesTargetCommand(Faction faction, IList<Entity> entities, PointI target):
            base(faction, entities) {

            if (target.X < 0 || target.Y < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(
                    "target", target, Tektosyne.Strings.ArgumentContainsNegative);

            Target = new SiteReference(target);
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="EntitiesTargetCommand"/> against the
        /// specified <see cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> to validate the <see cref="EntitiesTargetCommand"/>
        /// against.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException"><para>
        /// One of the conditions listed under <see cref="EntitiesCommand.Validate"/>.
        /// </para><para>-or-</para><para>
        /// The value of the <see cref="Command.Target"/> property is invalid. </para></exception>
        /// <remarks>
        /// <b>Validate</b> sets the <see cref="Command.Source"/> property and the <b>Value</b>
        /// components of the <see cref="Command.Faction"/>, <see cref="Command.Entities"/>, and
        /// <see cref="Command.Target"/> properties on success. These properties hold undefined
        /// values on failure.</remarks>

        public override void Validate(WorldState worldState) {
            base.Validate(worldState);

            int x = Target.Location.X, y = Target.Location.Y;
            if (x < 0 || y < 0)
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.CommandTargetNone, Name);

            if (x >= worldState.Sites.GetLength(0) || y >= worldState.Sites.GetLength(1))
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.CommandTargetInvalid, Name, Target);

            // store reference to target site
            Target = new SiteReference(worldState.Sites[x, y]);
        }

        #endregion
        #region XmlSerializable Members
        #region ReadXmlElements

        /// <summary>
        /// Reads XML element data into the <see cref="EntitiesTargetCommand"/> object using the
        /// specified <see cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <returns>
        /// <c>true</c> if the current node of the specified <paramref name="reader"/> contained any
        /// matching data; otherwise, <c>false</c>.</returns>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.SessionSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override bool ReadXmlElements(XmlReader reader) {
            if (base.ReadXmlElements(reader))
                return true;

            switch (reader.Name) {

                case "target": {
                    PointI target = SimpleXml.ReadPointI(reader);
                    Target = new SiteReference(target);
                    return true;
                }

                default: return false;
            }
        }

        #endregion
        #region WriteXmlElements

        /// <summary>
        /// Writes all current data of the <see cref="EntitiesTargetCommand"/> object that is
        /// serialized to nested XML elements to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlElements(XmlWriter writer) {
            base.WriteXmlElements(writer);

            writer.WriteStartElement("target");
            SimpleXml.WritePointI(writer, Target.Location);
            writer.WriteEndElement();
        }

        #endregion
        #endregion
    }
}
