using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Utility
{
    public class CRUtil
    {
        public static IEnumerator Race(params IEnumerator[] coroutines)
        {
            bool anyFinished = false;
            while (!anyFinished)
            {
                foreach (var coroutine in coroutines)
                {
                    if (!coroutine.MoveNext())
                    {
                        anyFinished = true;
                        break;
                    }
                }
                yield return null;
            }
        }

        public static IEnumerator WaitForEvent(Action subscribe)
        {
            bool eventTriggered = false;
            void Handler() => eventTriggered = true;

            subscribe += Handler;

            while (!eventTriggered)
            {
                yield return null;
            }

            subscribe -= Handler;
        }

        public static IEnumerator WaitForEvent(UnityEvent evnt)
        {
            bool eventTriggered = false;
            void Handler() => eventTriggered = true;

            evnt.AddListener(Handler);

            while (!eventTriggered)
            {
                yield return null;
            }

            evnt.RemoveListener(Handler);
        }
    }
}
