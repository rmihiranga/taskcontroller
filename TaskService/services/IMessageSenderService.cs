using System.Threading.Tasks;
using TaskService.Controllers;

namespace TaskService.Services
{
    public interface IMessageSenderService
    {
        Task SendCustomMessage(TaskProgress message);
    }
}
