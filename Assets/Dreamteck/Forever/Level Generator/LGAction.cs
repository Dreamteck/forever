namespace Dreamteck.Forever
{
    using System;

    public class LGAction
    {
        public delegate void LGHandler(Action completeHandler, params object[] args);
        private Action _completeHandler;
        private LGHandler _operation;
        private bool _isStarted = false;
        private object[] _args = new object[0];

        public LGAction(LGHandler operation, Action completeHandler, params object[] args)
        {
            _operation = operation;
            _completeHandler = completeHandler;
            _args = args;
        }

        public void Start()
        {
            if (!_isStarted)
            {
                _operation(OnOperationComplete, _args);
                _isStarted = true;
            }
        }

        private void OnOperationComplete()
        {
            if(_completeHandler != null)
            {
                _completeHandler();
            }
        }
    }
}
