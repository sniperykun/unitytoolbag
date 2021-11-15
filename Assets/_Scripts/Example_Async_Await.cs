using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using UnityToolBag;
using System;

// https://devblogs.microsoft.com/pfxteam/await-synchronizationcontext-and-console-apps/
// https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/task-based-asynchronous-programming
// https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/task-cancellation
// https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/exception-handling-task-parallel-library
// https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/chaining-tasks-by-using-continuation-tasks
/*
    Unity does not automatically stop async tasks that run on managed threads when you exit Play Mode.
    U should cancel by your own code


    if u want to use real-multi-threading-task
        u should use Task.Run()
        
        // in unity's main thread
        await Task.Run(){}
        // auto back to unity's main thread

    Async/Await is a state-machine

    await -> GetAwaiter()
        CustomAwaiter : ICriticalNotifyCompletion
            bool IsCompleted(){}
            void GetResult(){}
            void OnCompleted(){}
            void UnsafeOnCompleted(){}
*/


// U need to catch task exception manually
// or exception will be hidden

/*
        // 
        // How to cancel task in C#
        // 
        var tokenSource2 = new CancellationTokenSource();
        CancellationToken ct = tokenSource2.Token;

        var task = Task.Run(() =>
        {
            // Were we already canceled?
            ct.ThrowIfCancellationRequested();

            bool moreToDo = true;
            while (moreToDo)
            {
                // Poll on this property if you have to do
                // other cleanup before throwing.
                if (ct.IsCancellationRequested)
                {
                    // Clean up here, then...
                    ct.ThrowIfCancellationRequested();
                }
            }
        }, tokenSource2.Token); // Pass same token to Task.Run.

        tokenSource2.Cancel();

        // Just continue on this thread, or await with try-catch:
        try
        {
            await task;
        }
        catch (OperationCanceledException e)
        {
            Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e.Message}");
        }
        finally
        {
            tokenSource2.Dispose();
        }

*/

//
// Example for asyn/await in Unity
//
public class Example_Async_Await : MonoBehaviour
{
    public float _speed = 800f;
    public Transform _target;

    private bool IsMainThread
    {
        get
        {
            return _mainThreadId == Thread.CurrentThread.ManagedThreadId;
        }
    }

    private void PrintThread()
    {
        Debug.Log("in main thread:" + (IsMainThread));
    }

    private int _mainThreadId;

    void Awake()
    {
        _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        MainThreadDispatcher.InitMainThreadDispatcher();
    }

    private async Task DoHeavyThings()
    {
        // in main thread
        await Task.Run(() =>
        {
            // in thread pool
            for (int i = 0; i < 100000000; i++)
            {
            }
            Thread.Sleep(2000);
        });
        // in main thread
        Debug.Log("DoHeavyThings Done...");
    }

    private CancellationTokenSource _tokenSource;

    private void MakeCancel()
    {
        // make cancel manually
        
        /*
        */
    
        Debug.LogError("Make cancellation");
        _tokenSource.Cancel();
    }

    private void PrintCurrentContext()
    {
        if (SynchronizationContext.Current != null)
        {
            Debug.Log("---SynchronizationContext type:" + SynchronizationContext.Current.GetType());
        }
        else
        {
            // it’s null (unless you explicitly override that with SynchronizationContext.SetSynchronizationContext). 
            // So the continuations just get scheduled to run on the ThreadPool. 
            // That’s where the rest of those threads are coming from… they’re all ThreadPool threads.
            // just run on the thread pool
            Debug.Log("---SynchronizationContext is null---");
        }
    }

    private async Task WithUnityContent()
    {
        Debug.Log("========== before Task.Yield() frame count:" + Time.frameCount);
        await Task.Yield();
        Debug.Log("========== after Task.Yield() frame count:" + Time.frameCount);
        PrintCurrentContext();
    }

    private async Task TestMessageDispatcher(CancellationToken token)
    {
        try
        {
            PrintThread();
            Debug.Log("start frame count:" + Time.frameCount);
            await Task.Delay(2000);
            Debug.Log("after 2000ms frame count:" + Time.frameCount);
            PrintCurrentContext();
            Debug.Log("---------------");
            await Task.Run(() =>
                        {
                            // in thread pool's background thread
                            PrintCurrentContext();
                            try
                            {
                                PrintThread();
                                Debug.Log("Task ----- Start");
                                Thread.Sleep(3000);
                                token.ThrowIfCancellationRequested();
                                MainThreadDispatcher.InvokeOnMainThread(() =>
                                {
                                    PrintThread();
                                    Debug.Log("Task ----- frame count:" + Time.frameCount);
                                });
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError("Exception:" + ex.Message + ",exception:" + ex.GetType());
                                if (ex is OperationCanceledException)
                                {
                                    Debug.LogError("cancled man");
                                    throw ex;
                                }
                            }
                        }, token);
            // back to unity's main thread
            Debug.Log("before DoHeavy Things frame count:" + Time.frameCount + ",time:" + Time.realtimeSinceStartup);
            await DoHeavyThings();
            Debug.Log("after DoHeavy Things:");
            PrintThread();
            Debug.Log("after DoHeavy Things frame count:" + Time.frameCount + ",time:" + Time.realtimeSinceStartup);
            await Task.Delay(2000);

            Debug.Log("========== start ");
            await WithUnityContent();
            Debug.Log("all task done...");
            PrintCurrentContext();
        }
        catch (Exception ex)
        {
            Debug.LogError("Big Exception:" + ex.Message);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _tokenSource = new CancellationTokenSource();
            TestMessageDispatcher(_tokenSource.Token);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            MakeCancel();
        }

        // make cube rotating no-block main thread
        if (_target != null)
        {
            _target.Rotate(Vector3.up, Time.deltaTime * _speed);
        }
    }

    void OnDestroy()
    {
        if (_tokenSource != null)
        {
            MakeCancel();
        }
    }
}


