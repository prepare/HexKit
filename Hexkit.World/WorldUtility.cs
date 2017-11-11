using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Tektosyne;
using Tektosyne.Collections;
using Tektosyne.Geometry;

using Hexkit.Scenario;
using Hexkit.World.Commands;

namespace Hexkit.World {

    using VariableValueDictionary = SortedListEx<String, Int32>;

    /// <summary>
    /// Provides auxiliary methods for the <b>Hexkit.World</b> assembly.</summary>
    /// <remarks>
    /// The <b>WorldUtility</b> methods are likely to be called very frequently by computer player
    /// algorithms. For better runtime performance, release builds perform no argument checking.
    /// Debug builds are guarded by assertions, however.</remarks>

    public static class WorldUtility {
        #region AddAttributes

        /// <summary>
        /// Adds all current values for the specified attribute defined by the specified entities.
        /// </summary>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects to process.
        /// </param>
        /// <param name="attributeId">
        /// The <see cref="VariableClass.Id"/> string of the <see cref="AttributeClass"/> whose
        /// instance values to add up.</param>
        /// <returns>
        /// The sum of all <see cref="Entity.Attributes"/> for the specified <paramref
        /// name="attributeId"/> defined by any <paramref name="entities"/> element.</returns>

        public static int AddAttributes(IList<Entity> entities, string attributeId) {

            Debug.Assert(entities != null);
            Debug.Assert(!String.IsNullOrEmpty(attributeId));

            int sum = 0;

            for (int i = 0; i < entities.Count; i++)
                sum += entities[i].Attributes.GetValue(attributeId);

            return sum;
        }

        #endregion
        #region AddDictionary

        /// <summary>
        /// Adds the values of a specified <see cref="VariableValueDictionary"/> to all values with
        /// matching keys in another dictionary.</summary>
        /// <param name="targets">
        /// The <see cref="VariableValueDictionary"/> to which to add the specified <paramref
        /// name="modifiers"/>.</param>
        /// <param name="modifiers">
        /// The <see cref="VariableValueDictionary"/> to add to the specified <paramref
        /// name="targets"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="targets"/> or <paramref name="modifiers"/> is a null reference.
        /// </exception>
        /// <remarks>
        /// <b>AddDictionary</b> adds the <see cref="Int32"/> values in the specified <paramref
        /// name="modifiers"/> dictionary to all <see cref="Int32"/> values in the specified
        /// <paramref name="targets"/> dictionary with matching <see cref="String"/> keys.</remarks>

        public static void AddDictionary(VariableValueDictionary targets,
            VariableValueDictionary modifiers) {

            if (targets == null)
                ThrowHelper.ThrowArgumentNullException("targets");
            if (modifiers == null)
                ThrowHelper.ThrowArgumentNullException("modifiers");

            for (int i = 0; i < targets.Count; i++) {
                string key = targets.GetKey(i);
                int value = targets.GetByIndex(i);

                int modifier;
                if (modifiers.TryGetValue(key, out modifier))
                    targets.SetByIndex(i, value + modifier);
            }
        }

        #endregion
        #region AddResources

        /// <summary>
        /// Adds all current values for the specified resource defined by the specified entities.
        /// </summary>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects to process.
        /// </param>
        /// <param name="resourceId">
        /// The <see cref="VariableClass.Id"/> string of the <see cref="ResourceClass"/> whose
        /// instance value to add up.</param>
        /// <returns>
        /// The sum of all <see cref="Entity.Resources"/> values for the specified <paramref
        /// name="resourceId"/> defined by any <paramref name="entities"/> element.</returns>

        public static int AddResources(IList<Entity> entities, string resourceId) {

            Debug.Assert(entities != null);
            Debug.Assert(!String.IsNullOrEmpty(resourceId));

            int sum = 0;

            for (int i = 0; i < entities.Count; i++)
                sum += entities[i].Resources.GetValue(resourceId);

            return sum;
        }

        #endregion
        #region AddOwnedAttributes

        /// <summary>
        /// Adds all current values for the specified attribute defined by those specified entities
        /// that are owned.</summary>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects to process.
        /// </param>
        /// <param name="attributeId">
        /// The <see cref="VariableClass.Id"/> string of the <see cref="AttributeClass"/> whose
        /// instance values to add up.</param>
        /// <returns>
        /// The sum of all <see cref="Entity.Attributes"/> values for the specified <paramref
        /// name="attributeId"/> defined by any <paramref name="entities"/> element whose <see
        /// cref="Entity.Owner"/> is not a null reference.</returns>

