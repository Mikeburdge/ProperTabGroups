using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ProperTabGroups.Commands;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class RunningDocTableEventsHandler : IVsRunningDocTableEvents
{
    private readonly IVsRunningDocumentTable _runningDocumentTable;

    public RunningDocTableEventsHandler(IServiceProvider serviceProvider)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        _runningDocumentTable = serviceProvider.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
        if (_runningDocumentTable == null)
            throw new InvalidOperationException("Could not get the Running Document Table service.");



        // Advise for RDT events
        uint cookie;
        ErrorHandler.ThrowOnFailure(_runningDocumentTable.AdviseRunningDocTableEvents(this, out cookie));
    }

    public Guid GetWindowFrameGuid(IVsWindowFrame windowFrame)
    {
        // Query the GuidPersistenceSlot property of the window frame
        int result = windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_GuidPersistenceSlot, out object guidObject);

        Guid guid;
        if (result == VSConstants.S_OK && guidObject is Guid)
        {
            guid = (Guid)guidObject;
        }
        else
        {
            // Handle the case where the GUID could not be retrieved
            throw new InvalidOperationException("Could not retrieve the GUID from the IVsWindowFrame.");
        }

        return guid;
    }

    public int OnAfterDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (fFirstShow == 0) // fFirstShow is TRUE if the document window is shown for the first time.
        {
            WindowModifyingSubsystem.CheckWindowsVisibility(GetWindowFrameGuid(pFrame));
        }
        else
        {
            uint flags, readLocks, editLocks, itemid;
            string moniker;
            IVsHierarchy hierarchy;
            IntPtr docData = IntPtr.Zero;

            try
            {
                ErrorHandler.ThrowOnFailure(_runningDocumentTable.GetDocumentInfo(docCookie, out flags, out readLocks, out editLocks, out moniker, out hierarchy, out itemid, out docData));

                // Log or handle the document path (moniker) here
                System.Diagnostics.Debug.WriteLine($"Document opened: {moniker}");
            }
            finally
            {
                if (docData != IntPtr.Zero)
                {
                    Marshal.Release(docData);
                }
            }
        }
        return VSConstants.S_OK;
    }

    // Other IVsRunningDocTableEvents methods are implemented as no-ops for brevity.
    public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) => VSConstants.S_OK;
    public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;
    public int OnAfterSave(uint docCookie) => VSConstants.S_OK;
    public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame) => VSConstants.S_OK;
    public int OnBeforeSave(uint docCookie) => VSConstants.S_OK;
    public int OnAfterLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;

    public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
    {
        throw new NotImplementedException();
    }

    public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
    {
        throw new NotImplementedException();
    }
}
