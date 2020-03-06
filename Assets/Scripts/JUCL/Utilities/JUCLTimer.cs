using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace JUCL.Utilities
{
    //Timer class
    public class JUCLTimer
    {

        //Active timer references
        private static List<JUCLTimer> activeTimerList;
        private static GameObject initGameObject;

        //Class to init timer game object
        private static void InitIfNeeded()
        {
            if (initGameObject == null)
            {
                initGameObject = new GameObject("JUCL_InitGameObject");
                activeTimerList = new List<JUCLTimer>();
            }
        }

        //Removes the timer
        private static void RemoveTimer(JUCLTimer timer)
        {
            InitIfNeeded();
            activeTimerList.Remove(timer);
        }

        //Function called externally to stop a timer with a certain name
        public static void StopTimer(string timerName)
        {
            for (int i = 0; i < activeTimerList.Count; i++)
            {
                if (activeTimerList[i].timerName == timerName)
                {
                    activeTimerList[i].DestroySelf();
                    i--;
                }
            }
        }

        //Function called externally to start a timer with a designated action
        public static JUCLTimer Create(Action action, float timer, string timerName = null)
        {
            InitIfNeeded();
            GameObject gameObject = new GameObject("JUCL timer", typeof(MonoBehaviourHook));
            JUCLTimer juclTimer = new JUCLTimer(action, timer, timerName, gameObject);
            gameObject.GetComponent<MonoBehaviourHook>().onUpdate = juclTimer.Update;

            activeTimerList.Add(juclTimer);
            return juclTimer;
        }

        //Hook class for monobehaviour
        public class MonoBehaviourHook : MonoBehaviour
        {
            public Action onUpdate;
            private void Update()
            {
                onUpdate?.Invoke();
            }
        }

        //Timer variables
        private Action action;
        private float timer;
        private GameObject gameObject;
        private string timerName;
        private bool isDestroyed;

        //Constructor
        private JUCLTimer(Action action, float timer, string timerName, GameObject gameObject)
        {
            this.action = action;
            this.timer = timer;
            this.gameObject = gameObject;
            this.timerName = timerName;
            isDestroyed = false;
        }

        //Update function called from monobehaviour hook
        public void Update()
        {
            if (!isDestroyed)
            {
                timer -= Time.deltaTime;
                if (timer < 0.0f)
                {
                    //Trigger action
                    action?.Invoke();
                    DestroySelf();
                }
            }
        }

        //End timer by destroying this object
        private void DestroySelf()
        {
            isDestroyed = true;
            UnityEngine.Object.Destroy(gameObject);
            RemoveTimer(this);
        }
    }
}