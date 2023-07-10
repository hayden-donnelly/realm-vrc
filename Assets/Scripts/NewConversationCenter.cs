using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenAI;
using UnityEngine.SceneManagement;

public class NewConversationCenter
{
    private OpenAIApi openai;
    private Prompts prompts;
    private CustomTTS customTTS;
    private List<ChatMessage> conversationHistory;
    private Dictionary<int, string> sceneNames;

    public NewConversationCenter()
    {
        openai = new OpenAIApi();
        prompts = new Prompts();
        customTTS = new CustomTTS();
        InitializeConversationHistory();
        sceneNames = new Dictionary<int, string>()
        {
            { 1, "Space" },
            { 2, "SampleScene2" }
        };
    }

    public void InitializeConversationHistory()
    {
        conversationHistory = new List<ChatMessage>();
        conversationHistory.Add(new ChatMessage() 
        { 
            Role = "system", Content = prompts.MainPrompt 
        });
    }

    public async Task<string> GetReply(string text, string context = null)
    {
        if(context != null)
        {
            conversationHistory.Add(new ChatMessage() { Role = "system", Content = context });
        }
        conversationHistory.Add(new ChatMessage() { Role = "user", Content = text });
        var response = await openai.CreateChatCompletion(new CreateChatCompletionRequest()
        {
            Model = "gpt-3.5-turbo-0301", 
            Messages = conversationHistory
        });

        string responseText = "";
        if(response.Choices != null && response.Choices.Count > 0)
        {
            responseText = response.Choices[0].Message.Content.Trim();
        }
        return responseText;
    }

    public async Task<string> QueryDomainExpert(string text, string prompt)
    {
        List<ChatMessage> messages = new List<ChatMessage>() 
        {
            new ChatMessage() { Role = "system", Content = prompt },
            new ChatMessage() { Role = "user", Content = text } 
        };
        var response = await openai.CreateChatCompletion(new CreateChatCompletionRequest()
        {
            Model = "gpt-3.5-turbo", 
            Messages = messages
        });

        string responseText = "";
        if(response.Choices != null && response.Choices.Count > 0)
        {
            responseText = response.Choices[0].Message.Content.Trim();
        }
        return responseText;
    }

    public async Task<int> ParseForSpecialTask(string text, string prompt)
    {
        string responseText = await QueryDomainExpert(text, prompt);
        int specialTaskID = 0;
        int.TryParse(responseText, out specialTaskID);
        return specialTaskID;
    }


    public async Task<string> GetContext(string text)
    {
        string responseText = await QueryDomainExpert(text, prompts.KnowledgeBasePrompt);
        return "CONTEXT: " + responseText;
    }

    public async Task ExecuteSpecialTask(int specialTaskID, string userRequest)
    {
        switch(specialTaskID)
        {
            case 1:
                // Follow event
                break;
            case 2:
                // Stop follow event
                break;
            case 3:
                int teleportationID = 
                    await ParseForSpecialTask(userRequest, prompts.TeleportationPrompt);
                SceneManager.LoadScene(sceneNames[teleportationID]);
                break;
        }
    }

    public async Task TranscribeAndReply(string text)
    {
        int specialTaskID = await ParseForSpecialTask(text, prompts.SpecialTaskPrompt);
        Debug.Log("Special task identifier: " + specialTaskID);
        await ExecuteSpecialTask(specialTaskID, text);
        string context = await GetContext(text);
        Debug.Log("Context: " + context);
        string responseText = await GetReply(text, context);
        Debug.Log("Response: " + responseText);
    }
}
