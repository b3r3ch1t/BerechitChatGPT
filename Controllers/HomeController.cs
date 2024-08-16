using Microsoft.AspNetCore.Mvc;

namespace BerechitChatGPT.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Validate the anti-CSRF token
        public async Task<IActionResult> SendMessage(string userInput)
        {
            // Here you can process the user's input
            _logger.LogInformation($"Message received: {userInput}");

            // Return a JSON response with a success message or processed data
            return Json(new { success = true, message = userInput });
        }

        // Return the default view
        public IActionResult Index()
        {
            return View();
        }
    }
}