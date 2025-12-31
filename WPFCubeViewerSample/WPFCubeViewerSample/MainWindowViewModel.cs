using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFCubeViewerSample
{
    internal class MainWindowViewModel : INotifyPropertyChanged
    {
        private string code = String.Empty;

        /// <summary>
        /// Property for the voxel script C# code.
        /// </summary>
        public String Code
        {
            get { return code; }
            set
            {
                if (code != value)
                {
                    code = value;
                    RaisePropertyChanged();
                }
            }
        }

        #region INotifyPropertyChanged

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event for the specified property name.
        /// Thread-safe and can be overridden by subclasses.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed. If null, callers should supply via CallerMemberName.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            // Capture to local variable to avoid race conditions
            var handler = PropertyChanged;
            if (handler == null || string.IsNullOrEmpty(propertyName))
            {
                handler?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
                return;
            }

            handler.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises PropertyChanged explicitly. Useful when the property name is computed or not using the SetProperty helper.
        /// </summary>
        /// <param name="propertyName">Property name to raise. If omitted, CallerMemberName will be used.</param>
        public void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            OnPropertyChanged(propertyName);
        }


        #endregion


    }
}
