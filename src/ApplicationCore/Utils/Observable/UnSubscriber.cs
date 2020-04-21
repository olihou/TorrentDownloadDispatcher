using System;
using System.Collections.Generic;

namespace ApplicationCore.Utils.Observable
{
    public class UnSubscriber<TObserver> : IDisposable
    {
        private readonly ICollection<IObserver<TObserver>> _observers;
        private readonly IObserver<TObserver> _observer;

        public UnSubscriber(ICollection<IObserver<TObserver>> observers, IObserver<TObserver> observer)
        {
            this._observers = observers;
            this._observer = observer;
        }

        public void Dispose()
        {
            if (_observers.Contains(_observer))
                _observers.Remove(_observer);
        }
    }

}
