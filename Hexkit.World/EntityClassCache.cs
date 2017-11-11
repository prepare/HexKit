using System;
using System.Collections.Generic;
using System.Diagnostics;

using Tektosyne;
using Tektosyne.Collections;
using Hexkit.Scenario;

namespace Hexkit.World {
    #region Type Aliases

    using VariableModifierDictionary = SortedListEx<String, VariableModifier>;
    using VariableValueDictionary = SortedListEx<String, Int32>;
    using VariableList = KeyedList<String, Variable>;

    #endregion

    /// <summary>
    /// Caches <see cref="EntityClass"/> data that was transformed for use by <b>Hexkit.World</b>
    /// types.</summary>
    /// <remarks><para>
    /// Some of the data managed by <see cref="EntityClass"/> objects is not immediately usable by
    /// <b>Hexkit.World</b> types.
    /// </para><para>
    /// The <see cref="VariableValueDictionary"/> and <see cref="VariableModifierDictionary"/>
    /// collections holding the variable values and modifiers of an <see cref="EntityClass"/> must
    /// be transformed into <see cref="VariableList"/> collections. <b>EntityClassCache</b> performs
    /// this task, and caches the resulting collections in dictionaries for faster access to the
    /// variable values of each <see cref="EntityClass"/>.
    /// </para><para>
    /// Hexkit Game should call <see cref="EntityClassCache.Load"/> when a new <see
    /// cref="MasterSection"/> instance has been created to acquire the cached data for all entity
    /// classes. Hexkit Editor should call <see cref="EntityClassCache.Load"/> whenever the map view
    /// is recreated based on changed scenario data.</para></remarks>

    public static class EntityClassCache {
        #region Private Fields

        // dictionaries to cache variable collections
        private static readonly Dictionary<String, VariableList>
            _attributes = new Dictionary<String, VariableList>(),
            _counters = new Dictionary<String, VariableList>(),
            _resources = new Dictionary<String, VariableList>();

        private static readonly Dictionary<String, VariableList[]>
            _attributeModifiers = new Dictionary<String, VariableList[]>(),
            _resourceModifiers = new Dictionary<String, VariableList[]>();

        // property backers
        private static bool _isEmpty = true;

        #endregion
        #region EmptyModifierArray

        /// <summary>
        /// An <see cref="Array"/> containing one empty read-only <see cref="VariableList"/> for
        /// each <see cref="ModifierTarget"/>.</summary>
        /// <remarks>
        /// <b>EmptyModifierArray</b> holds the unchanged result of <see
        /// cref="CreateModifierArray"/>. This array is returned by <see cref="EntityClassCache"/>
        /// methods for nonexistent modifiers, and also shared by all <see
        /// cref="VariableModifierContainer"/> instances without modifier values.</remarks>

        internal static readonly VariableList[] EmptyModifierArray = CreateModifierArray();

        #endregion
        #region IsEmpty

        /// <summary>
        /// Gets a value indicating whether the <see cref="EntityClassCache"/> is empty.</summary>
        /// <value>
        /// <c>true</c> if the <see cref="EntityClassCache"/> does not contain any data; otherwise,
        /// <c>false</c>. The default is <c>true</c>.</value>
        /// <remarks>
        /// <b>IsEmpty</b> is set to <c>false</c> by <see cref="Load"/> and reset to <c>true</c> by
        /// <see cref="Clear"/>.</remarks>

        public static bool IsEmpty {
            [DebuggerStepThrough]
            get { return EntityClassCache._isEmpty; }
        }

        #endregion
        #region Private Methods
        #region CreateModifierArray

        /// <summary>
        /// Creates an array of empty <see cref="Variable"/> collections indexed by <see
        /// cref="ModifierTarget"/> values.</summary>
        /// <returns>
        /// An <see cref="Array"/> containing one empty read-only <see cref="VariableList"/> for
        /// each <see cref="ModifierTarget"/>.</returns>

        private static VariableList[] CreateModifierArray() {
            var array = new VariableList[VariableModifier.AllModifierTargets.Length];

            foreach (var target in VariableModifier.AllModifierTargets)
                array[(int) target] = VariableList.Empty;

            return array;
        }

        #endregion
        #region GetCollection

        /// <summary>
        /// Returns the <see cref="VariableList"/> stored in the specified dictionary with the
        /// identifier of the specified <see cref="EntityClass"/>.</summary>
        /// <param name="dictionary">
        /// The <see cref="Dictionary{String, VariableList}"/> to search.</param>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> whose <see cref="EntityClass.Id"/> to locate.</param>
        /// <returns><para>
        /// The read-only <see cref="VariableList"/> stored with the identifier of the specified
        /// <paramref name="entityClass"/> in the specified <paramref name="dictionary"/>.
        /// </para><para>-or-</para><para>
        /// An empty read-only <see cref="VariableList"/> if the identifier was not found.
        /// </para></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> or <paramref name="entityClass"/> is a null reference.
        /// </exception>

