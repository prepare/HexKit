using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;

using Tektosyne;
using Hexkit.Global;

namespace Hexkit.World.Commands {

    /// <summary>
    /// Represents a "Rename" command.</summary>
    /// <remarks>
    /// <b>RenameCommand</b> is serialized to the XML element "Rename" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</remarks>

    public sealed class RenameCommand: EntitiesCommand {
        #region RenameCommand()

        /// <overloads>
        /// Initializes a new instance of the <see cref="RenameCommand"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="RenameCommand"/> class with default
        /// properties.</summary>

        internal RenameCommand(): base() { }

        #endregion
        #region RenameCommand(Faction, IList<Entity>, String)

        /// <summary>
        /// Initializes a new instance of the <see cref="RenameCommand"/> class with the specified
        /// <see cref="WorldState"/>, entities, and entity name.</summary>
        /// <param name="faction">
        /// The initial value for the <see cref="Command.Faction"/> property.</param>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> whose <see cref="Entity"/> elements provide the initial value
        /// for the <see cref="Command.Entities"/> property.</param>
        /// <param name="entityName">
        /// The initial value for the <see cref="EntityName"/> property. This argument may be a null
        /// reference or an empty string.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="entities"/> contains a null reference.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="entities"/> is a null reference or an empty collection.</exception>

        public RenameCommand(Faction faction, IList<Entity> entities, string entityName):
            base(faction, entities) {

            this._entityName = entityName;
        }

        #endregion
        #region Private Fields

        // backer for EntityName property
        private string _entityName;

        #endregion
        #region EntityName

        /// <summary>
        /// Gets the entity name assigned by the <see cref="RenameCommand"/>.</summary>
        /// <value>
        /// A <see cref="String"/> containing the new <see cref="Entity.Name"/> for all affected
        /// <see cref="Command.Entities"/>.</value>
        /// <remarks><para>
        /// <b>EntityName</b> may be a null reference or an empty string, in which case the <see
        /// cref="Entity.Name"/> of all affected <see cref="Command.Entities"/> will be reset
        /// to the <see cref="Entity.DefaultName"/> of each <see cref="Entity"/>.
        /// </para><para>
        /// This property holds the value of the "name" XML attribute.</para></remarks>

        public String EntityName {
            [DebuggerStepThrough]
            get { return this._entityName; }
        }

        #endregion
        #region GenerateProgram

        /// <summary>
        /// Generates and executes the HCL <see cref="Command.Program"/> for the <see
        /// cref="RenameCommand"/>.</summary>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="RenameCommand"/> contains data that is invalid with respect to the
        /// current <see cref="Command.Context"/>.</exception>
        /// <exception cref="PropertyValueException">
        /// <see cref="Command.Context"/> is a null reference.</exception>
        /// <remarks>
        /// <b>GenerateProgram</b> calls <see cref="Command.SetEntityName"/> with each element of
        /// the <see cref="Command.Entities"/> array and the new <see cref="EntityName"/>.</remarks>

        protected override void GenerateProgram() {

            foreach (EntityReference entity in Entities)
                SetEntityName(entity.Id, EntityName);
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="RenameCommand"/>.
        /// </summary>
        /// <returns>
        /// The value of the <see cref="Command.Name"/> property, followed by the value of the <see
        /// cref="EntityName"/> property, followed by the <see cref="EntityReference.Name"/> of each
        /// <see cref="Command.Entities"/> element.</returns>

        public override string ToString() {
            StringBuilder sb = new StringBuilder();

            // append command name
            sb.Append(Name);

            // append new entity name if available
            if (String.IsNullOrEmpty(EntityName))
                sb.Append(" to default name");
            else
                sb.AppendFormat(" to “{0}”", EntityName);

            // append names of all entities
            for (int i = 0; i < Entities.Length; i++) {

                // separate names with commas
                sb.Append(i == 0 ? ": " : ", ");
                sb.Append(Entities[i].Name);
            }

            return sb.ToString();
        }

        #endregion
        #region XmlSerializable Members
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="RenameCommand"/> object using the specified
        /// <see cref="XmlReader"/>.</summary>
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

            this._entityName = reader["name"]; // may be null
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="RenameCommand"/> object that is serialized to
        /// XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            base.WriteXmlAttributes(writer);

            if (!String.IsNullOrEmpty(EntityName))
                writer.WriteAttributeString("name", EntityName);
        }

        #endregion
        #endregion
    }
}