        public static int AddOwnedAttributes(IList<Entity> entities, string attributeId) {

            Debug.Assert(entities != null);
            Debug.Assert(!String.IsNullOrEmpty(attributeId));

            int sum = 0;

            for (int i = 0; i < entities.Count; i++) {
                Entity entity = entities[i];
                if (entity.Owner != null)
                    sum += entity.Attributes.GetValue(attributeId);
            }

            return sum;
        }

        #endregion
        #region AddOwnedResources

        /// <summary>
        /// Adds all current values for the specified resource defined by those specified entities
        /// that are owned.</summary>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects to process.
        /// </param>
        /// <param name="resourceId">
        /// The <see cref="VariableClass.Id"/> string of the <see cref="ResourceClass"/> whose
        /// instance values to add up.</param>
        /// <returns>
        /// The sum of all <see cref="Entity.Resources"/> values for the specified <paramref
        /// name="resourceId"/> defined by any <paramref name="entities"/> element whose <see
        /// cref="Entity.Owner"/> is not a null reference.</returns>

        public static int AddOwnedResources(IList<Entity> entities, string resourceId) {

            Debug.Assert(entities != null);
            Debug.Assert(!String.IsNullOrEmpty(resourceId));

            int sum = 0;

            for (int i = 0; i < entities.Count; i++) {
                Entity entity = entities[i];
                if (entity.Owner != null)
                    sum += entity.Resources.GetValue(resourceId);
            }

            return sum;
        }

        #endregion
        #region AddStrength

        /// <summary>
        /// Adds the <see cref="Unit.Strength"/> of all surviving units in the specified collection.
        /// </summary>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing the <see cref="Unit"/> objects to process.</param>
        /// <returns>
        /// The sum of the <see cref="Unit.Strength"/> values of all specified <paramref
        /// name="units"/> for which <see cref="Unit.IsAlive"/> is <c>true</c>.</returns>

        public static int AddStrength(IList<Entity> units) {
            int strength = 0;

            for (int i = 0; i < units.Count; i++) {
                Unit unit = (Unit) units[i];
                if (unit.IsAlive)
                    strength += unit.Strength;
            }

            return strength;
        }

        #endregion
        #region AppendEntityNames

        /// <summary>
        /// Appends the names of all specified entities to the specified <see
        /// cref="StringBuilder"/>.</summary>
        /// <param name="builder">
        /// The <see cref="StringBuilder"/> to append to.</param>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects to process.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>AppendEntityNames</b> appends the comma-separated <see cref="Entity.Name"/> strings
        /// of all <paramref name="entities"/> to the specified <paramref name="builder"/>.
        /// </para><para>
        /// The specified <paramref name="builder"/> remains unchanged if <paramref
        /// name="entities"/> is a null reference or an empty collection.</para></remarks>

        public static void AppendEntityNames(StringBuilder builder, IList<Entity> entities) {
            if (builder == null)
                ThrowHelper.ThrowArgumentNullException("builder");

            if (entities == null || entities.Count == 0)
                return;

            builder.Append(entities[0].Name);

            for (int i = 1; i < entities.Count; i++) {
                builder.Append(", ");
                builder.Append(entities[i].Name);
            }
        }

        #endregion
        #region GetAllBuildCounts

        /// <summary>
        /// Counts the number of times any of the specified entity classes may be instantiated with
        /// a <see cref="BuildCommand"/>.</summary>
        /// <param name="worldState">
        /// The <see cref="WorldState"/> on which to execute the <see cref="BuildCommand"/>.</param>
        /// <param name="faction">
        /// The <see cref="Faction"/> that issues the <see cref="BuildCommand"/>.</param>
        /// <param name="classes">
        /// An <see cref="IList{T}"/> containing the <see cref="EntityClass"/> objects to process.
        /// </param>
        /// <returns>
        /// An <see cref="Array"/> of <see cref="Int32"/> values indicating the number of times the
        /// <paramref name="classes"/> element at the same index position may be instantiated with a
        /// <see cref="BuildCommand"/>.</returns>
        /// <remarks><para>
        /// <b>GetAllBuildTargets</b> calls <see cref="Faction.GetBuildCount"/> with all elements in
        /// the specified <paramref name="classes"/> collection to determine the number of times
        /// each <see cref="EntityClass"/> may be instantiated by the specified <paramref
        /// name="faction"/>.
        /// </para><para>
        /// <b>GetAllBuildTargets</b> never returns a null reference, but it returns an empty
        /// collection exactly if the <paramref name="classes"/> collection is empty.
        /// </para></remarks>

