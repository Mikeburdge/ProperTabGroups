using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace ProperTabGroups.Subsystems
{
    internal class SolutionEventsListener : IVsSolutionEvents
    {
        private IVsSolution _solution;
        private uint _solutionEventsCookie;
        private SaveLoadManager _saveLoadManager;

        public SolutionEventsListener()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _saveLoadManager = SaveLoadManager.Instance;
            _solution = (IVsSolution)Package.GetGlobalService(typeof(SVsSolution));
            if (_solution != null)
            {
                _solution.AdviseSolutionEvents(this, out _solutionEventsCookie);
            }
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) => VSConstants.S_OK;

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.S_OK;

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            _saveLoadManager.SaveTabGroups();
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.S_OK;

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.S_OK;

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.S_OK;

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution) => VSConstants.S_OK;

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => VSConstants.S_OK;

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            //_saveLoadManager.SaveTabGroups();
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved) => VSConstants.S_OK;

        public void Dispose()
        {
            if (_solution != null && _solutionEventsCookie != 0)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                _solution.UnadviseSolutionEvents(_solutionEventsCookie);
                _solutionEventsCookie = 0;
            }
        }
    }
}
