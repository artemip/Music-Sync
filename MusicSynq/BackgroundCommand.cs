using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace MusicSynq
{
    public class BackgroundCommand : ICommand
    {
        private readonly Func<bool> _canExecute;
        private readonly Action<object> _execute;

        public BackgroundCommand(Func<bool> canExecute, Action<object> execute)
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
            return (_canExecute == null) ? true : _canExecute();
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}