        public static int[] GetAllBuildCounts(WorldState worldState,
            Faction faction, IList<EntityClass> classes) {

            Debug.Assert(worldState != null);
            Debug.Assert(faction != null);
            Debug.Assert(classes != null);

            int[] counts = new int[classes.Count];

            // compute build counts for all classes
            for (int i = 0; i < classes.Count; i++)
                counts[i] = faction.GetBuildCount(worldState, classes[i]);

            return counts;
        }

        #endregion
        #region GetEntityByClass

        /// <summary>
        /// Returns the first element among the specified entities with the specified <see
        /// cref="EntityClass"/> identifier.</summary>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects to search.</param>
        /// <param name="classId">
        /// The <see cref="EntityClass.Id"/> string of the <see cref="Entity.EntityClass"/> value to
        /// locate.</param>
        /// <returns>
        /// The first <paramref name="entities"/> element whose <see cref="Entity.EntityClass"/>
        /// identifier matches the specified <paramref name="classId"/>, if found; otherwise, a null
        /// reference.</returns>
        /// <remarks>
        /// If the last character of the specified <paramref name="classId"/> is an asterisk ("*"),
        /// <b>GetEntityByClass</b> returns the first <paramref name="entities"/> element whose <see
        /// cref="Entity.EntityClass"/> identifier starts with the specified <paramref
        /// name="classId"/> minus the asterisk.</remarks>

        public static Entity GetEntityByClass(IList<Entity> entities, string classId) {

            Debug.Assert(entities != null);
            Debug.Assert(!String.IsNullOrEmpty(classId));

            if (classId.EndsWith("*", StringComparison.Ordinal)) {
                classId = classId.TrimEnd('*');

                for (int i = 0; i < entities.Count; i++)
                    if (entities[i].EntityClass.Id.StartsWith(classId, StringComparison.Ordinal))
                        return entities[i];

                return null;
            }

            for (int i = 0; i < entities.Count; i++)
                if (entities[i].EntityClass.Id == classId)
                    return entities[i];

            return null;
        }

        #endregion
        #region GetEntityNames

        /// <summary>
        /// Returns a list of all names of the specified entities.</summary>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects to process.
        /// </param>
        /// <returns>
        /// An <see cref="Array"/> containing the <see cref="Entity.Name"/> values of all elements
        /// in <paramref name="entities"/>.</returns>
        /// <remarks>
        /// <b>GetEntityNames</b> returns a null reference exactly if the specified <paramref
        /// name="entities"/> collection is a null reference.</remarks>

        public static string[] GetEntityNames(IList<Entity> entities) {
            if (entities == null) return null;

            int count = entities.Count;
            string[] names = new string[count];

            for (int i = 0; i < count; i++)
                names[i] = entities[i].Name;

            return names;
        }

        #endregion
        #region GetEntitySites

        /// <summary>
        /// Returns a list of all unique <see cref="Entity.Site"/> values among the specified
        /// entities.</summary>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects to process.
        /// </param>
        /// <returns>
        /// A <see cref="List{T}"/> containing all unique <see cref="Entity.Site"/> values of the
        /// elements in <paramref name="entities"/>, excluding null references.</returns>
        /// <remarks>
        /// <b>GetEntitySites</b> never returns a null reference, but it returns an empty collection
        /// if <paramref name="entities"/> is an empty collection, or if all <see
        /// cref="Entity.Site"/> values are null references.</remarks>

        public static List<Site> GetEntitySites(IList<Entity> entities) {
            List<Site> sites = new List<Site>(entities.Count);

            // add all unique sites that are not null
            for (int i = 0; i < entities.Count; i++) {
                Site site = entities[i].Site;

                if (site != null && !sites.Contains(site))
                    sites.Add(site);
            }

            return sites;
        }

