using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

using Tektosyne;
using Tektosyne.IO;
using Hexkit.Global;

namespace Hexkit.Scenario {

    /// <summary>
    /// Provides auxiliary constants and methods for managing scenario sections.</summary>

    public static class SectionUtility {
        #region AllSections

        /// <summary>
        /// An <see cref="Array"/> comprising all values in <see cref="ScenarioSection"/>.</summary>
        /// <remarks>
        /// <b>AllSections</b> facilitates iterating through all values of the <see
        /// cref="ScenarioSection"/> enumeration. This field holds the result of
        /// <c>Enum.GetValues(typeof(ScenarioSection))</c>.</remarks>

        public static readonly ScenarioSection[] AllSections =
            (ScenarioSection[]) Enum.GetValues(typeof(ScenarioSection));

        #endregion
        #region CloseComment

        /// <summary>
        /// A <see cref="Regex.Replace"/> string that closes a section "include" comment.</summary>
        /// <remarks><para>
        /// <b>CloseComment</b> should be supplied to the <see cref="Regex.Replace"/> method invoked
        /// on the <see cref="Regex"/> defined by <see cref="IncludeTag"/>.
        /// </para><para>
        /// This call replaces an XML element matched by <b>IncludeTag</b> with an XML comment that
        /// repeats the attributes specified by the original element after the literal string
        /// "include_close".</para></remarks>
        /// <example><para>
        /// The following XML comment is created for the example given in <see cref="IncludeTag"/>:
        /// </para><code>
        /// &lt;!-- include_close element="areas" href="Areas/MyAreas.xml" --/&gt;</code></example>

        internal const string CloseComment =
            @"<!-- include_close element=""${element}"" href=""${href}"" -->";

        #endregion
        #region IncludeTag

        /// <summary>
        /// A <see cref="Regex"/> that matches a section "include" tag.</summary>
        /// <remarks>
        /// <b>IncludeTag</b> matches an empty XML element named "include" containing an attribute
        /// named "element" indicating the <b>XmlName</b> of a <see cref="ScenarioSection"/>, and an
        /// attribute named "href" indicating the section's file path. Both attributes are stored in
        /// match groups of the same name.</remarks>
        /// <example><para>
        /// The following XML element is a valid "include" tag recognized by <b>IncludeTag</b>:
        /// </para><code>
        /// &lt;include element="areas" href="Areas/MyAreas.xml"/&gt;</code></example>

        internal readonly static Regex IncludeTag = new Regex(
            @"<include\s+element=""(?<element>[^""]*)""\s+href=""(?<href>[^""]*)""\s*/>");

        #endregion
        #region OpenComment

        /// <summary>
        /// A <see cref="Regex.Replace"/> string that opens a section "include" comments.</summary>
        /// <remarks><para>
        /// <b>OpenComment</b> should be supplied to the <see cref="Regex.Replace"/> method invoked
        /// on the <see cref="Regex"/> defined by <see cref="IncludeTag"/>.
        /// </para><para>
        /// This call replaces an XML element matched by <b>IncludeTag</b> with an XML comment that
        /// repeats the attributes specified by the original element after the literal string
        /// "include_open".</para></remarks>
        /// <example><para>
        /// The following XML comment is created for the example given in <see cref="IncludeTag"/>:
        /// </para><code>
        /// &lt;!-- include_open element="areas" href="Areas/MyAreas.xml" --/&gt;</code></example>

        internal const string OpenComment =
            @"<!-- include_open element=""${element}"" href=""${href}"" -->";

        #endregion
        #region Subsections

        /// <summary>
        /// An <see cref="Array"/> comprising all values in <see cref="ScenarioSection"/>, except
        /// for <b>ScenarioSection.Master</b>.</summary>
        /// <remarks>
        /// <b>Subsections</b> facilitates iterating through all values of the <see
        /// cref="ScenarioSection"/> enumeration that correspond to subsections.</remarks>

        public static readonly ScenarioSection[] Subsections = CreateSubsections();

        #endregion
        #region Private Methods
        #region CreateSubsections

        /// <summary>
        /// Creates the <see cref="Subsections"/> array.</summary>
        /// <returns>
        /// A new <see cref="Array"/> comprising all values in <see cref="AllSections"/>, except for
        /// <b>ScenarioSection.Master</b>.</returns>

