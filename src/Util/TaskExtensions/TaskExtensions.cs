using System;
using System.Threading.Tasks;

namespace ReFlex.Apps.DeepZoom.Util.TaskExtensions;

/// <summary>
/// Tqaken from "5 useful extensions for Task&lt;T&gt; in .NET" (https://steven-giesel.com/blogPost/d38e70b4-6f36-41ff-8011-b0b0d1f54f6e)
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// Sometimes you want to fire and forget a task.
    /// This means that you want to start a task, but you don't want to wait for it to finish.
    /// This is useful when you want to start a task, but you don't care about the result (non-critical tasks).
    /// For example when you want to start a task that sends an email.
    /// You don't want to wait for the email to be sent before you can continue with your code.
    /// So you can use the FireAndForget extension method to start the task and forget about it.
    /// Optionally you can pass an error handler to the method.
    /// This error handler will be called when the task throws an exception.
    ///
    /// usage:
    /// <code>SendEmailAsync().FireAndForget(errorHandler => Console.WriteLine(errorHandler.Message));</code>
    /// </summary>
    /// <param name="task"></param>
    /// <param name="errorHandler"></param>
    public static void FireAndForget(
        this Task task,
        Action<Exception> errorHandler = null)
    {
        task.ContinueWith(t =>
        {
            if (t.IsFaulted && errorHandler != null)
                errorHandler(t.Exception);
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
    
    /// <summary>
    /// If you want to retry a task a specific number of times, you can use the Retry extension method.
    /// This method will retry the task until it succeeds or the maximum number of retries is reached.
    /// You can pass a delay between retries. This delay will be used between each retry.
    ///
    /// usage:
    /// <code>var result = await (() => GetResultAsync()).Retry(3, TimeSpan.FromSeconds(1));</code>
    /// </summary>
    /// <param name="taskFactory"></param>
    /// <param name="maxRetries"></param>
    /// <param name="delay"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static async Task<TResult> Retry<TResult>(this Func<Task<TResult>> taskFactory, int maxRetries, TimeSpan delay)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return await taskFactory().ConfigureAwait(false);
            }
            catch
            {
                if (i == maxRetries - 1)
                    throw;
                await Task.Delay(delay).ConfigureAwait(false);
            }
        }

        return default(TResult); // Should not be reached
    }
    
    /// <summary>
    /// Executes a callback function when a Task encounters an exception.
    ///
    /// usage:
    /// <code>await GetResultAsync().OnFailure(ex => Console.WriteLine(ex.Message));</code>
    /// </summary>
    /// <param name="task"></param>
    /// <param name="onFailure"></param>
    public static async Task OnFailure(this Task task, Action<Exception> onFailure)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            onFailure(ex);
        }
    }
    /// <summary>
    /// Sometimes you want to set a timeout for a task.
    /// This is useful when you want to prevent a task from running for too long.
    /// You can use the Timeout extension method to set a timeout for a task.
    /// If the task takes longer than the timeout the task will be cancelled.
    ///
    /// usage:
    /// <code>await GetResultAsync().WithTimeout(TimeSpan.FromSeconds(1));</code>
    ///
    /// <remarks>Since .NET 6 you can use WaitAsync (https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.waitasync).</remarks>
    /// </summary>
    /// <param name="task"></param>
    /// <param name="timeout"></param>
    /// <exception cref="TimeoutException"></exception>
    public static async Task WithTimeout(this Task task, TimeSpan timeout)
    {
        var delayTask = Task.Delay(timeout);
        var completedTask = await Task.WhenAny(task, delayTask).ConfigureAwait(false);
        if (completedTask == delayTask)
            throw new TimeoutException();

        await task;
    }
    
    /// <summary>
    /// Sometimes you want to use a fallback value when a task fails. You can use the Fallback extension method to use a fallback value when a task fails.
    /// 
    /// usage:
    /// <code>var result = await GetResultAsync().Fallback("fallback");</code>
    /// </summary>
    /// <param name="task"></param>
    /// <param name="fallbackValue"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    public static async Task<TResult> Fallback<TResult>(this Task<TResult> task, TResult fallbackValue)
    {
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch
        {
            return fallbackValue;
        }
    }
    
}