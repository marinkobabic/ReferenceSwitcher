// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StorageProvider.cs" company="">
//   
// </copyright>
// <summary>
//   The storage provider.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace PubliWeb.Tools.ReferenceSwitcher.Storage
{
    using System.IO;
    using System.Xml.Serialization;

    using EnvDTE;

    /// <summary>
    ///     The storage provider.
    /// </summary>
    public class StorageProvider
    {
        /// <summary>
        /// The load.
        /// </summary>
        /// <param name="solution">
        /// The solution.
        /// </param>
        /// <returns>
        /// The <see cref="ProjectReferenceState"/>.
        /// </returns>
        public ProjectReferenceState Load(Solution solution)
        {
            string filename = this.GetFilename(solution);

            if (!File.Exists(filename))
            {
                return new ProjectReferenceState();
            }

            var serializer = new XmlSerializer(typeof(ProjectReferenceState));
            using (var stream = File.OpenRead(filename))
            {
                return serializer.Deserialize(stream) as ProjectReferenceState;
            }
        }

        /// <summary>
        /// The save.
        /// </summary>
        /// <param name="solution">
        /// The solution.
        /// </param>
        /// <param name="state">
        /// The state.
        /// </param>
        public void Save(Solution solution, ProjectReferenceState state)
        {
            var serializer = new XmlSerializer(typeof(ProjectReferenceState));
            using (var stream = File.OpenWrite(this.GetFilename(solution)))
            {
                serializer.Serialize(stream, state);
            }
        }

        /// <summary>
        /// The delete.
        /// </summary>
        /// <param name="solution">
        /// The solution.
        /// </param>
        internal void Delete(Solution solution)
        {
            string filename = this.GetFilename(solution);

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
        }

        /// <summary>
        /// The get filename.
        /// </summary>
        /// <param name="solution">
        /// The solution.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private string GetFilename(Solution solution)
        {
            string path = Path.GetDirectoryName(solution.FileName);
            return Path.Combine(path, Path.GetFileName(solution.FileName) + ".switchReferences.xml");
        }
    }
}