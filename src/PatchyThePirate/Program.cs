using Microsoft.AspNetCore.Mvc;
using Octokit;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello Copilot!");

string yourGitHubAppName = "PatchyThePirateExt";
string githubCopilotCompletionsUrl = 
    "https://api.githubcopilot.com/chat/completions";

// Exposing agent endpoint for Copilot to call
app.MapPost("/agent", async (
    [FromHeader(Name = "X-GitHub-Token")] string githubToken, 
    [FromBody] Request userRequest) =>
{
    // Identify users using the GitHub API Token provided in the request header
    var octokitClient = 
        new GitHubClient(
            new Octokit.ProductHeaderValue(yourGitHubAppName))
    {
        Credentials = new Credentials(githubToken)
    };
    var user = await octokitClient.User.Current();

    userRequest.Messages.Insert(0, new Message
    {
        Role = "system",
        Content = 
            "Start every response with the user's name, " + 
            $"which is @{user.Login}"
    });
    userRequest.Messages.Insert(0, new Message
    {
        Role = "system",
        Content = 
            "You are a helpful assistant that replies to " +
            "user messages as if you were Patchy the Pirate from Spongebob Squarepants."
    });

    // Create communication back to Copilot
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", githubToken);
    userRequest.Stream = true;

    // Use Copilots LLM to generate a response
    var copilotLLMResponse = await httpClient.PostAsJsonAsync(
    githubCopilotCompletionsUrl, userRequest);

    // Return response back to user.
    var responseStream = 
        await copilotLLMResponse.Content.ReadAsStreamAsync();
    return Results.Stream(responseStream, "application/json");
});

// Callback endpoint to allow users to install your extension as a GitHub App
app.MapGet("/callback", () => "You may close this tab and " + 
    "return to GitHub.com (where you should refresh the page " +
    "and start a fresh chat). If you're using VS Code or " +
    "Visual Studio, return there.");

app.Run();
