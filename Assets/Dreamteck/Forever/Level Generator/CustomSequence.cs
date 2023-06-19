namespace Dreamteck.Forever
{
    using UnityEngine;
    using System.Collections.Generic;

    [System.Serializable]
    public class CustomSequence : ScriptableObject
    {
        protected bool isDone = false;
        private bool stopped = false;

        public bool IsDone()
        {
            if (stopped) return true;
            return isDone;
        }

        /// <summary>
        /// Method used primaryly by the Remote Level editor to extract unique resources and prepare them for loading
        /// </summary>
        /// <returns></returns>
        public virtual GameObject[] GetAllSegments()
        {
            return new GameObject[0];
        }

        public virtual void Initialize()
        {
            isDone = false;
        }

        public virtual GameObject Next()
        {
            return null;
        }

        public virtual void Stop()
        {
            stopped = true;
        }
    }
}
