using System;
using System.Windows;

using Tektosyne;
using Tektosyne.Collections;

using Hexkit.Global;
using Hexkit.Scenario;

namespace Hexkit.Editor {
    #region Type Aliases

    using VariableModifierDictionary = SortedListEx<String, VariableModifier>;
    using VariableValueDictionary = SortedListEx<String, Int32>;

    #endregion

    /// <summary>
    /// Provides auxiliary methods for the Hexkit Editor application.</summary>

    public static class EditorUtility {
        #region ValidateVariables(Window, VariableModifierDictionary)

        /// <overloads>
        /// Validates the specified list of variable values.</overloads>
        /// <summary>
        /// Validates the specified <see cref="VariableModifierDictionary"/>.</summary>
        /// <param name="owner">
        /// The parent <see cref="Window"/> for any dialogs.</param>
        /// <param name="variables">
        /// A <see cref="VariableModifierDictionary"/> that maps <see cref="VariableClass.Id"/>
        /// string to the corresponding <see cref="VariableModifier"/> instance values.</param>
        /// <returns>
        /// <c>true</c> if the values of the specified <paramref name="variables"/> are valid;
        /// otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="variables"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ValidateVariables</b> checks that every <see cref="ModifierTarget"/> value for each
        /// in the specified <paramref name="variables"/> collection is either an even multiple of
        /// the <see cref="VariableClass.Scale"/> of its <see cref="VariableClass"/>, or vice versa.
        /// </para><para>
        /// If this is not the case, <b>ValidateVariables</b> asks the user to confirm the value,
        /// and returns <c>false</c> if the user declines. <b>ValidateVariables</b> returns
        /// <c>true</c> if all <paramref name="variables"/> pass this test.</para></remarks>

        public static bool ValidateVariables(Window owner, VariableModifierDictionary variables) {
            if (variables == null)
                ThrowHelper.ThrowArgumentNullException("variables");

            VariableSection variableSection = MasterSection.Instance.Variables;
            foreach (var pair in variables) {

                // get variable, display scale, and new value
                VariableClass variable = variableSection.GetVariable(pair.Key);
                if (variable == null) continue;

                int scale = variable.Scale;
                if (scale == 1) continue;

                foreach (ModifierTarget target in VariableModifier.AllModifierTargets) {
                    int value = Math.Abs(pair.Value.GetByTarget(target).GetValueOrDefault());
                    if (value == 0) continue;

                    // check that new value conforms to scale
                    if ((value > scale && value % scale != 0) ||
                        (value < scale && scale % value != 0)) {

                        string message = String.Format(ApplicationInfo.Culture,
                            Global.Strings.DialogVariableScaleMismatch, pair.Key);

                        MessageBoxResult result = MessageBox.Show(owner,
                            message, Global.Strings.TitleVariableScaleMismatch,
                            MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Cancel)
                            return false;
                    }
                }
            }

            return true;
        }

        #endregion
        #region ValidateVariables(Window, VariableValueDictionary)

        /// <summary>
        /// Validates the specified <see cref="VariableValueDictionary"/>.</summary>
        /// <param name="owner">
        /// The parent <see cref="Window"/> for any dialogs.</param>
        /// <param name="variables">
        /// A <see cref="VariableValueDictionary"/> that maps <see cref="VariableClass.Id"/> string
        /// to the corresponding <see cref="VariableClass"/> instance values.</param>
        /// <returns>
        /// <c>true</c> if the values of the specified <paramref name="variables"/> are valid;
        /// otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="variables"/> is a null reference.</exception>
        /// <remarks><para>
        /// <b>ValidateVariables</b> checks that every variable value in the specified <paramref
        /// name="variables"/> collection is either an even multiple of the <see
        /// cref="VariableClass.Scale"/> of its <see cref="VariableClass"/>, or vice versa.
        /// </para><para>
        /// If this is not the case, <b>ValidateVariables</b> asks the user to confirm the value,
        /// and returns <c>false</c> if the user declines. <b>ValidateVariables</b> returns
        /// <c>true</c> if all <paramref name="variables"/> pass this test.</para></remarks>

        public static bool ValidateVariables(Window owner, VariableValueDictionary variables) {
            if (variables == null)
                ThrowHelper.ThrowArgumentNullException("variables");

            VariableSection variableSection = MasterSection.Instance.Variables;
            foreach (var pair in variables) {

                // get variable, display scale, and new value
                VariableClass variable = variableSection.GetVariable(pair.Key);
                if (variable == null) continue;

                int scale = variable.Scale;
                if (scale == 1) continue;

                int value = Math.Abs(pair.Value);
                if (value == 0) continue;

                // check that new value conforms to scale
                if ((value > scale && value % scale != 0) ||
                    (value < scale && scale % value != 0)) {

                    string message = String.Format(ApplicationInfo.Culture,
                        Global.Strings.DialogVariableScaleMismatch, pair.Key);

                    MessageBoxResult result = MessageBox.Show(owner,
                        message, Global.Strings.TitleVariableScaleMismatch,
                        MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Cancel)
                        return false;
                }
            }

            return true;
        }

        #endregion
    }
}
