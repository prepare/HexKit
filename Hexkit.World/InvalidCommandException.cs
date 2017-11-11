using System;
using System.Runtime.Serialization;

using Hexkit.Global;

namespace Hexkit.World {

    /// <summary>
    /// The exception that is thrown when a game command or HCL instruction contains invalid data.
    /// </summary>
    /// <remarks>
    /// <b>InvalidCommandException</b> is thrown during validation and execution of a <see
    /// cref="Commands.Command"/> or <see cref="Instructions.Instruction"/> that contains invalid
    /// data, given the current execution context.</remarks>

    [Serializable]
    public class InvalidCommandException: Exception {
        #region InvalidCommandException()

        /// <overloads>
        /// Initializes a new instance of the <see cref="InvalidCommandException"/> class.
        /// </overloads>
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidCommandException"/> class with
        /// default properties.</summary>
        /// <remarks><para>
        /// The following table shows the initial property values for the new instance of <see
        /// cref="InvalidCommandException"/>:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Value</description>
        /// </listheader><item>
        /// <term><see cref="Exception.InnerException"/></term>
        /// <description>A null reference.</description>
        /// </item><item>
        /// <term><see cref="Exception.Message"/></term>
        /// <description>A localized message indicating an invalid command.</description>
        /// </item></list></remarks>

        public InvalidCommandException(): base(Global.Strings.CommandInvalid) { }

        #endregion
        #region InvalidCommandException(String)

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidCommandException"/> class with the
        /// specified error message.</summary>
        /// <param name="message">
        /// The error message that specifies the reason for the exception.</param>
        /// <remarks><para>
        /// The following table shows the initial property values for the new instance of <see
        /// cref="InvalidCommandException"/>:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Value</description>
        /// </listheader><item>
        /// <term><see cref="Exception.InnerException"/></term>
        /// <description>A null reference.</description>
        /// </item><item>
        /// <term><see cref="Exception.Message"/></term>
        /// <description>The specified <paramref name="message"/>.</description>
        /// </item></list></remarks>

        public InvalidCommandException(string message): base(message) { }

        #endregion
        #region InvalidCommandException(String, Exception)

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidCommandException"/> class with the
        /// specified error message and with the previous exception that is the cause of this <see
        /// cref="InvalidCommandException"/>.</summary>
        /// <param name="message">
        /// The error message that specifies the reason for the exception.</param>
        /// <param name="innerException">
        /// The previous <see cref="Exception"/> that is the cause of the current <see
        /// cref="InvalidCommandException"/>.</param>
        /// <remarks><para>
        /// The following table shows the initial property values for the new instance of <see
        /// cref="InvalidCommandException"/>:
        /// </para><list type="table"><listheader>
        /// <term>Property</term><description>Value</description>
        /// </listheader><item>
        /// <term><see cref="Exception.InnerException"/></term>
        /// <description>The specified <paramref name="innerException"/>.</description>
        /// </item><item>
        /// <term><see cref="Exception.Message"/></term>
        /// <description>The specified <paramref name="message"/>.</description>
        /// </item></list></remarks>

        public InvalidCommandException(string message, Exception innerException):
            base(message, innerException) { }

        #endregion
        #region InvalidCommandException(SerializationInfo, StreamingContext)

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidCommandException"/> class with
        /// serialized data.</summary>
        /// <param name="info">
        /// The <see cref="SerializationInfo"/> object providing serialized object data for the <see
        /// cref="InvalidCommandException"/>.</param>
        /// <param name="context">
        /// A <see cref="StreamingContext"/> object containing contextual information about the
        /// source or destination.</param>
        /// <remarks>
        /// Please refer to <see cref="Exception(SerializationInfo, StreamingContext)"/> for
        /// details.</remarks>

        protected InvalidCommandException(SerializationInfo info, StreamingContext context):
            base(info, context) { }

        #endregion
        #region ThrowNew

        /// <summary>
        /// Throws an <see cref="InvalidCommandException"/>.</summary>
        /// <param name="message">
        /// The error message that specifies the reason for the exception.</param>

        public static void ThrowNew(string message) {
            throw new InvalidCommandException(message);
        }

        #endregion
        #region ThrowNewWithFormat(..., Object)

        /// <overloads>
        /// Throws an <see cref="InvalidCommandException"/> with a formatted error message.
        /// </overloads>
        /// <summary>
        /// Throws an <see cref="InvalidCommandException"/> with a formatted error message and a
        /// single argument.</summary>
        /// <param name="format">
        /// A composite <see cref="String.Format"/> string for the error message that specifies the
        /// reason for the exception.</param>
        /// <param name="argument">
        /// The argument for the <paramref name="format"/> string.</param>

        public static void ThrowNewWithFormat(string format, object argument) {

            string message = String.Format(ApplicationInfo.Culture, format, argument);
            throw new InvalidCommandException(message);
        }

        #endregion
        #region ThrowNewWithFormat(..., Object, Object)

        /// <summary>
        /// Throws an <see cref="InvalidCommandException"/> with a formatted error message and two
        /// arguments.</summary>
        /// <param name="format">
        /// A composite <see cref="String.Format"/> string for the error message that specifies the
        /// reason for the exception.</param>
        /// <param name="arg0">
        /// The first argument for the <paramref name="format"/> string.</param>
        /// <param name="arg1">
        /// The second argument for the <paramref name="format"/> string.</param>

        public static void ThrowNewWithFormat(string format, object arg0, object arg1) {

            string message = String.Format(ApplicationInfo.Culture, format, arg0, arg1);
            throw new InvalidCommandException(message);
        }

        #endregion
        #region ThrowNewWithFormat(..., Object, Object, Object)

        /// <summary>
        /// Throws an <see cref="InvalidCommandException"/> with a formatted error message and three
        /// arguments.</summary>
        /// <param name="format">
        /// A composite <see cref="String.Format"/> string for the error message that specifies the
        /// reason for the exception.</param>
        /// <param name="arg0">
        /// The first argument for the <paramref name="format"/> string.</param>
        /// <param name="arg1">
        /// The second argument for the <paramref name="format"/> string.</param>
        /// <param name="arg2">
        /// The third argument for the <paramref name="format"/> string.</param>

        public static void ThrowNewWithFormat(
            string format, object arg0, object arg1, object arg2) {

            string message = String.Format(ApplicationInfo.Culture, format, arg0, arg1, arg2);
            throw new InvalidCommandException(message);
        }

        #endregion
    }
}
