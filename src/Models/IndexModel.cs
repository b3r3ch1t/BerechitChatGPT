using BerechitChatGPT.Common;
using Microsoft.AspNetCore.Mvc;

namespace BerechitChatGPT.Models
{
	public class IndexModel
	{
		public LLamaOptions Options { get; set; }

		[BindProperty]
		public ISessionConfig SessionConfig { get; set; }

		[BindProperty]
		public InferenceOptions InferenceOptions { get; set; }

	}
}
