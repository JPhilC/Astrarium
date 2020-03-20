﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Astrarium.Types
{
    /// <summary>
    /// Base class for all ViewModels.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// Raises when the window or dialog associated with current ViewModel is going to be closed.
        /// </summary>
        public event Action<bool?> Closing;

        /// <summary>
        /// Raised when the ViewModel property is changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Closes the window associated with current ViewModel.
        /// </summary>
        public virtual void Close()
        {
            Close(null);
        }

        /// <summary>
        /// Sets the dialog result value and closes the dialog associated with the current ViewModel.
        /// </summary>
        /// <param name="dialogResult">Dialog result value to be set.</param>
        public void Close(bool? dialogResult)
        {
            Closing?.Invoke(dialogResult);
        }

        /// <summary>
        /// Notifies subscribers about changing property or properties. 
        /// </summary>
        /// <param name="propertyName">Cahnged property name(s).</param>
        protected void NotifyPropertyChanged(params string[] propertyName)
        {
            foreach (string pn in propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(pn));
            }
        }
        
        /// <summary>
        /// Disposes allocated resources
        /// </summary>
        public virtual void Dispose()
        {
            foreach (var binding in bindings)
            {
                binding.Source.PropertyChanged -= SourcePropertyChangedHandler;
            }
            bindings.Clear();
        }

        private List<SimpleBinding> bindings = new List<SimpleBinding>();
        public void AddBinding(SimpleBinding binding)
        {
            bindings.Add(binding);
            binding.Source.PropertyChanged += SourcePropertyChangedHandler;
        }

        private void SourcePropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            var binding = bindings.FirstOrDefault(b => b.Source == sender && b.SourcePropertyName == e.PropertyName);
            if (binding != null)
            {
                NotifyPropertyChanged(binding.TargetPropertyName);
            }
        }

        private Dictionary<string, object> backingFields = new Dictionary<string, object>();
        protected T GetValue<T>(string propertyName, T defaultValue = default(T))
        {
            var binding = bindings.FirstOrDefault(b => b.TargetPropertyName == propertyName);
            if (binding != null)
                return binding.GetValue<T>();
            else
                return backingFields.ContainsKey(propertyName) ? (T)backingFields[propertyName] : defaultValue;
        }

        protected void SetValue(string propertyName, object value)
        {
            var binding = bindings.FirstOrDefault(b => b.TargetPropertyName == propertyName);
            if (binding != null)
                binding.SetValue(value);
            else
                backingFields[propertyName] = value;
            NotifyPropertyChanged(propertyName);
        }
    }
}
