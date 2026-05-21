using CryptoOrbit.Dtos;
using CryptoOrbit.Interfaces;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading.Tasks;

namespace CryptoOrbit.Services;

public class GeminiServices : IGeminiInterfece
{
    private readonly IChatCompletionService _chatCompletionService;
    public GeminiServices(IChatCompletionService chatCompletionService)
    {
        _chatCompletionService = chatCompletionService;
    }
    public async Task<string> GetInfoCryptoForCoin(string prompt)
    {
        var responseIa = await _chatCompletionService.GetChatMessageContentAsync(prompt);

        string text = responseIa.Content;

        string clearText = text.Replace("```json", "").Replace("```", "").Trim();

        return clearText;
    }
}