using Microsoft.AspNetCore.Mvc;
using LLama.Common;
using LLama;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BerechitChatGPT.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private static ChatSession _session;
		private static InteractiveExecutor _executor;

		public HomeController(ILogger<HomeController> logger)
		{
			_logger = logger;

			// Initialize the session and executor if not already done
			if (_session == null || _executor == null)
			{
				InitializeChatSession();
			}
		}

		// Method to initialize the chat session
		private void InitializeChatSession()
		{
			var modelPath = Program.ModelPath;

			var parameters = new ModelParams(modelPath)
			{
				ContextSize = 4096, // Set your context size
				GpuLayerCount = 5 // Set your GPU layer count based on your hardware
			};

			var model = LLamaWeights.LoadFromFile(parameters);
			var context = model.CreateContext(parameters);
			_executor = new InteractiveExecutor(context);
			 


			//var formalInstruction = "Please translate all text using formal language and avoid the use of slang.";
			//_executor.Context.s(formalInstruction); // Set the instruction without showing it in the chat

			// Initializing the chat history with custom initial message
			var chatHistory = new ChatHistory();
			chatHistory.AddMessage(AuthorRole.Assistant, "Hello, my name is BOB. I specialize in translations using formal language and avoiding the use of slang. How can I assist you today?");
			_session = new ChatSession(_executor, chatHistory);
		}


		// Handles the user's message input
		[HttpPost]
		public async Task<IActionResult> SendMessage(string userInput)
		{
			string result;

			if (userInput == "regenerate")
			{
				result = await RegenerateLastResponse();
			}
			else
			{
				result = await ProcessMessageAsync(userInput);
			}

			// Add the new messages to the chat history and pass it to the view
			var chatHistory = _session.History;
			chatHistory.AddMessage(AuthorRole.User, userInput);
			chatHistory.AddMessage(AuthorRole.Assistant, result);

			ViewBag.ChatHistory = chatHistory.Messages.Select(m => new Tuple<string, string>(m.AuthorRole.ToString(), m.Content)).ToList();
			return View("Index");
		}

		// Process the user's message asynchronously
		private async Task<string> ProcessMessageAsync(string userInput)
		{
			var inferenceParams = new InferenceParams()
			{
				MaxTokens = 1024, // Limit the response length
				AntiPrompts = new List<string> { "User:" } // Stop generating when the user's prompt is repeated
			};

			var response = "";
			await foreach (var text in _session.ChatAsync(new ChatHistory.Message(AuthorRole.User, userInput), inferenceParams))
			{
				response += text;
			}

			return response;
		}

		// Regenerate the last response
		private async Task<string> RegenerateLastResponse()
		{
			var inferenceParams = new InferenceParams()
			{
				MaxTokens = 1024, // Limit the response length
				AntiPrompts = new List<string> { "User:" }
			};

			var response = "";
			await foreach (var text in _session.RegenerateAssistantMessageAsync(inferenceParams))
			{
				response += text;
			}

			return response;
		}

		// Return the default view
		public IActionResult Index()
		{
			// Pass the current chat history to the view
			var chatHistory = _session.History ;
			ViewBag.ChatHistory = chatHistory.Messages.Select(m => new Tuple<string, string>(m.AuthorRole.ToString(), m.Content)).ToList();

			return View();
		}
	}
}
