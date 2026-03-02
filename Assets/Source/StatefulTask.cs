using System;
using System.Threading.Tasks;
using UnityEngine;

public class StatefulTask
{
    private bool taskRunning = false;

    // When dirty flag is set, the result of the current running task is invalidated. 
    // So we have to schedule a new task run after the current one finishes.
    private bool dirtyFlag = false;
    private Task workTask;

    private readonly Action taskFunction;
    private readonly Action completeFunction;

    public StatefulTask(Action taskFunction, Action completeFunction = null)
    {
        this.taskFunction = taskFunction;
        this.completeFunction = completeFunction;
    }

    // This function must be called from the main thread
    public void RequestRecompute()
    {
#if UNITY_WEBGL
        // No multithreading in WebGL, just run directly
        taskFunction.Invoke();
        completeFunction?.Invoke();
#else
        dirtyFlag = true;
        if (!taskRunning)
        {
            RunTask();
        }
#endif
    }

    private void RunTask()
    {
        taskRunning = true;
        dirtyFlag = false;
        workTask = Task.Run(() =>
        {
            taskFunction.Invoke();
            App.Instance.ScheduleTask(() =>
            {
                if (dirtyFlag)
                {
                    RunTask();
                }
                else
                {
                    taskRunning = false;
                    completeFunction?.Invoke();
                }
            });
        }).ContinueWith(failedTask => Debug.LogError(failedTask.Exception), TaskContinuationOptions.OnlyOnFaulted); 
    }
}