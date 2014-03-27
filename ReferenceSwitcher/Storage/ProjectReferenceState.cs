// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectReferenceState.cs" company="">
//   
// </copyright>
// <summary>
//   The project reference state.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace PubliWeb.Tools.ReferenceSwitcher.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     The project reference state.
    /// </summary>
    [Serializable]
    public class ProjectReferenceState
    {
        /// <summary>
        ///     Gets or sets the changes.
        /// </summary>
        public ProjectReferenceChange[] Changes { get; set; }

        /// <summary>
        /// The add update.
        /// </summary>
        /// <param name="sourceProject">
        /// The source project.
        /// </param>
        /// <param name="referenceProject">
        /// The reference project.
        /// </param>
        /// <param name="knownPath">
        /// The known path.
        /// </param>
        public void AddUpdate(string sourceProject, string referenceProject, string knownPath)
        {
            if (this.Changes == null)
            {
                this.Changes = new ProjectReferenceChange[0];
            }

            var item = this.Changes.Where(x => x.SourceProject == sourceProject && x.ReferencedProject == referenceProject).FirstOrDefault();

            if (item == null)
            {
                item = new ProjectReferenceChange() { SourceProject = sourceProject, ReferencedProject = referenceProject };
                this.Changes = this.Changes.Union(new[] { item }).ToArray();
            }

            item.KnownPaths = knownPath;
        }

        /// <summary>
        /// The get projects that reference.
        /// </summary>
        /// <param name="uniqueName">
        /// The unique name.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        public IEnumerable<ProjectReferenceChange> GetProjectsThatReference(string uniqueName)
        {
            if (this.Changes == null)
            {
                yield break;
            }

            foreach (var item in this.Changes.Where(x => x.ReferencedProject == uniqueName))
            {
                yield return item;
            }
        }
    }
}