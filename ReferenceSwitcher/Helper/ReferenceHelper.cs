// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReferenceHelper.cs" company="">
//   
// </copyright>
// <summary>
//   The reference helper.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace PubliWeb.Tools.ReferenceSwitcher.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using EnvDTE;

    using Microsoft.Build.Construction;
    using Microsoft.VisualStudio.Shell.Interop;

    using PubliWeb.Tools.ReferenceSwitcher.Storage;

    using VSLangProj;

    using ProjectItem = Microsoft.Build.Evaluation.ProjectItem;

    /// <summary>
    ///     The reference helper.
    /// </summary>
    public class ReferenceHelper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceHelper"/> class.
        /// </summary>
        /// <param name="showMessageAction">
        /// The show message action.
        /// </param>
        public ReferenceHelper(Action<string, OLEMSGBUTTON, OLEMSGICON> showMessageAction)
        {
            this.ShowMessageAction = showMessageAction;
        }

        /// <summary>
        ///     The get any suitable existing item xml.
        /// </summary>
        /// <param name="itemType">
        ///     The item type.
        /// </param>
        /// <param name="unevaluatedInclude">
        ///     The unevaluated include.
        /// </param>
        /// <param name="metadata">
        ///     The metadata.
        /// </param>
        /// <param name="suitableExistingItemXml">
        ///     The suitable existing item xml.
        /// </param>
        private delegate ProjectElement GetAnySuitableExistingItemXml(string itemType, string unevaluatedInclude, IEnumerable<KeyValuePair<string, string>> metadata, out ProjectItemElement suitableExistingItemXml);

        /// <summary>
        ///     Gets or sets the show message action.
        /// </summary>
        private Action<string, OLEMSGBUTTON, OLEMSGICON> ShowMessageAction { get; set; }

        /// <summary>
        /// The switch back to assembly references.
        /// </summary>
        /// <param name="solution">
        /// The solution.
        /// </param>
        /// <param name="progressBar">
        /// </param>
        public void SwitchBackToAssemblyReferences(Solution solution, ProgressBar progressBar)
        {
            var projects = solution.GetVsProjects();
            var projectsCount = (uint)projects.Count;
            uint actCount = 0;

            var storage = new StorageProvider();
            var projectRefState = storage.Load(solution);

            foreach (var targetProject in projects)
            {
                var items = projectRefState.GetProjectsThatReference(targetProject.Project.UniqueName);

                actCount++;
                progressBar.Progress(string.Format("Switch back all references to project {0}", targetProject.Project.Name), actCount, projectsCount);

                foreach (var item in items)
                {
                    Project projectToAddReferenceTo = null;
                    foreach (var p in projects)
                    {
                        if (p.Project.UniqueName == item.SourceProject)
                        {
                            projectToAddReferenceTo = p.Project;
                            break;
                        }
                    }

                    string filePath = item.KnownPaths;

                    if (string.IsNullOrEmpty(filePath))
                    {
                        continue;
                    }

                    if (projectToAddReferenceTo != null)
                    {
                        this.AddAssemblyReference(projectToAddReferenceTo, targetProject.Project, filePath);
                    }
                }
            }

            storage.Delete(solution);
        }

        /// <summary>
        /// The switch to project references.
        /// </summary>
        /// <param name="solution">
        /// The solution.
        /// </param>
        /// <param name="progressBar">
        /// </param>
        public void SwitchToProjectReferences(Solution solution, ProgressBar progressBar)
        {
            string projectName = string.Empty;
            string referenceName = string.Empty;

            try
            {
                var projects = solution.GetVsProjects();
                var projectsCount = (uint)projects.Count;
                uint actCount = 0;

                foreach (var targetProject in projects)
                {
                    actCount++;

                    progressBar.Progress(string.Format("Switch project {0}", targetProject.Project.Name), actCount, projectsCount);

                    var foundAssemblyReferences = FindProjectReferencesToAdd(targetProject.Project).ToList();
                    if (!foundAssemblyReferences.Any())
                    {
                        continue;
                    }

                    SaveChanges(targetProject.Project, foundAssemblyReferences);

                    // add project references
                    foreach (var item in foundAssemblyReferences)
                    {
                        projectName = item.Project.Project.Name;
                        referenceName = item.ProjectToReference.Name;
                        item.Reference.Remove();
                        item.Project.References.AddProject(item.ProjectToReference);
                    }

                    referenceName = string.Empty;
                }
            }
            catch (Exception exception)
            {
                throw new Exception(string.Format("Target project {0} referece {1}. {2}", projectName, referenceName, exception.Message), exception);
            }
        }

        /// <summary>
        /// The find project references to add.
        /// </summary>
        /// <param name="targetProject">
        /// The target project.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        private static IEnumerable<ProjectReferenceToAdd> FindProjectReferencesToAdd(Project targetProject)
        {
            string assemblyName = targetProject.GetAssemblyName();

            var solution = targetProject.DTE.Solution;

            var changes = new List<ProjectReferenceToAdd>();

            if (string.IsNullOrEmpty(assemblyName))
            {
                return changes;
            }

            foreach (var vsProject in solution.GetVsProjects())
            {
                if (vsProject.Project.UniqueName == targetProject.UniqueName)
                {
                    continue;
                }

                foreach (Reference reference in vsProject.References)
                {
                    if (reference.Name == assemblyName)
                    {
                        changes.Add(new ProjectReferenceToAdd { Reference = reference, Project = vsProject, ProjectToReference = targetProject });
                    }
                }
            }

            return changes;
        }

        /// <summary>
        /// The save changes.
        /// </summary>
        /// <param name="projectAdded">
        /// The project added.
        /// </param>
        /// <param name="changes">
        /// The changes.
        /// </param>
        private static void SaveChanges(Project projectAdded, IEnumerable<ProjectReferenceToAdd> changes)
        {
            var storage = new StorageProvider();

            var referenceChanges = storage.Load(projectAdded.DTE.Solution);

            foreach (var item in changes)
            {
                referenceChanges.AddUpdate(item.Project.Project.UniqueName, item.ProjectToReference.UniqueName, ToRelative(item.Reference.Path, item.Project.Project.FullName));
            }

            storage.Save(projectAdded.DTE.Solution, referenceChanges);
        }

        /// <summary>
        /// The to absolute.
        /// </summary>
        /// <param name="filePath">
        /// The file path.
        /// </param>
        /// <param name="relativeTo">
        /// The relative to.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string ToAbsolute(string filePath, string relativeTo)
        {
            if (Path.IsPathRooted(filePath))
            {
                return new Uri(filePath).LocalPath;
            }

            var fullPath = Path.Combine(Path.GetDirectoryName(relativeTo), filePath);
            return new Uri(Path.GetFullPath(fullPath)).LocalPath;
        }

        /// <summary>
        /// The to relative.
        /// </summary>
        /// <param name="filePath">
        /// The file path.
        /// </param>
        /// <param name="relativeTo">
        /// The relative to.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string ToRelative(string filePath, string relativeTo)
        {
            if (!Path.IsPathRooted(filePath))
            {
                return filePath;
            }

            var newFileUri = new Uri(filePath);
            var projectUri = new Uri(Path.GetDirectoryName(relativeTo).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);

            Uri relativeUri = projectUri.MakeRelativeUri(newFileUri);

            return relativeUri.ToString();
        }

        /// <summary>
        /// The add assembly reference.
        /// </summary>
        /// <param name="projectNeedingReference">
        /// The project needing reference.
        /// </param>
        /// <param name="projectRemoving">
        /// The project removing.
        /// </param>
        /// <param name="filePath">
        /// The file path.
        /// </param>
        private void AddAssemblyReference(Project projectNeedingReference, Project projectRemoving, string filePath)
        {
            var vsProject = (VSProject)projectNeedingReference.Object;
            filePath = ToAbsolute(filePath, vsProject.Project.FullName);

            foreach (Reference reference in vsProject.References)
            {
                if (reference.SourceProject == null)
                {
                    continue;
                }

                if (reference.SourceProject.FullName == projectRemoving.FullName)
                {
                    reference.Remove();
                    break;
                }
            }

            if (projectNeedingReference.ReferenceExists(filePath))
            {
                return;
            }

            if (!File.Exists(filePath))
            {
                this.ShowError(string.Format("Not able to add reference to file {0} for the project {1}", filePath, vsProject.Project.Name));
                Debug.Print("File " + filePath + " does not exist");
                return;
            }

            Reference newRef = vsProject.References.Add(filePath);

            if (!newRef.Path.Equals(filePath, StringComparison.OrdinalIgnoreCase))
            {
                Microsoft.Build.Evaluation.Project buildProject = Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.GetLoadedProjects(projectNeedingReference.FullName).First();
                ProjectItem msBuildRef = null;

                AssemblyName newFileAssemblyName = AssemblyName.GetAssemblyName(filePath);
                foreach (var item in buildProject.GetItems("Reference"))
                {
                    AssemblyName refAssemblyName = null;
                    try
                    {
                        refAssemblyName = new AssemblyName(item.EvaluatedInclude);
                    }
                    catch
                    {
                    }

                    if (refAssemblyName != null)
                    {
                        var refToken = refAssemblyName.GetPublicKeyToken() ?? new byte[0];
                        var newToken = newFileAssemblyName.GetPublicKeyToken() ?? new byte[0];

                        if (refAssemblyName.Name.Equals(newFileAssemblyName.Name, StringComparison.OrdinalIgnoreCase) && ((refAssemblyName.Version != null && refAssemblyName.Version.Equals(newFileAssemblyName.Version)) || (refAssemblyName.Version == null && newFileAssemblyName.Version == null)) && (refAssemblyName.CultureInfo != null && refAssemblyName.CultureInfo.Equals(newFileAssemblyName.CultureInfo) || (refAssemblyName.CultureInfo == null && newFileAssemblyName.CultureInfo == null)) && refToken.SequenceEqual(newToken))
                        {
                            msBuildRef = item;
                            break;
                        }
                    }
                }

                if (msBuildRef != null)
                {
                    msBuildRef.SetMetadataValue("HintPath", ToRelative(filePath, projectNeedingReference.FullName));
                }
            }
        }

        /// <summary>
        /// The show error.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        private void ShowError(string message)
        {
            this.ShowMessageAction(message, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGICON.OLEMSGICON_WARNING);
        }

        /// <summary>
        ///     The project reference to add.
        /// </summary>
        public class ProjectReferenceToAdd
        {
            /// <summary>
            ///     Gets or sets the project.
            /// </summary>
            public VSProject Project { get; set; }

            /// <summary>
            ///     Gets or sets the project to reference.
            /// </summary>
            public Project ProjectToReference { get; set; }

            /// <summary>
            ///     Gets or sets the reference.
            /// </summary>
            public Reference Reference { get; set; }
        }
    }
}