        #endregion
        #region GetOwnedEntities

        /// <summary>
        /// Returns a list of all owned entities in the specified collection.</summary>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects to process.
        /// </param>
        /// <returns><para>
        /// A <see cref="List{T}"/> containing all elements in <paramref name="entities"/> whose
        /// <see cref="Entity.Owner"/> is not a null reference.
        /// </para><para>-or-</para><para>
        /// A null reference if <paramref name="entities"/> is an empty collection or does not
        /// contain any owned entities.</para></returns>

        public static List<Entity> GetOwnedEntities(IList<Entity> entities) {
            Debug.Assert(entities != null);
            List<Entity> owned = null;

            for (int i = 0; i < entities.Count; i++) {
                Entity entity = entities[i];
                Debug.Assert(entity != null);

                if (entity.Owner != null) {
                    if (owned == null) owned = new List<Entity>();
                    owned.Add(entity);
                }
            }

            return owned;
        }

        #endregion
        #region GetSiteLocations

        /// <summary>
        /// Returns a list of all unique coordinates of the specified sites.</summary>
        /// <param name="sites">
        /// An <see cref="IList{T}"/> containing the <see cref="Site"/> objects to process.</param>
        /// <returns>
        /// A <see cref="List{PointI}"/> containing all unique <see cref="Site.Location"/> values
        /// found in <paramref name="sites"/>.</returns>
        /// <remarks>
        /// <b>GetSiteLocations</b> never returns a null reference, but it returns an empty
        /// collection if <paramref name="sites"/> is an empty collection.</remarks>

        public static List<PointI> GetSiteLocations(IList<Site> sites) {
            List<PointI> locations = new List<PointI>(sites.Count);

            for (int i = 0; i < sites.Count; i++) {
                PointI location = sites[i].Location;

                if (!locations.Contains(location))
                    locations.Add(location);
            }

            return locations;
        }

        #endregion
        #region GetUnownedEntities

        /// <summary>
        /// Returns a list of all unowned entities in the specified collection.</summary>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects to process.
        /// </param>
        /// <returns><para>
        /// A <see cref="List{T}"/> containing all elements in <paramref name="entities"/> whose
        /// <see cref="Entity.Owner"/> is a null reference.
        /// </para><para>-or-</para><para>
        /// A null reference if <paramref name="entities"/> is an empty collection or does not
        /// contain any unowned entities.</para></returns>

        public static List<Entity> GetUnownedEntities(IList<Entity> entities) {
            Debug.Assert(entities != null);
            List<Entity> unowned = null;

            for (int i = 0; i < entities.Count; i++) {
                Entity entity = entities[i];
                Debug.Assert(entity != null);

                if (entity.Owner == null) {
                    if (unowned == null)
                        unowned = new List<Entity>();
                    unowned.Add(entity);
                }
            }

            return unowned;
        }

        #endregion
        #region MaximumAttribute

        /// <summary>
        /// Finds the greatest current value for the specified attribute defined by the specified
        /// entities.</summary>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects to process.
        /// </param>
        /// <param name="attributeId">
        /// The <see cref="VariableClass.Id"/> string of the <see cref="AttributeClass"/> whose
        /// instance values to maximize.</param>
        /// <returns><para>
        /// The greatest <see cref="Entity.Attributes"/> value for the specified <paramref
        /// name="attributeId"/> defined by any <paramref name="entities"/> element.
        /// </para><para>-or-</para><para>
        /// <see cref="Int32.MinValue"/> if <paramref name="entities"/> is an empty collection or
        /// contains no elements that define a <b>Attributes</b> value for <paramref
        /// name="attributeId"/>.</para></returns>

        public static int MaximumAttribute(IList<Entity> entities, string attributeId) {

            Debug.Assert(entities != null);
            Debug.Assert(!String.IsNullOrEmpty(attributeId));

            int maximum = Int32.MinValue;

            for (int i = 0; i < entities.Count; i++)
                maximum = Math.Max(maximum,
                    entities[i].Attributes.GetValue(attributeId));

            return maximum;
        }

        #endregion
        #region MinimumAttackRange

