using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace MusicSynq
{
    public class DelegateCommand : ICommand
    {
        private readonly Func<bool> _canExecute;
        private readonly Action<object> _execute;

        public DelegateCommand(Func<bool> canExecute, Action<object> execute)
        {
            _canExecute = canExecute;
            _execute = execute;
        }

        public void Execute(object parameter)
        {
            ThreadPool.QueueUserWorkItem(delegate { _execute.Invoke(parameter); });
        }

        public bool CanExecute(object parameter)
        {
            CanExecuteChanged(this, new EventArgs());

            return _canExecute();
        }

        public event EventHandler CanExecuteChanged;
    }
}