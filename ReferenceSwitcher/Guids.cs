// --------------------------------------------------------------------------------------------------------------------
// <copyright company="" file="Guids.cs">
//   
// </copyright>
// <summary>
//   The guid list.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace PubliWeb.Tools.ReferenceSwitcher
{
    using System;

    /// <summary>
    ///     The guid list.
    /// </summary>
    internal static class GuidList
    {
        /// <summary>
        ///     The guid reference switcher cmd set string.
        /// </summary>
        public const string guidReferenceSwitcherCmdSetString = "a55e24c4-c11a-434b-99d7-01b7b195a738";

        /// <summary>
        ///     The guid reference switcher pkg string.
        /// </summary>
        public const string guidReferenceSwitcherPkgString = "4fd72c7c-cb99-438d-9061-47c8706c532c";

        /// <summary>
        ///     The guid reference switcher cmd set.
        /// </summary>
        public static readonly Guid guidReferenceSwitcherCmdSet = new Guid(guidReferenceSwitcherCmdSetString);
    };
}