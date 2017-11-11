using System;
using System.Diagnostics;
using System.Xml;

using Tektosyne;
using Tektosyne.Geometry;
using Tektosyne.Xml;

using Hexkit.Global;
using Hexkit.Scenario;
using Hexkit.World.Commands;

namespace Hexkit.World.Instructions {

    /// <summary>
    /// Represents an HCL instruction.</summary>
    /// <remarks><para>
    /// <b>Instruction</b> provides the basic functionality for all instructions defined by the
    /// Hexkit Command Language (HCL). Derived classes should override <see
    /// cref="Instruction.Execute"/>, any required properties, and the protected XML input/output
    /// methods to implement the semantics of specific instructions.
    /// </para><para>
    /// <b>Instruction</b> objects are created during the execution of a <see cref="Command"/>.
    /// Their sequence constitutes a HCL program that encodes the command's effects. Commands are
    /// replayed by executing the program that was generated during their first execution.
    /// </para><para>
    /// <b>Instruction</b> corresponds to the the complex XML type "instruction" defined in <see
    /// cref="FilePaths.SessionSchema"/>.</para></remarks>

    public abstract class Instruction: XmlSerializable {
        #region Instruction()

        /// <overloads>
        /// Initializes a new instance of the <see cref="Instruction"/> class.</overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="Instruction"/> class with default
        /// properties.</summary>

        protected Instruction() { }

        #endregion
        #region Instruction(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="Instruction"/> class with the specified
        /// identifier.</summary>
        /// <param name="id">
        /// The initial value for the <see cref="Id"/> property.</param>

        protected Instruction(string id) {
            if (id != null)
                this._id = String.Intern(id);
        }

        #endregion
        #region Private Fields

        // property backers
        private string _id;

        #endregion
        #region Category

        /// <summary>
        /// Gets the category of the <see cref="Instruction"/>.</summary>
        /// <value>
        /// The default category <see cref="InstructionCategory.Normal"/>.</value>
        /// <remarks>
        /// The <see cref="Instruction"/> implementation of <b>Category</b> always returns <see
        /// cref="InstructionCategory.Normal"/>. Derived classes that represent instructions of
        /// another category should override this property and return the appropriate value.
        /// </remarks>

        public virtual InstructionCategory Category {
            [DebuggerStepThrough]
            get { return InstructionCategory.Normal; }
        }

        #endregion
        #region Id

        /// <summary>
        /// Gets the identifier indicating the object that is manipulated by the <see
        /// cref="Instruction"/>.</summary>
        /// <value>
        /// The identifier of the <see cref="EntityClass"/>, <see cref="Entity"/>, or <see
        /// cref="Faction"/> that is manipulated by the <see cref="Instruction"/>. The default is a
        /// null reference.</value>
        /// <remarks>
        /// <b>Id</b> holds the value of the "id" XML attribute.</remarks>

        public string Id {
            [DebuggerStepThrough]
            get { return this._id; }
        }

        #endregion
        #region Name

        /// <summary>
        /// Gets the display name of the <see cref="Instruction"/>.</summary>
        /// <value>
        /// The display name of the <see cref="Instruction"/>. The default is the value of the <see
        /// cref="XmlName"/> property.</value>
        /// <exception cref="NotImplementedException">
        /// The property was accessed on an abstract base class.</exception>
        /// <remarks>
        /// <b>Name</b> returns the name that should be used to represent the <see
        /// cref="Instruction"/> within Hexkit Game.</remarks>

        public string Name {
            [DebuggerStepThrough]
            get { return XmlName; }
        }

        #endregion
        #region Results

