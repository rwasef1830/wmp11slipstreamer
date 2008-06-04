using System;
using System.Collections.Generic;
using System.Text;
using Epsilon.DebugServices;

namespace WMP11Slipstreamer
{
    partial class Backend
    {
        #region Progress fields
        int _progressTotalSteps;
        int _progressCurrentStep;
        #endregion

        /// <summary>
        /// Raised when Backend wants to show a popup message.
        /// </summary>
        internal event EventHandler<OnMessageEventArgs> OnMessage;

        /// <summary>
        /// Raised when the source type is identified
        /// </summary>
        internal event Action<string> OnSourceDetected;

        /// <summary>
        /// If value == -1, progress bar should be hidden and reset, else show
        /// the value and make it visible.
        /// </summary>
        internal event ProgressEventDelegate OnGlobalProgressUpdate;

        /// <summary>
        /// If value == -1, progress bar should be hidden and reset, else show
        /// the value and make it visible.
        /// </summary>
        internal event ProgressEventDelegate OnCurrentProgressUpdate;

        /// <summary>
        /// Called just before starting an operation from which there
        /// can be no graceful cancel or shutdown without corruption of data.
        /// </summary>
        internal event CriticalOperationEventDelegate OnEnterCriticalOperation;

        /// <summary>
        /// Called just after a critical operation has completed successfully.
        /// Aborting the operation can be done now.
        /// </summary>
        internal event CriticalOperationEventDelegate OnExitCriticalOperation;

        /// <summary>
        /// Called to announce to the caller the start of a new operation.
        /// Can be used to update a label or write to the console.
        /// </summary>
        internal event Action<string> OnAnnounceOperation;

        internal delegate void ProgressEventDelegate(int val, int max);
        internal delegate void CriticalOperationEventDelegate();

        internal class OnMessageEventArgs : EventArgs
        {
            internal readonly string Message;
            internal readonly string MessageTitle;
            internal readonly MessageEventType MessageType;

            public OnMessageEventArgs(string message, string messageTitle,
                MessageEventType messageType)
            {
                this.Message = message;
                this.MessageTitle = messageTitle;
                this.MessageType = messageType;
            }
        }

        internal enum MessageEventType
        {
            Error = 16,
            Warning = 48,
            Information = 64,
        }

        void ShowMessage(string message, string messageTitle,
            MessageEventType messageType)
        {
            if (this.OnMessage != null)
            {
                OnMessageEventArgs args = new OnMessageEventArgs(
                    message, messageTitle, messageType);
                this.OnMessage(this, args);
            }
        }

        void AnnounceOperation(string opMessage)
        {
            HelperConsole.InfoWrite("Announcing: ");
            HelperConsole.InfoWriteLine(opMessage);
            if (this.OnAnnounceOperation != null)
            {
                this.OnAnnounceOperation(opMessage);
            }
        }

        void IncrementGlobalProgress()
        {
            this.UpdateGlobalProgress(++this._progressCurrentStep, 
                this._progressTotalSteps);
        }

        void HideGlobalProgress()
        {
            this.UpdateGlobalProgress(-1, 0);
        }

        void UpdateGlobalProgress(int val, int max)
        {
            if (this.OnGlobalProgressUpdate != null)
            {
                this.OnGlobalProgressUpdate(val, max);
            }
        }

        void UpdateCurrentProgress(int val, int max)
        {
            if (this.OnCurrentProgressUpdate != null)
            {
                this.OnCurrentProgressUpdate(val, max);
            }
        }

        void IncrementCurrentProgress(ProgressTracker currentProgressInfo)
        {
            this.UpdateCurrentProgress(++currentProgressInfo.Value, 
                currentProgressInfo.Maximum);
        }

        void ResetCurrentProgress()
        {
            this.UpdateCurrentProgress(0, 1);
        }

        void HideCurrentProgress()
        {
            this.UpdateCurrentProgress(-1, 0);
        }

        void BeginCriticalOperation()
        {
            if (this.OnEnterCriticalOperation != null)
            {
                this.OnEnterCriticalOperation();
            }
        }

        void EndCriticalOperation()
        {
            if (this.OnExitCriticalOperation != null)
            {
                this.OnExitCriticalOperation();
            }
        }

        class ProgressTracker
        {
            internal int Value;
            internal readonly int Maximum;

            internal ProgressTracker(int max)
            {
                this.Value = 0;
                this.Maximum = max;
            }
        }
    }
}
