using Microsoft.AspNetCore.SignalR;
using TaskService.Controllers;

namespace TaskService.Services
{
    public class MessageSenderService : IMessageSenderService, IHostedService
    {
        private readonly IHubContext<MessageHub> _hubContext;
        private readonly ILogger<MessageSenderService> _logger;

        public MessageSenderService(IHubContext<MessageHub> hubContext, ILogger<MessageSenderService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendCustomMessage(TaskProgress message)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
                _logger.LogInformation($"Custom message sent: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending custom message: {ex.Message}");
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MessageSenderService is starting.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MessageSenderService is stopping.");
            return Task.CompletedTask;
        }
    }
}
