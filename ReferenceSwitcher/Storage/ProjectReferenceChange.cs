// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectReferenceChange.cs" company="">
//   
// </copyright>
// <summary>
//   The project reference change.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace PubliWeb.Tools.ReferenceSwitcher.Storage
{
    using System;

    /// <summary>
    ///     The project reference change.
    /// </summary>
    [Serializable]
    public class ProjectReferenceChange
    {
        /// <summary>
        ///     Gets or sets the known paths.
        /// </summary>
        public string KnownPaths { get; set; }

        /// <summary>
        ///     Gets or sets the referenced project.
        /// </summary>
        public string ReferencedProject { get; set; }

        /// <summary>
        ///     Gets or sets the source project.
        /// </summary>
        public string SourceProject { get; set; }
    }
}