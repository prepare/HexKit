using Hexkit.Scenario;

namespace Hexkit.World {

    /// <summary>
    /// Specifies how to check for connections between map sites.</summary>
    /// <remarks><para>
    /// <b>SiteConnection</b> defines the available options when using the <see
    /// cref="Site.IsConnected"/> method to check for visual connections between one <see
    /// cref="Site"/> and a neighboring <see cref="Site"/>.
    /// </para><para>
    /// A visual connection between two adjacent sites is established by an <see cref="Entity"/> 
    /// that defines a <see cref="EntityClass.GetConnections"/> value indicating the shared edge or
    /// vertex. <b>SiteConnection</b> defines three options to locate such an <see cref="Entity"/>.
    /// </para><list type="table"><listheader>
    /// <term>Option</term><description>Effect</description>
    /// </listheader><item>
    /// <term><see cref="SiteConnection.Local"/></term><description>
    /// Check only the site on which <see cref="Site.IsConnected"/> is called.
    /// </description></item><item>
    /// <term><see cref="SiteConnection.LocalOrNeighbor"/></term><description>
    /// Check the site on which <see cref="Site.IsConnected"/> is called, then the specified
    /// adjacent site. Succeed if a connecting entity is found on either site.
    /// </description></item><item>
    /// <term><see cref="SiteConnection.LocalAndNeighbor"/></term><description>
    /// Check the site on which <see cref="Site.IsConnected"/> is called, then the specified
    /// adjacent site. Succeed only if matching connecting entities are found on both sites.
    /// </description></item></list></remarks>

    public enum SiteConnection {

        /// <summary>
        /// Specifies a connection established by a local <see cref="Entity"/>.</summary>

        Local,

        /// <summary>
        /// Specifies a connection established by a local <see cref="Entity"/>, by a neighboring
        /// <see cref="Entity"/>, or by both together.</summary>

        LocalOrNeighbor,

        /// <summary>
        /// Specifies a connection established by a local <see cref="Entity"/> together with a
        /// matching neighboring <see cref="Entity"/>.</summary>

        LocalAndNeighbor
    }
}
