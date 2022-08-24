using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tetris
{
    /// <summary>
    /// Contains helpful methods for <see cref="Coroutine"/>s. Such as Timer and Cached <see cref="YieldInstruction"/>s.
    /// </summary>
    public static class CoroutineUtility
    {
        #region Cached Coroutine Instructions
        //-- WaitForEndOfFrame and WaitForFixedUpdate

        /// <summary>
        /// A cached <see cref="UnityEngine.WaitForEndOfFrame"/>. <para/>
        /// This is more optimized than creating a new <see cref="UnityEngine.WaitForEndOfFrame"/> every time we want to wait for a single frame.
        /// </summary>
        public static readonly WaitForEndOfFrame WaitForEndOfFrame = new();
        /// <summary>
        /// A cached <see cref="UnityEngine.WaitForFixedUpdate"/>. <para/>
        /// This is more optimized than creating a new <see cref="UnityEngine.WaitForFixedUpdate"/> every time we want to wait for a single frame.
        /// </summary>
        public static readonly WaitForFixedUpdate WaitForFixedUpdate = new();

        //-- WaitForSeconds

        private static Dictionary<float, WaitForSeconds> _cachedWaitDictionary = new();
        /// <summary>
        /// Finds a cached <see cref="WaitForSeconds"/> using a dictionary. If one doesn't exist, then a new one is created. <para/>
        /// This is more optimized than creating a new <see cref="WaitForSeconds"/> every time we want to wait a bit.
        /// </summary>
        /// <param name="time">The time to wait in seconds.</param>
        /// <returns>The cached <see cref="WaitForSeconds"/>.</returns>
        public static WaitForSeconds GetWait(float time)
        {
            // Try get the cached WaitForSeconds if it already exists
            if (_cachedWaitDictionary.TryGetValue(time, out WaitForSeconds waitSec))
            {
                return waitSec;
            }

            // If it doesn't exist, then create a new one and cache it
            _cachedWaitDictionary[time] = new(time);

            return _cachedWaitDictionary[time];
        }

        //-- WaitForSecondsRealtime

        private static Dictionary<float, WaitForSecondsRealtime> _cachedWaitRealtimeDictionary = new();
        /// <summary>
        /// Finds a cached <see cref="WaitForSecondsRealtime"/> using a dictionary. If one doesn't exist, then a new one is created. <para/>
        /// This is more optimized than creating a new <see cref="WaitForSecondsRealtime"/> every time we want to wait a bit.
        /// </summary>
        /// <param name="time">The time to wait in seconds.</param>
        /// <returns>The cached <see cref="WaitForSecondsRealtime"/>.</returns>
        public static WaitForSecondsRealtime GetWaitRealtime(float time)
        {
            // Try get the cached WaitForSeconds if it already exists
            if (_cachedWaitRealtimeDictionary.TryGetValue(time, out WaitForSecondsRealtime waitSec))
            {
                return waitSec;
            }

            // If it doesn't exist, then create a new one and cache it
            _cachedWaitRealtimeDictionary[time] = new(time);

            return _cachedWaitRealtimeDictionary[time];
        }

        //-- WaitUntil

        private static Dictionary<Func<bool>, WaitUntil> _cachedWaitUntilDictionary = new();
        /// <summary>
        /// Finds a cached <see cref="WaitUntil"/> using a dictionary. If one doesn't exist, then a new one is created. <para/>
        /// This is more optimized than creating a new <see cref="WaitUntil"/> every time we want to wait a bit.
        /// </summary>
        /// <returns>The cached <see cref="WaitUntil"/>.</returns>
        public static WaitUntil GetWaitUntil(Func<bool> predicate)
        {
            // Try get the cached WaitForSeconds if it already exists
            if (_cachedWaitUntilDictionary.TryGetValue(predicate, out WaitUntil waitSec))
            {
                return waitSec;
            }

            // If it doesn't exist, then create a new one and cache it
            _cachedWaitUntilDictionary[predicate] = new(predicate);

            return _cachedWaitUntilDictionary[predicate];
        }

        //-- WaitWhile

        private static Dictionary<Func<bool>, WaitWhile> _cachedWaitWhileDictionary = new();
        /// <summary>
        /// Finds a cached <see cref="WaitWhile"/> using a dictionary. If one doesn't exist, then a new one is created. <para/>
        /// This is more optimized than creating a new <see cref="WaitWhile"/> every time we want to wait a bit.
        /// </summary>
        /// <returns>The cached <see cref="WaitWhile"/>.</returns>
        public static WaitWhile GetWaitWhile(Func<bool> predicate)
        {
            // Try get the cached WaitForSeconds if it already exists
            if (_cachedWaitWhileDictionary.TryGetValue(predicate, out WaitWhile waitSec))
            {
                return waitSec;
            }

            // If it doesn't exist, then create a new one and cache it
            _cachedWaitWhileDictionary[predicate] = new(predicate);

            return _cachedWaitWhileDictionary[predicate];
        }
        #endregion

        #region Timers
        /// <summary>
        /// Waits until the given <see cref="YieldInstruction"/> is finished, then performs the <paramref name="onComplete"/> <see cref="Action"/>.
        /// </summary>
        /// <param name="script">The <see cref="MonoBehaviour"/> the <see cref="Coroutine"/> is ran on.</param>
        /// <param name="onComplete">The <see cref="Action"/> called when the <paramref name="instruction"/> has been successfully performed.</param>
        /// <param name="instruction">The <see cref="YieldInstruction"/> performed in the <see cref="Coroutine"/>.</param>
        /// <param name="repeatAmount">The amount of times to repeat the instruction. Any input below 1 will simply make the method do nothing.</param>
        /// <returns>The <see cref="Coroutine"/> that was ran. Use <see cref="MonoBehaviour.StopCoroutine(Coroutine)"/> to stop the returned <see cref="Coroutine"/>.</returns>
        public static Coroutine StartTimer(this MonoBehaviour script, Action onComplete, YieldInstruction instruction, int repeatAmount = 1)
        {
            return script.StartCoroutine(TimerIEnumerator(onComplete, instruction, repeatAmount));
        }

        /// <summary>
        /// Waits until the given <see cref="CustomYieldInstruction"/> is finished, then performs the <paramref name="onComplete"/> <see cref="Action"/>.
        /// </summary>
        /// <param name="script">The <see cref="MonoBehaviour"/> the <see cref="Coroutine"/> is ran on.</param>
        /// <param name="onComplete">The <see cref="Action"/> called when the <paramref name="instruction"/> has been successfully performed.</param>
        /// <param name="instruction">The <see cref="CustomYieldInstruction"/> performed in the <see cref="Coroutine"/>.</param>
        /// <param name="repeatAmount">The amount of times to repeat the instruction. Any input below 1 will simply make the method do nothing.</param>
        /// <returns>The <see cref="Coroutine"/> that was ran. Use <see cref="MonoBehaviour.StopCoroutine(Coroutine)"/> to stop the returned <see cref="Coroutine"/>.</returns>
        public static Coroutine StartTimer(this MonoBehaviour script, Action onComplete, CustomYieldInstruction instruction, int repeatAmount = 1)
        {
            return script.StartCoroutine(TimerIEnumerator(onComplete, instruction, repeatAmount));
        }

        /// <summary>
        /// Waits until the given <see cref="YieldInstruction"/> is finished, then performs the <paramref name="OnComplete"/> <see cref="Action"/>.
        /// </summary>
        /// <param name="OnComplete">The <see cref="Action"/> called when the <paramref name="instruction"/> has been successfully performed.</param>
        /// <param name="instruction">The <see cref="YieldInstruction"/> performed in the <see cref="Coroutine"/>.</param>
        /// <param name="repeatAmount">The amount of times to repeat the instruction. Any input below 1 will simply make the method do nothing.</param>
        /// <returns>The <see cref="IEnumerator"/>. Use <see cref="MonoBehaviour.StartCoroutine(IEnumerator)"/> to start a <see cref="Coroutine"/>.</returns>
        public static IEnumerator TimerIEnumerator(Action OnComplete, YieldInstruction instruction, int repeatAmount = 1)
        {
            // Do nothing if repeat amount is below 1
            if (repeatAmount < 1)
            {
                yield break;
            }
            // Only yield once if repeat amount is 1
            else if (repeatAmount == 1)
            {
                yield return instruction;
            }
            // Repeat amount if above 1
            else
            {
                // Repeat instruction
                for (int i = 0; i < repeatAmount; i++)
                {
                    yield return instruction;
                }
            }

            OnComplete?.Invoke();
        }
        /// <summary>
        /// Waits until the given <see cref="IEnumerator"/> is finished, then performs the <paramref name="onComplete"/> <see cref="Action"/>.
        /// </summary>
        /// <param name="onComplete">The <see cref="Action"/> called when the <paramref name="instruction"/> has been successfully performed.</param>
        /// <param name="instruction">The <see cref="IEnumerator"/> performed in the <see cref="Coroutine"/>.</param>
        /// <param name="repeatAmount">The amount of times to repeat the instruction. Any input below 1 will simply make the method do nothing.</param>
        /// <returns>The <see cref="IEnumerator"/>. Use <see cref="MonoBehaviour.StartCoroutine(IEnumerator)"/> to start a <see cref="Coroutine"/>.</returns>
        public static IEnumerator TimerIEnumerator(Action onComplete, IEnumerator instruction, int repeatAmount = 1)
        {
            // Do nothing if repeat amount is below 1
            if (repeatAmount < 1)
            {
                yield break;
            }
            // Only yield once if repeat amount is 1
            else if (repeatAmount == 1)
            {
                yield return instruction;
            }
            // Repeat amount if above 1
            else
            {
                // Repeat instruction
                for (int i = 0; i < repeatAmount; i++)
                {
                    yield return instruction;
                }
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// Starts a timer which waits for the given <paramref name="time"/>, then performs the <paramref name="onComplete"/> <see cref="Action"/>.
        /// </summary>
        /// <param name="script">The <see cref="MonoBehaviour"/> the <see cref="Coroutine"/> is ran on.</param>
        /// <param name="time">The time to wait before <paramref name="onComplete"/> is called.</param>
        /// <param name="onComplete">The <see cref="Action"/> called when the right amount of <paramref name="time"/> has passed.</param>
        /// <param name="isUnscaled">If true, then this timer will instead use <see cref="WaitForSecondsRealtime"/> instead of <see cref="WaitForSeconds"/>.</param>
        /// <returns>The <see cref="Coroutine"/> that was ran. Use <see cref="MonoBehaviour.StopCoroutine(Coroutine)"/> to stop the returned <see cref="Coroutine"/>.</returns>
        public static Coroutine Timer(this MonoBehaviour script, float time, Action onComplete, bool isUnscaled = false)
        {
            // Regular time
            if (!isUnscaled)
            {
                return script.StartTimer(onComplete, GetWait(time));
            }
            // Unscaled time
            else
            {
                return script.StartTimer(onComplete, GetWaitRealtime(time));
            }
        }

        /// <summary>
        /// Starts a timer which waits for the next frame, then performs the <paramref name="onComplete"/> <see cref="Action"/>.
        /// </summary>
        /// <param name="script">The <see cref="MonoBehaviour"/> the <see cref="Coroutine"/> is ran on.</param>
        /// <param name="onComplete">The <see cref="Action"/> called when the frame is finished.</param>
        /// <param name="frameAmount">The amount of frames to wait. Any input below 1 will simply make the method do nothing.</param>
        /// <param name="isFixedFrame">If true, then this timer will instead use <see cref="UnityEngine.WaitForFixedUpdate"/> instead of <see cref="UnityEngine.WaitForEndOfFrame"/>.</param>
        /// <returns>The <see cref="Coroutine"/> that was ran. Use <see cref="MonoBehaviour.StopCoroutine(Coroutine)"/> to stop the returned <see cref="Coroutine"/>.</returns>
        public static Coroutine TimerFrame(this MonoBehaviour script, Action onComplete, int frameAmount = 1, bool isFixedFrame = false)
        {
            // Regular update
            if (!isFixedFrame)
            {
                return script.StartTimer(onComplete, WaitForEndOfFrame, frameAmount);
            }
            // Fixed update
            else
            {
                return script.StartTimer(onComplete, WaitForFixedUpdate, frameAmount);
            }
        }

        /// <summary>
        /// Starts a timer which waits until the given <paramref name="predicate"/> becomes true, then performs the <paramref name="onComplete"/> <see cref="Action"/>.
        /// </summary>
        /// <param name="script">The <see cref="MonoBehaviour"/> the <see cref="Coroutine"/> is ran on.</param>
        /// <param name="onComplete">The <see cref="Action"/> called when the <paramref name="predicate"/> becomes true.</param>
        /// <returns>The <see cref="Coroutine"/> that was ran. Use <see cref="MonoBehaviour.StopCoroutine(Coroutine)"/> to stop the returned <see cref="Coroutine"/>.</returns>
        public static Coroutine TimerWaitUntil(this MonoBehaviour script, Func<bool> predicate, Action onComplete)
        {
            return script.StartTimer(onComplete, GetWaitUntil(predicate));
        }
        /// <summary>
        /// Starts a timer which waits while the given <paramref name="predicate"/> is true, then performs the <paramref name="onComplete"/> <see cref="Action"/>.
        /// </summary>
        /// <param name="script">The <see cref="MonoBehaviour"/> the <see cref="Coroutine"/> is ran on.</param>
        /// <param name="onComplete">The <see cref="Action"/> called when the <paramref name="predicate"/> becomes false.</param>
        /// <returns>The <see cref="Coroutine"/> that was ran. Use <see cref="MonoBehaviour.StopCoroutine(Coroutine)"/> to stop the returned <see cref="Coroutine"/>.</returns>
        public static Coroutine TimerWaitWhile(this MonoBehaviour script, Func<bool> predicate, Action onComplete)
        {
            return script.StartTimer(onComplete, GetWaitWhile(predicate));
        }
        #endregion
    }
}