        private static ScenarioSection[] CreateSubsections() {
            ScenarioSection[] array = new ScenarioSection[AllSections.Length - 1];

            int index = 0;
            foreach (ScenarioSection value in AllSections)
                if (value != ScenarioSection.Master)
                    array[index++] = value;

            return array;
        }

        #endregion
        #endregion
        #region CombineSections

        /// <summary>
        /// Combines all sections of the specified scenario description into a monolithic XML file.
        /// </summary>
        /// <param name="master">
        /// An XML file containing the Master section of the scenario description to process.
        /// </param>
        /// <param name="output">
        /// The file path to the monolithic XML document that will contain a copy of the <paramref
        /// name="master"/> file with inlined copies of all referenced subsection files.</param>
        /// <exception cref="ArgumentNullOrEmptyException">
        /// <paramref name="master"/> or <paramref name="output"/> is a null reference or an empty
        /// string.</exception>
        /// <remarks><para>
        /// <b>CombineSections</b> replaces all <see cref="IncludeTag"/> lines encountered while
        /// reading the <paramref name="master"/> file with the contents of the referenced
        /// subsection file, surrounded by <see cref="OpenComment"/> and <see cref="CloseComment"/>.
        /// </para><para>
        /// The resulting monolithic XML document is written to the specified <paramref
        /// name="output"/> file, which will be overwritten if it already exists.</para></remarks>

        public static void CombineSections(string master, string output) {
            IOUtility.CopyAndReplace(master, output, "href", IncludeTag, OpenComment, CloseComment);
        }

        #endregion
        #region GetSection

        /// <summary>
        /// Gets the <see cref="ScenarioSection"/> for the specified <see
        /// cref="XmlSerializable.XmlName"/>.</summary>
        /// <param name="name">
        /// The <see cref="XmlSerializable.XmlName"/> for a scenario section, as defined by <see
        /// cref="FilePaths.ScenarioSchema"/>.</param>
        /// <returns>
        /// A <see cref="ScenarioSection"/> value indicating the scenario section represented by the
        /// specified <paramref name="name"/>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="name"/> is not an XML element name of a scenario section, as defined by
        /// <see cref="FilePaths.ScenarioSchema"/>.</exception>

        public static ScenarioSection GetSection(string name) {
            switch (name) {

                case AreaSection.ConstXmlName:     return ScenarioSection.Areas;
                case EntitySection.ConstXmlName:   return ScenarioSection.Entities;
                case FactionSection.ConstXmlName:  return ScenarioSection.Factions;
                case ImageSection.ConstXmlName:    return ScenarioSection.Images;
                case MasterSection.ConstXmlName:   return ScenarioSection.Master;
                case VariableSection.ConstXmlName: return ScenarioSection.Variables;

                default:
                    ThrowHelper.ThrowArgumentExceptionWithFormat(
                        "name", Tektosyne.Strings.ArgumentSpecifiesInvalid, "XmlName");
                    return 0;
            }
        }

        #endregion
        #region GetXmlName

        /// <summary>
        /// Gets the <see cref="XmlSerializable.XmlName"/> of the specified <see
        /// cref="ScenarioSection"/>.</summary>
        /// <param name="section">
        /// A <see cref="ScenarioSection"/> value indicating the scenario section whose <see
        /// cref="XmlSerializable.XmlName"/> to return.</param>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="section"/> is not a valid <see cref="ScenarioSection"/> value.
        /// </exception>
        /// <returns>
        /// The <see cref="XmlSerializable.XmlName"/> of the class that implements the specified
        /// <paramref name="section"/>.</returns>

        public static string GetXmlName(ScenarioSection section) {
            switch (section) {

                case ScenarioSection.Areas:     return AreaSection.ConstXmlName;
                case ScenarioSection.Entities:  return EntitySection.ConstXmlName;
                case ScenarioSection.Factions:  return FactionSection.ConstXmlName;
                case ScenarioSection.Images:    return ImageSection.ConstXmlName;
                case ScenarioSection.Master:    return MasterSection.ConstXmlName;
                case ScenarioSection.Variables: return VariableSection.ConstXmlName;

                default:
                    ThrowHelper.ThrowInvalidEnumArgumentException(
                        "section", (int) section, typeof(ScenarioSection));
                    return null;
            }
        }

        #endregion
    }
}