        /// <summary>
        /// Gets or sets the <see cref="InstructionResults"/> for the last call to <see
        /// cref="Execute"/>.</summary>
        /// <value>
        /// The <see cref="InstructionResults"/> for the most recent call to <see cref="Execute"/>.
        /// The default is a null reference.</value>
        /// <remarks><para>
        /// Set <b>Results</b> to a valid <see cref="InstructionResults"/> object prior to calling
        /// <see cref="Execute"/> if you wish to retrieve instruction results. All instructions
        /// check <b>Results</b> for validity before attempting to store object references.
        /// </para><para>
        /// Once the call to <b>Execute</b> has returned and the instruction results have been
        /// processed, you should reset <b>Results</b> to a null reference so that the results can
        /// be garbage-collected. We cannot use weak reference wrappers in this place because the
        /// referenced objects might no longer exist anywhere else when <b>Execute</b> returns.
        /// </para></remarks>

        public InstructionResults Results { get; set; }

        #endregion
        #region Execute

        /// <summary>
        /// Executes the <see cref="Instruction"/> on the specified <see cref="WorldState"/> and
        /// indicates whether the <see cref="WorldState"/> has changed.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="Instruction"/>.</param>
        /// <returns>
        /// <c>true</c> if execution of the <see cref="Instruction"/> has changed the data of the
        /// specified <paramref name="worldState"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="worldState"/> is a null reference.</exception>
        /// <exception cref="InvalidCommandException">
        /// The <see cref="Instruction"/> contains data that is invalid with respect to the
        /// specified <paramref name="worldState"/>.</exception>
        /// <remarks><para>
        /// The <see cref="Instruction"/> implementation of <b>Execute</b> does nothing and always
        /// returns <c>false</c>. Derived classes should override this method to implement the
        /// effects of specific instructions.
        /// </para><para>
        /// When overriding <b>Execute</b>, first check if the instruction would have any effect on
        /// the specified <paramref name="worldState"/>, and immediately return <c>false</c> if this
        /// is not the case. Otherwise, execute the instruction normally and return <c>true</c>.
        /// </para><para>
        /// The caller should delete all instructions from a command's <see cref="Command.Program"/>
        /// for which <b>Execute</b> returns <c>false</c>.
        /// </para><para>
        /// You do not need to override <b>Execute</b> for instructions whose <see cref="Category"/>
        /// is <see cref="InstructionCategory.Event"/>. The caller will pass the instruction to a
        /// suitable <see cref="ShowEventCallback"/> method rather than invoking <b>Execute</b>.
        /// </para></remarks>

        internal virtual bool Execute(WorldState worldState) {
            return false;
        }

        #endregion
        #region ToString

        /// <summary>
        /// Returns a <see cref="String"/> that represents the <see cref="Instruction"/>.</summary>
        /// <returns>
        /// The value of the <see cref="Name"/> property.</returns>

        public override string ToString() {
            return Name;
        }

        #endregion
        #region Get... Methods
        #region GetEntity(WorldState, String)

        /// <overloads>
        /// Returns the <see cref="Entity"/> with the specified identifier.</overloads>
        /// <summary>
        /// Returns the <see cref="Entity"/> with the specified identifier contained in the
        /// specified <see cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> whose <see cref="WorldState.Entities"/> collection to
        /// search.</param>
        /// <param name="id">
        /// The <see cref="Entity.Id"/> string of the <see cref="Entity"/> to locate.</param>
        /// <returns>
        /// The <see cref="Entity"/> with the specified <paramref name="id"/> contained in the
        /// specified <paramref name="worldState"/>.</returns>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="id"/> is a null reference or an empty string.
        /// </para><para>-or-</para><para>
        /// <paramref name="id"/> does not match any of the elements in the <see
        /// cref="WorldState.Entities"/> collection of the specified <paramref name="worldState"/>.
        /// </para></exception>

        protected Entity GetEntity(WorldState worldState, string id) {
            Debug.Assert(worldState != null);

            if (String.IsNullOrEmpty(id))
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.InstructionIdentifierEmpty, Name);

            Entity entity;
            if (!worldState.Entities.TryGetValue(id, out entity))
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.InstructionEntityInvalid, Name, id);

