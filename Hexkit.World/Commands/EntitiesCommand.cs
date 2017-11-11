using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using Tektosyne;
using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World.Commands {

    /// <summary>
    /// Represents a command that operates on entities.</summary>
    /// <remarks><para>
    /// <b>EntitiesCommand</b> adds the <see cref="Command.Entities"/> and <see
    /// cref="Command.Source"/> command parameters to the functionality provided by the <see
    /// cref="Command"/> class.
    /// </para><para>
    /// The <see cref="EntityReference.Id"/> components of the elements in <b>Entities</b> hold the
    /// values of the "entities" XML attribute. The <see cref="EntityReference.Value"/> components
    /// hold the corresponding <see cref="Entity"/> objects once <see
    /// cref="EntitiesCommand.Validate"/> has succeeded.
    /// </para><para>
    /// <b>Source</b> is an <em>inferred</em> argument which is neither provided by clients nor
    /// serialized to session XML files. Instead, <b>Validate</b> sets <b>Source</b> to the 
    /// coordinates and value of the first valid <see cref="Entity.Site"/> property found in the
    /// <b>Entities</b> array, if any.
    /// </para><para>
    /// Derived classes should add the remaining members and any other functionality that is
    /// required for specific commands.
    /// </para><para>
    /// <b>EntitiesCommand</b> corresponds to the complex XML type "entitiesCommand" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</para></remarks>

    public abstract class EntitiesCommand: Command {
        #region EntitiesCommand()

        /// <overloads>
        /// Initializes a new instance of the <see cref="EntitiesCommand"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="EntitiesCommand"/> class with default
        /// properties.</summary>

        protected EntitiesCommand(): base() { }

        #endregion
        #region EntitiesCommand(Faction, IList<Entity>)

        /// <summary>
        /// Initializes a new instance of the <see cref="EntitiesCommand"/> class with the specified
        /// <see cref="Faction"/> and entities.</summary>
        /// <param name="faction">
        /// The initial value for the <see cref="Command.Faction"/> property.</param>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> whose <see cref="Entity"/> elements provide the initial value
        /// for the <see cref="Command.Entities"/> property.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="entities"/> contains a null reference.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="entities"/> is a null reference or an empty collection.</exception>

        protected EntitiesCommand(Faction faction, IList<Entity> entities): base(faction) {

            if (entities == null || entities.Count == 0)
                ThrowHelper.ThrowArgumentNullOrEmptyException("entities");

            Entities = new EntityReference[entities.Count];
            Source = SiteReference.Invalid;

            for (int i = 0; i < entities.Count; i++) {
                Entity entity = entities[i];

                if (entity == null)
                    ThrowHelper.ThrowArgumentException(
                        "entities", Tektosyne.Strings.ArgumentContainsNull);

                Entities[i] = new EntityReference(entity);
            }
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="EntitiesCommand"/>.
        /// </summary>
        /// <returns>
        /// The result of the base class implementation of <see cref="Command.ToString"/>, followed
        /// by the <see cref="EntityReference.Name"/> of each <see cref="Command.Entities"/>
        /// element.</returns>

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append(base.ToString());

            // append names of all entities
            for (int i = 0; i < Entities.Length; i++) {

                // separate names with commas
                sb.Append(i == 0 ? ": " : ", ");
                sb.Append(Entities[i].Name);
            }

            return sb.ToString();
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="EntitiesCommand"/> against the specified
        /// <see cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> to validate the <see cref="EntitiesCommand"/> against.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException"><para>
        /// One of the conditions listed under <see cref="Command.Validate"/>.
        /// </para><para>-or-</para><para>
        /// The <see cref="Command.Entities"/> array contains invalid data.</para></exception>
        /// <remarks>
        /// <b>Validate</b> sets the <see cref="Command.Source"/> property and the <b>Value</b>
        /// components of the <see cref="Command.Faction"/> and <see cref="Command.Entities"/>
        /// properties on success. These properties hold undefined values on failure.</remarks>

        public override void Validate(WorldState worldState) {
            base.Validate(worldState);

            if (Entities.Length == 0)
                InvalidCommandException.ThrowNewWithFormat(Global.Strings.CommandEntityNone, Name);

            Faction faction = Faction.Value;
            EntityCategory category = EntityCategory.Unit;

            /*
             * Possible optimization: Do not re-acquire Entities[i].Value
             * if it is not a null reference. The values were set by the
             * public constructor in this case and should be correct.
             */

            for (int i = 0; i < Entities.Length; i++) {
                string id = Entities[i].Id;

                if (String.IsNullOrEmpty(id))
                    InvalidCommandException.ThrowNewWithFormat(
                        Global.Strings.CommandEntityEmpty, Name);

                Entity entity = faction.GetEntity(id);
                if (entity == null)
                    InvalidCommandException.ThrowNewWithFormat(
                        Global.Strings.CommandEntityInvalid, Name, id, faction.Id);

                if (i == 0)
                    category = entity.Category;
                else if (entity.Category != category)
                    InvalidCommandException.ThrowNewWithFormat(
                        Global.Strings.CommandEntityCategory, Name, entity.Id, entity.Category);

                // store reference to entity
                Entities[i].Value = entity;

                // store source site if defined
                if (Source.Value == null && entity.Site != null)
                    Source = new SiteReference(entity.Site);
            }
        }

        #endregion
        #region XmlSerializable Members
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="EntitiesCommand"/> object using the
        /// specified <see cref="XmlReader"/>.</summary>
        /// <param name="reader">
        /// The <see cref="XmlReader"/> from which to read.</param>
        /// <exception cref="XmlException">
        /// An error occurred while parsing the XML data provided by <paramref name="reader"/>.
        /// </exception>
        /// <exception cref="System.Xml.Schema.XmlSchemaException">
        /// <paramref name="reader"/> is an <see cref="XmlValidatingReader"/>, and the XML data did
        /// not conform to <see cref="FilePaths.SessionSchema"/>.</exception>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void ReadXmlAttributes(XmlReader reader) {
            base.ReadXmlAttributes(reader);

            string idRefs = reader["entities"];
            if (idRefs != null) {

                string[] tokens = idRefs.Split(null);
                Entities = new EntityReference[tokens.Length];

                // add IDs with null references to entity list
                for (int i = 0; i < tokens.Length; i++) {
                    string token = String.Intern(tokens[i]);
                    Entities[i] = new EntityReference(token);
                }
            }
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="EntitiesCommand"/> object that is serialized
        /// to XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            base.WriteXmlAttributes(writer);

            if (Entities.Length > 0) {
                StringBuilder sb = new StringBuilder();

                foreach (EntityReference entity in Entities) {
                    if (sb.Length > 0) sb.Append(" ");
                    sb.Append(entity.Id);
                }

                writer.WriteAttributeString("entities", sb.ToString());
            }
        }

        #endregion
        #endregion
    }
}
