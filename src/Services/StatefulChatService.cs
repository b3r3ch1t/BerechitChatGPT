 

using BerechitChatGPT.Models;
using LLama;

namespace BerechitChatGPT.Services;

public class StatefulChatService : IDisposable
{
    private readonly ChatSession _session;
    private readonly LLamaContext _context;
    private readonly ILogger<StatefulChatService> _logger;
    private bool _continue = false;

    private const string SystemPrompt = "Transcript of a dialog, where the User interacts with you. You are a linguistics specialist with expertise in translations, and is able to translate text with high accuracy. You is also capable of providing linguistic insights, explanations, and clarifications when requested. You is helpful, precise, and always responds to the User's requests with professionalism.";

    public StatefulChatService(IConfiguration configuration, ILogger<StatefulChatService> logger)
    {
        var @params = new LLama.Common.ModelParams(configuration["ModelPath"]!)
        {
            ContextSize = 512,
        };

         
        using var weights = LLamaWeights.LoadFromFile(@params);

        _logger = logger;
        _context = new LLamaContext(weights, @params);

        _session = new ChatSession(new InteractiveExecutor(_context));
        _session.History.AddMessage(LLama.Common.AuthorRole.System, SystemPrompt);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    public async Task<string> Send(SendMessageInput input)
    {

        if (!_continue)
        {
            _logger.LogInformation("Prompt: {text}", SystemPrompt);
            _continue = true;
        }
        _logger.LogInformation("Input: {text}", input.Text);
        var outputs = _session.ChatAsync(
            new LLama.Common.ChatHistory.Message(LLama.Common.AuthorRole.User, input.Text),
            new LLama.Common.InferenceParams()
            { 
                AntiPrompts = new string[] { "User:" },
            });

        var result = "";
        await foreach (var output in outputs)
        {
            _logger.LogInformation("Message: {output}", output);
            result += output; 

        }

        return StripRoles(result) ;
    }

    private string StripRoles(string text)
    {
        var strippedText = text;

        // Remove "You:" from the start of the string
        if (strippedText.StartsWith("You:"))
        {
            strippedText = strippedText.Substring("You:".Length).TrimStart();
        }

        // Remove "User:" from the end of the string
        if (strippedText.EndsWith("User:"))
        {
            strippedText = strippedText.Substring(0, strippedText.Length - "User:".Length).TrimEnd();
        }

        return strippedText;
    }


    public async IAsyncEnumerable<string> SendStream(SendMessageInput input)
    {
        if (!_continue)
        {
            _logger.LogInformation(SystemPrompt);
            _continue = true;
        }

        _logger.LogInformation(input.Text);

        var outputs = _session.ChatAsync(
            new LLama.Common.ChatHistory.Message(LLama.Common.AuthorRole.User, input.Text!)
            , new LLama.Common.InferenceParams()
            {
                
                AntiPrompts = new string[] { "User:" },
            });

        await foreach (var output in outputs)
        {
            _logger.LogInformation(output);
            yield return output;
        }
    }
}