            return entity;
        }

        #endregion
        #region GetEntity(WorldState, String, EntityCategory)

        /// <summary>
        /// Returns the <see cref="Entity"/> with the specified identifier and category, contained
        /// in the specified <see cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> whose <see cref="WorldState.Entities"/> collection to
        /// search.</param>
        /// <param name="id">
        /// The <see cref="Entity.Id"/> string of the <see cref="Entity"/> to locate.</param>
        /// <param name="category">
        /// The <see cref="Entity.Category"/> of the <see cref="Entity"/> to locate.</param>
        /// <returns>
        /// The <see cref="Entity"/> with the specified <paramref name="id"/> and <paramref
        /// name="category"/>, contained in the specified <paramref name="worldState"/>.</returns>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="id"/> is a null reference or an empty string.
        /// </para><para>-or-</para><para>
        /// <paramref name="id"/> does not match any of the elements in the <see
        /// cref="WorldState.Entities"/> collection of the specified <paramref name="worldState"/>.
        /// </para><para>-or-</para><para>
        /// <paramref name="category"/> does not match the <see cref="Entity.Category"/> of the
        /// element with a matching identifier.</para></exception>

        protected Entity GetEntity(WorldState worldState, string id, EntityCategory category) {
            Debug.Assert(worldState != null);

            if (String.IsNullOrEmpty(id))
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.InstructionIdentifierEmpty, Name);

            Entity entity;
            worldState.Entities.TryGetValue(id, out entity);
            if (entity == null || entity.Category != category)
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.InstructionEntityInvalid, Name, id);

            return entity;
        }

        #endregion
        #region GetEntityClass(String)

        /// <overloads>
        /// Returns the <see cref="EntityClass"/> with the specified identifier.</overloads>
        /// <summary>
        /// Returns the <see cref="EntityClass"/> with the specified identifier.</summary>
        /// <param name="id">
        /// The <see cref="EntityClass.Id"/> string of the <see cref="EntityClass"/> to locate.
        /// </param>
        /// <returns>
        /// The <see cref="EntityClass"/> with the specified <paramref name="id"/>.</returns>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="id"/> is a null reference or an empty string.
        /// </para><para>-or-</para><para>
        /// <paramref name="id"/> does not match any of the <see cref="EntityClass"/> objects
        /// defined by the current <see cref="EntitySection"/>.</para></exception>

        protected EntityClass GetEntityClass(string id) {

            if (String.IsNullOrEmpty(id))
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.InstructionIdentifierEmpty, Name);

            EntityClass entityClass = MasterSection.Instance.Entities.GetEntity(id);
            if (entityClass == null)
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.InstructionClassInvalid, Name, id);

            return entityClass;
        }

        #endregion
        #region GetEntityClass(String, EntityCategory)

        /// <summary>
        /// Returns the <see cref="EntityClass"/> with the specified identifier and category.
        /// </summary>
        /// <param name="id">
        /// The <see cref="EntityClass.Id"/> string of the <see cref="EntityClass"/> to locate.
        /// </param>
        /// <param name="category">
        /// The <see cref="EntityClass.Category"/> of the <see cref="EntityClass"/> to locate.
        /// </param>
        /// <returns>
        /// The <see cref="EntityClass"/> with the specified <paramref name="id"/> and <paramref
        /// name="category"/>.</returns>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="id"/> is a null reference or an empty string.
        /// </para><para>-or-</para><para>
        /// <paramref name="id"/> does not match any of the <see cref="EntityClass"/> objects of the
        /// specified <paramref name="category"/> defined by the current <see
        /// cref="EntitySection"/>.</para></exception>

        protected EntityClass GetEntityClass(string id, EntityCategory category) {

            if (String.IsNullOrEmpty(id))
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.InstructionIdentifierEmpty, Name);

            var entities = MasterSection.Instance.Entities.GetEntities(category);

            EntityClass entityClass;
            if (!entities.TryGetValue(id, out entityClass))
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.InstructionClassInvalid, Name, id);

            return entityClass;
        }

        #endregion
        #region GetFaction

        /// <summary>
        /// Returns the <see cref="Faction"/> with the specified identifier contained in the
        /// specified <see cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> whose <see cref="WorldState.Factions"/> collection to
        /// search.</param>
        /// <param name="id">
        /// The <see cref="Faction.Id"/> string of the <see cref="Faction"/> to locate.</param>
        /// <returns>
        /// The <see cref="Faction"/> with the specified <paramref name="id"/> contained in the
        /// specified <paramref name="worldState"/>.</returns>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="id"/> is a null reference or an empty string.
        /// </para><para>-or-</para><para>
        /// <paramref name="id"/> does not match any of the elements in the <see
        /// cref="WorldState.Factions"/> collection of the specified <paramref name="worldState"/>.
        /// </para></exception>

        protected Faction GetFaction(WorldState worldState, string id) {
            Debug.Assert(worldState != null);

            if (String.IsNullOrEmpty(id))
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.InstructionIdentifierEmpty, Name);

            Faction faction;
            if (!worldState.Factions.TryGetValue(id, out faction))
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.InstructionFactionInvalid, Name, id);

            return faction;
        }

        #endregion
        #region GetFactionHistory

        /// <summary>
        /// Returns the <see cref="FactionHistory"/> with the specified identifier contained in the
        /// specified <see cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> whose <see cref="WorldState.History"/> to search.</param>
        /// <param name="id">
        /// The <see cref="Faction.Id"/> string of the <see cref="FactionHistory"/> to locate.
        /// </param>
        /// <returns>
        /// The <see cref="FactionHistory"/> with the specified <paramref name="id"/> contained in
        /// the specified <paramref name="worldState"/>.</returns>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="id"/> is a null reference or an empty string.
        /// </para><para>-or-</para><para>
        /// <paramref name="id"/> does not match any of the elements in the <see
        /// cref="History.Factions"/> collection of the <see cref="WorldState.History"/> of the
        /// specified <paramref name="worldState"/>.</para></exception>

        protected FactionHistory GetFactionHistory(WorldState worldState, string id) {
            Debug.Assert(worldState != null);

            if (String.IsNullOrEmpty(id))
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.InstructionIdentifierEmpty, Name);

            FactionHistory history;
            if (!worldState.History.Factions.TryGetValue(id, out history))
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.InstructionFactionInvalid, Name, id);

            return history;
        }

        #endregion
        #region GetSite

        /// <summary>
        /// Returns the <see cref="Site"/> with the specified coordinates contained in the specified
        /// <see cref="WorldState"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> whose <see cref="WorldState.Sites"/> to search.</param>
        /// <param name="location">
        /// The <see cref="Site.Location"/> of the <see cref="Site"/> to find.</param>
        /// <returns>
        /// The <see cref="Site"/> with the specified <paramref name="location"/> contained in the
        /// specified <paramref name="worldState"/>.</returns>
        /// <exception cref="InvalidCommandException">
        /// <paramref name="location"/> does not indicate a valid element in the <see
        /// cref="WorldState.Sites"/> array of the specified <paramref name="worldState"/>.
        /// </exception>

        protected Site GetSite(WorldState worldState, PointI location) {
            Debug.Assert(worldState != null);

            try {
                Site site = worldState.Sites[location.X, location.Y];
                Debug.Assert(site.Location == location);
                return site;
            }
            catch (IndexOutOfRangeException) {
                // throw new exception for invalid coordinates
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.InstructionSiteInvalid, Name, Site.Format(location));
                return null;
            }
        }

        #endregion
        #region GetVariable(String)

        /// <overloads>
        /// Returns the <see cref="VariableClass"/> with the specified identifier.</overloads>
        /// <summary>
        /// Returns the <see cref="VariableClass"/> with the specified identifier.</summary>
        /// <param name="id">
        /// The <see cref="VariableClass.Id"/> string of the <see cref="VariableClass"/> to locate.
        /// </param>
        /// <returns>
        /// The <see cref="VariableClass"/> with the specified <paramref name="id"/>.</returns>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="id"/> is a null reference or an empty string.
        /// </para><para>-or-</para><para>
        /// <paramref name="id"/> does not match any of the <see cref="VariableClass"/> objects
        /// defined by the current <see cref="VariableSection"/>.</para></exception>

        protected VariableClass GetVariable(string id) {

            if (String.IsNullOrEmpty(id))
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.InstructionIdentifierEmpty, Name);

            VariableClass variable = MasterSection.Instance.Variables.GetVariable(id);
            if (variable == null)
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.InstructionVariableInvalid, Name, id);

            return variable;
        }

        #endregion
        #region GetVariable(String, VariableCategory)

        /// <summary>
        /// Returns the <see cref="VariableClass"/> with the specified identifier and category.
        /// </summary>
        /// <param name="id">
        /// The <see cref="VariableClass.Id"/> string of the <see cref="VariableClass"/> to locate.
        /// </param>
        /// <param name="category">
        /// The <see cref="VariableClass.Category"/> of the <see cref="VariableClass"/> to locate.
        /// </param>
        /// <returns>
        /// The <see cref="VariableClass"/> with the specified <paramref name="id"/> and <paramref
        /// name="category"/>.</returns>
        /// <exception cref="InvalidCommandException"><para>
        /// <paramref name="id"/> is a null reference or an empty string.
        /// </para><para>-or-</para><para>
        /// <paramref name="id"/> does not match any of the <see cref="VariableClass"/> objects of
        /// the specified <paramref name="category"/> defined by the current <see
        /// cref="VariableSection"/>.</para></exception>

        protected VariableClass GetVariable(string id, VariableCategory category) {

            if (String.IsNullOrEmpty(id))
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.InstructionIdentifierEmpty, Name);

            VariableClass variable;
            var variables = MasterSection.Instance.Variables.GetVariables(category);
            if (!variables.TryGetValue(id, out variable))
                InvalidCommandException.ThrowNewWithFormat(
                    Global.Strings.InstructionVariableInvalid, Name, id);

            return variable;
        }

        #endregion
        #endregion
        #region XmlSerializable Members
        #region XmlName

        /// <summary>
        /// Gets the name of the XML element associated with the <see cref="Instruction"/>.
        /// </summary>
        /// <value>
        /// The name of the XML element to which the data of this <see cref="Instruction"/> object
        /// is serialized.</value>
        /// <exception cref="NotImplementedException">
        /// The property was accessed on an abstract base class.</exception>
        /// <remarks><para>
        /// <b>XmlName</b> returns the <see cref="System.Reflection.MemberInfo.Name"/> of the actual
        /// concrete <see cref="Type"/> of this object, without the suffix "Instruction" if present.
        /// </para><para>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// property.</para></remarks>

        internal override string XmlName {
            get {
                Type type = GetType();
                if (type.IsAbstract)
                    ThrowHelper.ThrowNotImplementedException(Tektosyne.Strings.PropertyAbstract);

                // remove suffix "Instruction" if present
                string name = type.Name;
                int suffix = name.IndexOf("Instruction", StringComparison.Ordinal);
                if (suffix > 0) name = name.Remove(suffix);

                return name;
            }
        }

        #endregion
        #region ReadXmlAttributes

        /// <summary>
        /// Reads XML attribute data into the <see cref="Instruction"/> object using the specified
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
            XmlUtility.ReadAttributeAsString(reader, "id", ref this._id);
        }

        #endregion
        #region WriteXmlAttributes

        /// <summary>
        /// Writes all current data of the <see cref="Instruction"/> object that is serialized to
        /// XML attributes to the specified <see cref="XmlWriter"/>.</summary>
        /// <param name="writer">
        /// The <see cref="XmlWriter"/> to which to write.</param>
        /// <remarks>
        /// Please refer to class <see cref="XmlSerializable"/> for a complete description of this
        /// method.</remarks>

        protected override void WriteXmlAttributes(XmlWriter writer) {
            if (!String.IsNullOrEmpty(Id))
                writer.WriteAttributeString("id", Id);
        }

        #endregion
        #endregion
    }
}
