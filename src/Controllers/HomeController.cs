using BerechitChatGPT.Models;
using BerechitChatGPT.Services;
using Microsoft.AspNetCore.Mvc;

namespace BerechitChatGPT.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly StatefulChatService _statefulChatService;
        public HomeController(ILogger<HomeController> logger, StatefulChatService statefulChatService)
        {
            _logger = logger;
            _statefulChatService = statefulChatService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Validate the anti-CSRF token
        public async Task<IActionResult> SendMessage(string userInput)
        {
            // Here you can process the user's input
            _logger.LogInformation($"Message received: {userInput}");

            var sendMessageInput = new SendMessageInput()
            {
                Text = userInput,
            };

            var result = await _statefulChatService
                .Send(sendMessageInput);

            // Return a JSON response with a success message or processed data
            return Json(new { success = true, message = result });
        }

        // Return the default view
        public IActionResult Index()
        {
            return View();
        }
    }
}