        /// <summary>
        /// Finds the smallest maximum range for an <see cref="AttackCommand"/> performed by the
        /// specified units.</summary>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing the <see cref="Unit"/> objects to process.</param>
        /// <returns><para>
        /// The smallest <see cref="Unit.AttackRange"/> value defined by any <paramref
        /// name="units"/> element.
        /// </para><para>-or-</para><para>
        /// <see cref="Int32.MaxValue"/> if <paramref name="units"/> is an empty collection.
        /// </para></returns>

        public static int MinimumAttackRange(IList<Entity> units) {
            int range = Int32.MaxValue;

            for (int i = 0; i < units.Count; i++)
                range = Math.Min(range, ((Unit) units[i]).AttackRange);

            return range;
        }

        #endregion
        #region MinimumAttribute

        /// <summary>
        /// Finds the smallest current value for the specified attribute defined by the specified
        /// entities.</summary>
        /// <param name="entities">
        /// An <see cref="IList{T}"/> containing the <see cref="Entity"/> objects to process.
        /// </param>
        /// <param name="attributeId">
        /// The <see cref="VariableClass.Id"/> string of the <see cref="AttributeClass"/> whose
        /// instance values to minimize.</param>
        /// <returns><para>
        /// The smallest <see cref="Entity.Attributes"/> value for the specified <paramref
        /// name="attributeId"/> defined by any <paramref name="entities"/> element.
        /// </para><para>-or-</para><para>
        /// <see cref="Int32.MaxValue"/> if <paramref name="entities"/> is an empty collection or
        /// contains no elements that define a <b>Attributes</b> value for <paramref
        /// name="attributeId"/>.</para></returns>

        public static int MinimumAttribute(IList<Entity> entities, string attributeId) {

            Debug.Assert(entities != null);
            Debug.Assert(!String.IsNullOrEmpty(attributeId));

            int minimum = Int32.MaxValue;

            for (int i = 0; i < entities.Count; i++)
                minimum = Math.Min(minimum,
                    entities[i].Attributes.GetValue(attributeId));

            return minimum;
        }

        #endregion
        #region MinimumMovement

        /// <summary>
        /// Finds the smallest maximum range for a <see cref="MoveCommand"/> performed by the
        /// specified units.</summary>
        /// <param name="units">
        /// An <see cref="IList{T}"/> containing the <see cref="Unit"/> objects to process.</param>
        /// <returns><para>
        /// The smallest <see cref="Unit.Movement"/> value defined by any <paramref name="units"/>
        /// element.
        /// </para><para>-or-</para><para>
        /// <see cref="Int32.MaxValue"/> if <paramref name="units"/> is an empty collection.
        /// </para></returns>

        public static int MinimumMovement(IList<Entity> units) {
            int cost = Int32.MaxValue;

            for (int i = 0; i < units.Count; i++)
                cost = Math.Min(cost, ((Unit) units[i]).Movement);

            return cost;
        }

        #endregion
        #region SubtractDictionary

        /// <summary>
        /// Subracts the values of a specified <see cref="VariableValueDictionary"/> from all values
        /// with matching keys in another dictionary.</summary>
        /// <param name="targets">
        /// The <see cref="VariableValueDictionary"/> from which to subtract the specified <paramref
        /// name="modifiers"/>.</param>
        /// <param name="modifiers">
        /// The <see cref="VariableValueDictionary"/> to subtract from the specified <paramref
        /// name="targets"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="targets"/> or <paramref name="modifiers"/> is a null reference.
        /// </exception>
        /// <remarks>
        /// <b>SubtractDictionary</b> subtracts the <see cref="Int32"/> values in the specified
        /// <paramref name="modifiers"/> dictionary from all <see cref="Int32"/> values in the
        /// specified <paramref name="targets"/> dictionary with matching <see cref="String"/> keys.
        /// </remarks>

        public static void SubtractDictionary(VariableValueDictionary targets,
            VariableValueDictionary modifiers) {

            if (targets == null)
                ThrowHelper.ThrowArgumentNullException("targets");
            if (modifiers == null)
                ThrowHelper.ThrowArgumentNullException("modifiers");

            for (int i = 0; i < targets.Count; i++) {
                string key = targets.GetKey(i);
                int value = targets.GetByIndex(i);

                int modifier;
                if (modifiers.TryGetValue(key, out modifier))
                    targets.SetByIndex(i, value - modifier);
            }
        }

        #endregion
    }
}