        private static VariableList GetCollection(
            Dictionary<String, VariableList> dictionary, EntityClass entityClass) {

            if (dictionary == null)
                ThrowHelper.ThrowArgumentNullException("dictionary");
            if (entityClass == null)
                ThrowHelper.ThrowArgumentNullException("entityClass");

            VariableList variables;
            return (dictionary.TryGetValue(entityClass.Id, out variables) ?
                variables : VariableList.Empty);
        }

        #endregion
        #region GetCollections

        /// <summary>
        /// Returns the <see cref="VariableList"/> array stored in the specified dictionary with the
        /// identifier of the specified <see cref="EntityClass"/>.</summary>
        /// <param name="dictionary">
        /// The <see cref="Dictionary{String, VariableList}"/> to search.</param>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> whose <see cref="EntityClass.Id"/> to locate.</param>
        /// <returns><para>
        /// The <see cref="Array"/> containing one read-only <see cref="VariableList"/> for each
        /// <see cref="ModifierTarget"/> that is stored with the identifier of the specified
        /// <paramref name="entityClass"/> in the specified <paramref name="dictionary"/>.
        /// </para><para>-or-</para><para>
        /// An <see cref="Array"/> containing one empty read-only <see cref="VariableList"/> for
        /// each <see cref="ModifierTarget"/> if the identifier was not found.</para></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="dictionary"/> or <paramref name="entityClass"/> is a null reference.
        /// </exception>

        private static VariableList[] GetCollections(
            Dictionary<String, VariableList[]> dictionary, EntityClass entityClass) {

            if (dictionary == null)
                ThrowHelper.ThrowArgumentNullException("dictionary");
            if (entityClass == null)
                ThrowHelper.ThrowArgumentNullException("entityClass");

            VariableList[] variables;
            return (dictionary.TryGetValue(entityClass.Id, out variables) ?
                variables : EmptyModifierArray);
        }

        #endregion
        #endregion
        #region Clear

        /// <summary>
        /// Clears the current data of the <see cref="EntityClassCache"/>.</summary>
        /// <remarks>
        /// <b>Clear</b> clears all variable values stored in the <see cref="EntityClassCache"/>
        /// class. Clients should call this method when a scenario is unloaded.</remarks>

        public static void Clear() {

            EntityClassCache._attributes.Clear();
            EntityClassCache._attributeModifiers.Clear();
            EntityClassCache._counters.Clear();
            EntityClassCache._resources.Clear();
            EntityClassCache._resourceModifiers.Clear();

            // mark cache as empty
            EntityClassCache._isEmpty = true;
        }

        #endregion
        #region GetAttributes

        /// <summary>
        /// Returns the transformed <see cref="EntityClass.Attributes"/> collection for the
        /// specified <see cref="EntityClass"/>.</summary>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> whose transformed <see cref="EntityClass.Attributes"/>
        /// collection to return.</param>
        /// <returns><para>
        /// A read-only <see cref="VariableList"/> containing the values of the <see
        /// cref="EntityClass.Attributes"/> for the specified <paramref name="entityClass"/>.
        /// </para><para>-or-</para><para>
        /// An empty read-only <see cref="VariableList"/> if the <see cref="EntityClassCache"/> does
        /// not contain this collection.</para></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entityClass"/> is a null reference.</exception>

        public static VariableList GetAttributes(EntityClass entityClass) {
            return GetCollection(EntityClassCache._attributes, entityClass);
        }

        #endregion
        #region GetAttributeModifiers

        /// <summary>
        /// Returns the transformed <see cref="EntityClass.AttributeModifiers"/> collection for the
        /// specified <see cref="EntityClass"/>.</summary>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> whose transformed <see
        /// cref="EntityClass.AttributeModifiers"/> collection to return.</param>
        /// <returns><para>
        /// An <see cref="Array"/> containing one read-only <see cref="VariableList"/> for each <see
        /// cref="ModifierTarget"/>. Each collection contains the corresponding values of the <see
        /// cref="EntityClass.AttributeModifiers"/> for the specified <paramref
        /// name="entityClass"/>.
        /// </para><para>-or-</para><para>
        /// An <see cref="Array"/> containing one empty read-only <see cref="VariableList"/> for
        /// each <see cref="ModifierTarget"/> if the <see cref="EntityClassCache"/> does not contain
        /// this collection.</para></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entityClass"/> is a null reference.</exception>

        public static VariableList[] GetAttributeModifiers(EntityClass entityClass) {
            return GetCollections(EntityClassCache._attributeModifiers, entityClass);
        }

        #endregion
        #region GetCounters

