using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//Track and hand-roll a coroutine. When it is done,
//set a flag that lets us say we're done, cool.
public class ImpactCoroutineWatcher
{
    private bool bCoroutineFinished;
    private IEnumerator myCoroutine;

    //When this is true, we throw on the brakes and abandon ship.
    private bool bStopThisCoroutine;

    public bool IsFinished()
    {
        return bCoroutineFinished;
    }

    public string GetCoroutineName()
    {
        if (myCoroutine == null)
        {
            return "[null]";
        }

        return myCoroutine.ToString();

    }

    //the loop that runs the coroutine must be 
    //itself a coroutine! I herd you like etc etcetc

    public IEnumerator StartCoroutine( IEnumerator routine)
    {
        myCoroutine = routine;
        while (!bStopThisCoroutine && myCoroutine.MoveNext() )
        {
            //return whatever the coroutine does, which can be something like
            //wait x seconds, wait for next frame, or just a yield return null
            yield return myCoroutine.Current;
        }

        bCoroutineFinished = true;
    }

    public void StopCoroutine()
    {
        //we're gonna set a stop flag that our StartCoroutine loop will then
        //catch and we will abandon ship.
        bStopThisCoroutine = true;
    }

}

/// <summary>
/// Utility functions to handle exceptions thrown from coroutine and iterator functions
/// http://JacksonDunstan.com/articles/3718
/// </summary>
public static class CoroutineUtils
{

    /// <summary>
    /// Start a coroutine that might throw an exception. Call the callback with the exception if it
    /// does or null if it finishes without throwing an exception.
    /// </summary>
    /// <param name="monoBehaviour">MonoBehaviour to start the coroutine on</param>
    /// <param name="enumerator">Iterator function to run as the coroutine</param>
    /// <param name="done">Callback to call when the coroutine has thrown an exception or finished.
    /// The thrown exception or null is passed as the parameter.</param>
    /// <returns>The started coroutine</returns>
     public static Coroutine StartThrowingCoroutine(
        this MonoBehaviour monoBehaviour,
        IEnumerator enumerator,
        Action<Exception> done
    )
    {
        return monoBehaviour.StartCoroutine(RunThrowingIterator(enumerator, done));
    }
    /// <summary>
    /// Run an iterator function that might throw an exception. Call the callback with the exception
    /// if it does or null if it finishes without throwing an exception.
    /// </summary>
    /// <param name="enumerator">Iterator function to run</param>
    /// <param name="done">Callback to call when the iterator has thrown an exception or finished.
    /// The thrown exception or null is passed as the parameter.</param>
    /// <returns>An enumerator that runs the given enumerator</returns>
    public static IEnumerator RunThrowingIterator(
        IEnumerator enumerator,
        Action<Exception> done
    )
    {
        while (true)
        {
            object current;
            try
            {
                if (enumerator.MoveNext() == false)
                {
                    break;
                }
                current = enumerator.Current;
            }
            catch (Exception ex)
            {
                done(ex);
                yield break;
            }
            yield return current;
        }
        done(null);
    } 
} 
