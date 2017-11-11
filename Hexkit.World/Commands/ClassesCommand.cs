using System;
using System.Diagnostics;
using System.Text;
using System.Xml;

using Tektosyne;
using Tektosyne.Collections;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.World.Commands {
    #region Type Aliases

    /*
     * The EntityClassDictionary used by ClassesCommand is a KeyValueList instead of the usual
     * SortedListEx because the dictionary may contain multiple instances of the same key.
     */

    using EntityClassDictionary = KeyValueList<String, EntityClass>;

    #endregion

    /// <summary>
    /// Represents a command that operates on entity classes.</summary>
    /// <remarks><para>
    /// <b>ClassesCommand</b> adds the <see cref="ClassesCommand.EntityClasses"/> command parameter
    /// to the functionality provided by the <see cref="Command"/> class.
    /// </para><para>
    /// Derived classes should add the remaining members and any other functionality that is
    /// required for specific commands.
    /// </para><para>
    /// <b>ClassesCommand</b> corresponds to the complex XML type "classesCommand" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</para></remarks>

    public abstract class ClassesCommand: Command {
        #region ClassesCommand()

        /// <overloads>
        /// Initializes a new instance of the <see cref="ClassesCommand"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="ClassesCommand"/> class with default
        /// properties.</summary>

        protected ClassesCommand(): base() {
            this._entityClasses = new EntityClassDictionary(4);
        }

        #endregion
        #region ClassesCommand(Faction, String[])

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassesCommand"/> class with the specified
        /// <see cref="WorldState"/> and <see cref="EntityClass"/> identifiers.</summary>
        /// <param name="faction">
        /// The initial value for the <see cref="Command.Faction"/> property.</param>
        /// <param name="classIds">
        /// The initial values for the <b>Keys</b> of the <see cref="EntityClasses"/> collection.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="classIds"/> contains a null reference or an empty string.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="faction"/> is a null reference.</exception>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="classIds"/> is a null reference or an empty array.</exception>

        protected ClassesCommand(Faction faction, string[] classIds): base(faction) {

            if (classIds == null || classIds.Length == 0)
                ThrowHelper.ThrowArgumentNullOrEmptyException("classIds");

            this._entityClasses = new EntityClassDictionary(classIds.Length);
            foreach (string classId in classIds) {

                if (String.IsNullOrEmpty(classId))
                    ThrowHelper.ThrowArgumentException(
                        "classIds", Tektosyne.Strings.ArgumentContainsNullOrEmpty);

                EntityClasses.Add(classId, null);
            }
        }

        #endregion
        #region Private Fields

        // property backers
        private readonly EntityClassDictionary _entityClasses;

        #endregion
        #region EntityClasses

        /// <summary>
        /// Gets a list of all entity classes affected by the command.</summary>
        /// <value>
        /// A <see cref="EntityClassDictionary"/> that maps <see cref="EntityClass.Id"/> strings to
        /// the corresponding <see cref="EntityClass"/> objects. The default is an empty collection.
        /// </value>
        /// <remarks><para>
        /// <b>EntityClasses</b> never returns a null reference, but it may return an empty
        /// collection before initialization has completed. Its <see
        /// cref="EntityClassDictionary.Values"/> are always null references until <see
        /// cref="Validate"/> has succeeded, and are never null references afterwards.
        /// </para><para>
        /// The <see cref="EntityClassDictionary.Keys"/> of the <b>EntityClasses</b> collection are
        /// not necessarily unique and hold the values of the "classes" attribute.</para></remarks>

        public EntityClassDictionary EntityClasses {
            [DebuggerStepThrough]
            get { return this._entityClasses; }
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="ClassesCommand"/>.
        /// </summary>
        /// <returns><para>
        /// The result of the base class implementation of <see cref="Command.ToString"/>, followed
        /// by a string representation of each <see cref="EntityClasses"/> element.
        /// </para><para>
        /// The latter is the value of the <see cref="EntityClass.Name"/> property of the element's
        /// <b>Value</b> if it is not a null reference; or the element's <b>Key</b> otherwise.
        /// </para></returns>

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append(base.ToString());

            // append names of all entity classes
            for (int i = 0; i < EntityClasses.Count; i++) {

                // separate names with commas
                sb.Append(i == 0 ? ": " : ", ");

                // use name if available, else ID
                if (EntityClasses.GetByIndex(i) == null)
                    sb.Append(EntityClasses.GetKey(i));
                else
                    sb.Append(EntityClasses.GetByIndex(i).Name);
            }

            return sb.ToString();
        }

        #endregion
        #region Validate

        /// <summary>
        /// Validates the current data of the <see cref="ClassesCommand"/> against the specified
        /// <see cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> to validate the <see cref="ClassesCommand"/> against.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException"><para>
        /// One of the conditions listed under <see cref="Command.Validate"/>.
        /// </para><para>-or-</para><para>
        /// The <see cref="EntityClasses"/> collection contains invalid data.</para></exception>
        /// <remarks>
        /// <b>Validate</b> sets the <b>Value</b> component of the <see cref="Command.Faction"/>
        /// property and the <b>Values</b> of the <see cref="EntityClasses"/> collection on success.
        /// These properties hold undefined values on failure.</remarks>

        public override void Validate(WorldState worldState) {
            base.Validate(worldState);

            if (EntityClasses.Count == 0)
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.CommandEntityClassNone, Name);

            EntitySection entitySection = MasterSection.Instance.Entities;

            for (int i = 0; i < EntityClasses.Count; i++) {
                string id = EntityClasses.GetKey(i);

                if (String.IsNullOrEmpty(id))
                    InvalidCommandException.ThrowNewWithFormat(
                        Global.Strings.CommandEntityClassEmpty, Name);

                EntityClass entityClass = entitySection.GetEntity(id);
                if (entityClass == null)
                    InvalidCommandException.ThrowNewWithFormat(
                        Global.Strings.CommandEntityClassInvalid, Name, id);

                // store reference to entity class
                EntityClasses.SetByIndex(i, entityClass);
            }
        }

        #endregion
        #region XmlSerializable Members
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="ClassesCommand"/> object using the
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

            string idRefs = reader["classes"];
            if (idRefs != null) {

                // add IDs with null references to class list
                foreach (string token in idRefs.Split(null))
                    EntityClasses.Add(String.Intern(token), null);
            }
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="ClassesCommand"/> object that is serialized to
        /// XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            base.WriteXmlAttributes(writer);

            if (EntityClasses.Count > 0)
                writer.WriteAttributeString("classes", String.Join(" ", EntityClasses.Keys));
        }

        #endregion
        #endregion
    }
}
