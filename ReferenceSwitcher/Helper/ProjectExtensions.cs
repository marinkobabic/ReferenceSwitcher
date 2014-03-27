// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectExtensions.cs" company="">
//   
// </copyright>
// <summary>
//   The project extensions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace PubliWeb.Tools.ReferenceSwitcher.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using EnvDTE;

    using VSLangProj;

    /// <summary>
    ///     The project extensions.
    /// </summary>
    internal static class ProjectExtensions
    {
        /// <summary>
        /// The get assembly name.
        /// </summary>
        /// <param name="project">
        /// The project.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string GetAssemblyName(this Project project)
        {
            return project.GetProjectProperty("AssemblyName");
        }

        /// <summary>
        /// The get output file name.
        /// </summary>
        /// <param name="project">
        /// The project.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string GetOutputFileName(this Project project)
        {
            return GetProjectProperty(project, "OutputFileName");
        }

        /// <summary>
        /// The get project property.
        /// </summary>
        /// <param name="project">
        /// The project.
        /// </param>
        /// <param name="propertyName">
        /// The property name.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string GetProjectProperty(this Project project, string propertyName)
        {
            if (project == null || project.Properties == null || project.Properties.Count == 0 || string.IsNullOrEmpty(propertyName))
            {
                return null;
            }

            return (from Property prop in project.Properties where prop.Name == propertyName select prop.Value as string).FirstOrDefault();
        }

        /// <summary>
        /// The get solution projects.
        /// </summary>
        /// <param name="solution">
        /// The solution.
        /// </param>
        /// <param name="condition">
        /// The condition.
        /// </param>
        /// <returns>
        /// The <see cref="IList"/>.
        /// </returns>
        public static IList<Project> GetSolutionProjects(this Solution solution, Func<Project, bool> condition)
        {
            var projects = new List<Project>();
            foreach (Project targetProject in solution.Projects)
            {
                if (condition(targetProject))
                {
                    projects.Add(targetProject);
                }

                projects.AddRange(GetProjects(targetProject, condition));
            }

            return projects;
        }

        /// <summary>
        /// The get target framework.
        /// </summary>
        /// <param name="project">
        /// The project.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string GetTargetFramework(this Project project)
        {
            string frameworkMoniker = project.GetTargetFrameworkMoniker();
            if (frameworkMoniker == null)
            {
                return null;
            }

            string str = frameworkMoniker.Substring(frameworkMoniker.Length - 3, 3).Replace(".", string.Empty);
            if (frameworkMoniker.ToUpperInvariant().IndexOf("SILVERLIGHT", System.StringComparison.Ordinal) > -1)
            {
                return "SL" + str.Substring(0, 1);
            }

            return "NET" + str;
        }

        /// <summary>
        /// The get vs projects.
        /// </summary>
        /// <param name="solution">
        /// The solution.
        /// </param>
        /// <returns>
        /// The <see cref="IList"/>.
        /// </returns>
        public static IList<VSProject> GetVsProjects(this Solution solution)
        {
            return solution.GetSolutionProjects(p => p.IsVsProjectFile()).Select(project => (VSProject)project.Object).ToList();
        }

        /// <summary>
        /// The is directory.
        /// </summary>
        /// <param name="project">
        /// The project.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool IsDirectory(this Project project)
        {
            try
            {
                return project != null && !project.IsVsProjectFile() && string.IsNullOrEmpty(project.FileName);
            }
            catch (NotImplementedException)
            {
                //happens when the project is not loaded the filename raises the exception
                return false;
            }
        }

        /// <summary>
        /// The is vs project file.
        /// </summary>
        /// <param name="project">
        /// The project.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool IsVsProjectFile(this Project project)
        {
            bool isProjectFile = project != null && project.Object != null && project.Object is VSProject;
            return isProjectFile;
        }

        /// <summary>
        /// The reference exists.
        /// </summary>
        /// <param name="project">
        /// The project.
        /// </param>
        /// <param name="filePath">
        /// The file path.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool ReferenceExists(this Project project, string filePath)
        {
            var targetProject = project.Object as VSProject;
            if (targetProject == null)
            {
                return false;
            }

            return targetProject.References.Cast<Reference>().Any(reference => reference.Path == filePath);
        }

        /// <summary>
        /// The get projects.
        /// </summary>
        /// <param name="project">
        /// The project.
        /// </param>
        /// <param name="condition">
        /// The condition.
        /// </param>
        /// <returns>
        /// The <see cref="IList"/>.
        /// </returns>
        private static IList<Project> GetProjects(Project project, Func<Project, bool> condition)
        {
            var projects = new List<Project>();
            if (!project.IsDirectory())
            {
                return projects;
            }

            foreach (ProjectItem projectItem in project.ProjectItems)
            {
                var subProject = projectItem.Object as Project;
                if (subProject != null && condition(subProject))
                {
                    projects.Add(subProject);
                }

                projects.AddRange(GetProjects(subProject, condition));
            }

            return projects;
        }

        /// <summary>
        /// The get target framework moniker.
        /// </summary>
        /// <param name="project">
        /// The project.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string GetTargetFrameworkMoniker(this Project project)
        {
            return GetProjectProperty(project, "TargetFrameworkMoniker");
        }
    }
}