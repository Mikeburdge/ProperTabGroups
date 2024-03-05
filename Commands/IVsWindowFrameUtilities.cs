using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace ProperTabGroups.Commands
{
    internal interface IVsWindowFrameUtilities
    {
        public class IVsWindowFrameUtilities
        {
            public void SetWindowTitle(IVsWindowFrame windowFrame, string title)
            {
                ErrorHandler.ThrowOnFailure(windowFrame.SetProperty((int)__VSFPROPID.VSFPROPID_Caption, title));
            }
            public string GetWindowTitle(IVsWindowFrame windowFrame)
            {
                object caption;
                ErrorHandler.ThrowOnFailure(windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_Caption, out caption));
                return caption as string;
            }

        }

        public class MyRunningDocTableEvents : IVsRunningDocTableEvents
        {
            public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
            {
                // Handle document window hide
                return VSConstants.S_OK;
            }

            public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
            {
                if (fFirstShow != 0)
                {
                    // The document window is being shown for the first time
                }
                return VSConstants.S_OK;
            }

            public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
            {
                throw new NotImplementedException();
            }

            public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
            {
                throw new NotImplementedException();
            }

            public int OnAfterSave(uint docCookie)
            {
                throw new NotImplementedException();
            }

            public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
            {
                throw new NotImplementedException();
            }
        }



    }
}