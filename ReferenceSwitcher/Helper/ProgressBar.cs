// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProgressBar.cs" company="">
//   
// </copyright>
// <summary>
//   The progress bar.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace PubliWeb.Tools.ReferenceSwitcher.Helper
{
    using System;

    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    ///     The progress bar.
    /// </summary>
    public class ProgressBar : IDisposable
    {
        /// <summary>
        ///     The status bar.
        /// </summary>
        private readonly IVsStatusbar statusBar;

        /// <summary>
        ///     The cookie.
        /// </summary>
        private uint cookie = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressBar"/> class.
        /// </summary>
        /// <param name="statusBar">
        /// The status bar.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public ProgressBar(IVsStatusbar statusBar)
        {
            if (statusBar == null)
            {
                throw new ArgumentNullException("statusBar");
            }

            this.statusBar = statusBar;
        }

        /// <summary>
        ///     The dispose.
        /// </summary>
        public void Dispose()
        {
            this.ClearProgress();
        }

        /// <summary>
        /// The progress.
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <param name="actIndex">
        /// The act index.
        /// </param>
        /// <param name="total">
        /// The total.
        /// </param>
        public void Progress(string text, uint actIndex, uint total = 100)
        {
            uint mycookie = this.cookie;
            var result = this.statusBar.Progress(ref mycookie, 1, string.Empty, actIndex, total);
            if (result == 0)
            {
                this.cookie = mycookie;
            }

            this.statusBar.SetText(text);
            if (actIndex == total)
            {
                this.ClearProgress();
            }
        }

        /// <summary>
        ///     The clear progress.
        /// </summary>
        private void ClearProgress()
        {
            uint mycookie = this.cookie;
            this.statusBar.Progress(ref mycookie, 0, string.Empty, 0, 0);
            this.statusBar.FreezeOutput(0);
            this.statusBar.Clear();
        }
    }
}