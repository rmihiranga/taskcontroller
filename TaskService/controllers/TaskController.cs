using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using TaskService.Services;

namespace TaskService.Controllers
{
    [ApiController]
    [Route("")]
    public class TaskController : ControllerBase
    {
        private static ConcurrentQueue<TaskItem> _tasks = new ConcurrentQueue<TaskItem>();
        private static ConcurrentDictionary<Guid, TaskItem> _tasksDict = new ConcurrentDictionary<Guid, TaskItem>();
        private static ConcurrentDictionary<Guid, CancellationTokenSource> _taskTokens = new ConcurrentDictionary<Guid, CancellationTokenSource>();

        private readonly IMessageSenderService _messageSenderService;

        public TaskController(IMessageSenderService messageSenderService)
        {
            _messageSenderService = messageSenderService;
        }

        [HttpPost("submit_tasks")]
        public IActionResult SubmitTasks([FromBody] TaskRequest request)
        {
            // Check if request is null or TaskDurationsInSeconds is null or empty
            if (request?.TaskDurationsInSeconds == null || request.TaskDurationsInSeconds.Length == 0)
            {
                return BadRequest("Task durations should not be empty");
            }

            // Validate that TaskDurationsInSeconds is an integer array
            if (!request.TaskDurationsInSeconds.All(d => d is int))
            {
                return NotFound("Input parameter should be an integer array.");
            }

            // Check if any duration is out of the valid range
            if (request.TaskDurationsInSeconds.Any(d => d < 10 || d > 25))
            {
                return BadRequest("Task durations should be within range 10-25");
            }


            foreach (var duration in request.TaskDurationsInSeconds)
            {
                var len = _tasksDict.Count;
                var taskItem = new TaskItem { Id = Guid.NewGuid(), Name = "Task " + (len + 1), Seq = len + 1, Duration = duration};
                _tasks.Enqueue(taskItem);
                _tasksDict.TryAdd(taskItem.Id, taskItem);
            }

            return Accepted();
        }

        [HttpGet("tasks")]
        public IActionResult GetTasks()
        {
            
            var sortedTasks = _tasksDict.Values.OrderByDescending(t => t.Seq).ToList();
            return Ok(sortedTasks);
        }

        [HttpPost("start")]
        public IActionResult StartTasks()
        {
            // _isProcessStarted = true;

            processQueue();
            return Ok();
        }

        private void processQueue()
        {
            while (_tasks.TryDequeue(out var task))
            {
                var cts = new CancellationTokenSource();
                _taskTokens.TryAdd(task.Id, cts);

                task.Status = "In Progress";
                _tasksDict.AddOrUpdate(task.Id, task, (key, existingTask) => task);

                Task.Run(async () =>
                {
                    try
                    {
                        for (int i = 0; i <= 100; i += 10)
                        {
                            await Task.Delay(task.Duration * 1000 / 10, cts.Token);
                            await _messageSenderService.SendCustomMessage(new TaskProgress { Id = task.Id, Progress = i });
                        }
                        task.Status = "Completed";
                        _tasksDict.AddOrUpdate(task.Id, task, (key, existingTask) => task);
                    }
                    catch (TaskCanceledException)
                    {
                        _tasks.Enqueue(task);
                        task.Status = "Pending";
                        _tasksDict.AddOrUpdate(task.Id, task, (key, existingTask) => task);
                    }
                    finally
                    {
                        _taskTokens.TryRemove(task.Id, out _);
                    }
                });
            }
        }

        [HttpPost("stop")]
        public IActionResult StopTasks()
        {
            foreach (var cts in _taskTokens.Values)
            {
                cts.Cancel();
            }

            _taskTokens.Clear();

            return Ok();
        }
    }

    public class TaskRequest
    {
        public required int[] TaskDurationsInSeconds { get; set; }
    }

    public class TaskItem
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public int Seq { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; } = "Pending";
    }

    public class TaskProgress
    {
        public Guid Id { get; set; }
        public int Progress {get; set;}
    }
}