        /// <summary>
        /// Returns the transformed <see cref="EntityClass.Counters"/> collection for the specified
        /// <see cref="EntityClass"/>.</summary>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> whose transformed <see cref="EntityClass.Counters"/>
        /// collection to return.</param>
        /// <returns><para>
        /// A read-only <see cref="VariableList"/> containing the values of the <see
        /// cref="EntityClass.Counters"/> for the specified <paramref name="entityClass"/>.
        /// </para><para>-or-</para><para>
        /// An empty read-only <see cref="VariableList"/> if the <see cref="EntityClassCache"/> does
        /// not contain this collection.</para></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entityClass"/> is a null reference.</exception>

        public static VariableList GetCounters(EntityClass entityClass) {
            return GetCollection(EntityClassCache._counters, entityClass);
        }

        #endregion
        #region GetResources

        /// <summary>
        /// Returns the transformed <see cref="EntityClass.Resources"/> collection for the specified
        /// <see cref="EntityClass"/>.</summary>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> whose transformed <see cref="EntityClass.Resources"/>
        /// collection to return.</param>
        /// <returns><para>
        /// A read-only <see cref="VariableList"/> containing the values of the <see
        /// cref="EntityClass.Resources"/> for the specified <paramref name="entityClass"/>.
        /// </para><para>-or-</para><para>
        /// An empty read-only <see cref="VariableList"/> if the <see cref="EntityClassCache"/> does
        /// not contain this collection.</para></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entityClass"/> is a null reference.</exception>

        public static VariableList GetResources(EntityClass entityClass) {
            return GetCollection(EntityClassCache._resources, entityClass);
        }

        #endregion
        #region GetResourceModifiers

        /// <summary>
        /// Returns the transformed <see cref="EntityClass.ResourceModifiers"/> collection for the
        /// specified <see cref="EntityClass"/>.</summary>
        /// <param name="entityClass">
        /// The <see cref="EntityClass"/> whose transformed <see
        /// cref="EntityClass.ResourceModifiers"/> collection to return.</param>
        /// <returns><para>
        /// An <see cref="Array"/> containing one read-only <see cref="VariableList"/> for each <see
        /// cref="ModifierTarget"/>. Each collection contains the corresponding values of the <see
        /// cref="EntityClass.ResourceModifiers"/> for the specified <paramref name="entityClass"/>.
        /// </para><para>-or-</para><para>
        /// An <see cref="Array"/> containing one empty read-only <see cref="VariableList"/> for
        /// each <see cref="ModifierTarget"/> if the <see cref="EntityClassCache"/> does not contain
        /// this collection.</para></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="entityClass"/> is a null reference.</exception>

        public static VariableList[] GetResourceModifiers(EntityClass entityClass) {
            return GetCollections(EntityClassCache._resourceModifiers, entityClass);
        }

        #endregion
        #region Load

        /// <summary>
        /// Loads the data of all entity classes in the current scenario into the <see
        /// cref="EntityClassCache"/>.</summary>
        /// <remarks><para>
        /// <b>Load</b> clears all data currently cached by the <see cref="EntityClassCache"/>
        /// class, and then checks if a valid <see cref="MasterSection"/> instance exists.
        /// </para><para>
        /// If so, <b>Load</b> adds the variable values of all <see cref="EntityClass"/> objects
        /// defined by the current scenario to the <see cref="EntityClassCache"/>.</para></remarks>

        public static void Load() {
            Clear(); // clear cached data

            // quit if no data to acquire
            if (MasterSection.Instance == null)
                return;

            const VariablePurpose purpose = (VariablePurpose.Entity | VariablePurpose.Scenario);
            const VariablePurpose basic = (purpose | VariablePurpose.Basic);
            const VariablePurpose modifier = (purpose | VariablePurpose.Modifier);

            // traverse all entity categories
            foreach (EntityCategory category in EntitySection.AllCategories) {

                // acquire all entity classes in each category
                foreach (var pair in MasterSection.Instance.Entities.GetEntities(category)) {

                    _attributes.Add(pair.Key, Variable.CreateCollection(
                        pair.Value.Attributes, VariableCategory.Attribute, basic, true));

                    _attributeModifiers.Add(pair.Key, Variable.CreateCollections(
                        pair.Value.AttributeModifiers, VariableCategory.Attribute, modifier, true));

                    _counters.Add(pair.Key, Variable.CreateCollection(
                        pair.Value.Counters, VariableCategory.Counter, basic, true));

                    _resources.Add(pair.Key, Variable.CreateCollection(
                        pair.Value.Resources, VariableCategory.Resource, basic, true));

                    _resourceModifiers.Add(pair.Key, Variable.CreateCollections(
                        pair.Value.ResourceModifiers, VariableCategory.Resource, modifier, true));
                }
            }

            // mark cache as filled
            EntityClassCache._isEmpty = false;
        }

        #endregion
    }
}
