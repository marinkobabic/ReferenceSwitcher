// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReferenceSwitcherPackage.cs" company="">
//   
// </copyright>
// <summary>
//   This is the class that implements the package exposed by this assembly.
//   The minimum requirement for a class to be considered a valid package for Visual Studio
//   is to implement the IVsPackage interface and register itself with the shell.
//   This package uses the helper classes defined inside the Managed Package Framework (MPF)
//   to do it: it derives from the Package class that provides the implementation of the
//   IVsPackage interface and uses the registration attributes defined in the framework to
//   register itself and its components with the shell.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace PubliWeb.Tools.ReferenceSwitcher
{
    using System;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.InteropServices;

    using EnvDTE;

    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using PubliWeb.Tools.ReferenceSwitcher.Helper;

    /// <summary>
    ///     This is the class that implements the package exposed by this assembly.
    ///     The minimum requirement for a class to be considered a valid package for Visual Studio
    ///     is to implement the IVsPackage interface and register itself with the shell.
    ///     This package uses the helper classes defined inside the Managed Package Framework (MPF)
    ///     to do it: it derives from the Package class that provides the implementation of the
    ///     IVsPackage interface and uses the registration attributes defined in the framework to
    ///     register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidReferenceSwitcherPkgString)]
    public sealed class ReferenceSwitcherPackage : Package
    {
        /// <summary>
        ///     The application lock.
        /// </summary>
        private static readonly object ApplicationLock = new object();

        /// <summary>
        ///     The _s_application.
        /// </summary>
        private static DTE _s_application;

        /// <summary>
        ///     The reference helper.
        /// </summary>
        private readonly ReferenceHelper referenceHelper;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ReferenceSwitcherPackage" /> class.
        ///     Default constructor of the package.
        ///     Inside this method you can place any initialization code that does not require
        ///     any Visual Studio service because at this point the package object is created but
        ///     not sited yet inside Visual Studio environment. The place to do all the other
        ///     initialization is the Initialize method.
        /// </summary>
        public ReferenceSwitcherPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
            this.referenceHelper = new ReferenceHelper(this.ShowMessageBox);
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation

        /// <summary>
        ///     Gets the application.
        /// </summary>
        public static DTE Application
        {
            get
            {
                return _s_application;
            }
        }

        /// <summary>
        ///     Initialization of the package; this method is called right after the package is sited, so this is the place
        ///     where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = this.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the menu item.
                var menuCommandID = new CommandID(GuidList.guidReferenceSwitcherCmdSet, (int)PkgCmdIDList.cmdidSwitchToProjectReferences);
                var menuItem = new MenuCommand(this.SwitchToProjectReferencesMenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);

                menuCommandID = new CommandID(GuidList.guidReferenceSwitcherCmdSet, (int)PkgCmdIDList.cmdidResetProjectReferences);
                menuItem = new MenuCommand(this.ResetProjectReferencesMenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);

                lock (ApplicationLock) _s_application = (DTE)this.GetService(typeof(SDTE));
            }
        }

        /// <summary>
        ///     The create status bar.
        /// </summary>
        /// <returns>
        ///     The <see cref="IVsStatusbar" />.
        /// </returns>
        private IVsStatusbar CreateStatusBar()
        {
            return (IVsStatusbar)this.GetService(typeof(SVsStatusbar));
        }

        /// <summary>
        /// The reset project references menu item callback.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void ResetProjectReferencesMenuItemCallback(object sender, EventArgs e)
        {
            if (this.SaveSolution())
            {
                using (var progressBar = new ProgressBar(this.CreateStatusBar()))
                {
                    this.referenceHelper.SwitchBackToAssemblyReferences(Application.Solution, progressBar);
                }
            }
        }

        /// <summary>
        ///     The save solution.
        /// </summary>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool SaveSolution()
        {
            Projects projects = Application.Solution.Projects;
            if (string.IsNullOrWhiteSpace(Application.Solution.FullName))
            {
                this.ShowMessageBox("You must save the solution first.", OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGICON.OLEMSGICON_CRITICAL);
                return false;
            }

            return true;
        }

        /// <summary>
        /// The show message box.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <param name="button">
        /// The button.
        /// </param>
        /// <param name="icon">
        /// The icon.
        /// </param>
        private void ShowMessageBox(string message, OLEMSGBUTTON button = OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGICON icon = OLEMSGICON.OLEMSGICON_INFO)
        {
            var vsUiShell = (IVsUIShell)this.GetService(typeof(SVsUIShell));
            Guid rclsidComp = Guid.Empty;
            int pnResult;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(vsUiShell.ShowMessageBox(0U, ref rclsidComp, "ReferenceSwitcher", message, string.Empty, 0U, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_INFO, 0, out pnResult));
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        ///     See the Initialize method to see how the menu item is associated to this function using
        ///     the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void SwitchToProjectReferencesMenuItemCallback(object sender, EventArgs e)
        {
            if (this.SaveSolution())
            {
                using (var progressBar = new ProgressBar(this.CreateStatusBar()))
                {
                    this.referenceHelper.SwitchToProjectReferences(Application.Solution, progressBar);
                }
            }
        }
    }
}