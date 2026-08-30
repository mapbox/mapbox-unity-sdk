using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace Mapbox.Tests
{
    /// <summary>
    /// Bounded waiting for coroutine tests.
    ///
    /// A bare <c>while (!done) yield return null;</c> has no way to end if the thing it waits on
    /// never happens - a request that fails, a callback that is dropped, a task cancelled at
    /// teardown - so the test hangs until the runner's own timeout, with nothing in the report
    /// saying what it was waiting for. These fail instead, naming the condition.
    /// </summary>
    public static class TestWait
    {
        /// <summary>
        /// Generous by design: these tests reach the live API, and the point is to catch a wait that
        /// will never finish, not to police how long a slow one takes.
        /// </summary>
        public const float DefaultTimeoutSeconds = 30f;

        /// <summary>
        /// Yields until <paramref name="condition"/> is true, failing the test if
        /// <paramref name="timeoutSeconds"/> elapses first. <paramref name="description"/> completes
        /// the sentence "waiting for ...", so phrase it as a noun: "the tile fetch to complete".
        /// </summary>
        /// <summary>
        /// Yields while <paramref name="condition"/> stays true - the direct replacement for
        /// <c>while (cond) yield return null;</c>, so the condition transfers unchanged.
        /// </summary>
        public static IEnumerator While(Func<bool> condition, string description,
            float timeoutSeconds = DefaultTimeoutSeconds)
        {
            return Until(() => !condition(), description, timeoutSeconds);
        }

        public static IEnumerator Until(Func<bool> condition, string description,
            float timeoutSeconds = DefaultTimeoutSeconds)
        {
            // realtimeSinceStartup, not Time.time: EditMode tests do not run the player loop, so
            // Time.time can stand still and the timeout would never trigger.
            var start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > timeoutSeconds)
                {
                    Assert.Fail($"Timed out after {timeoutSeconds:0.#}s waiting for {description}.");
                    yield break;
                }

                yield return null;
            }
        }
    }
}
