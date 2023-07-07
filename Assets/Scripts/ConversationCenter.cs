using UnityEngine;
using OpenAI;
using System.Collections.Generic;
using System.Threading.Tasks;
using LMNT;

public class ConversationCenter : MonoBehaviour
{
    [SerializeField] private GameObject recordingNotification;
    [SerializeField] private string voice = "Olivia";
    [SerializeField] private string testText = "test test test";
    [SerializeField] private AudioSource audioSource;
    private OpenAIApi openai = new OpenAIApi();
    private SpeechRecognition speechRecognition = new SpeechRecognition();
    private CustomTTS customTTS;
    private List<ChatMessage> messages = new List<ChatMessage>();
    private string prompt = 
        @"You are an assistant inside of a virtual reality game. You have the ability to move 
        around and interact with objects within this game. Your goal is to aid and entertain 
        the user. The user may ask you to complete anyone of the following special tasks:
        1. Move to their location
        2. Stay where you are
        3. Teleport them to a new location
        If the user asks you to complete one of these tasks, you should begin your response 
        with the number of the special task that you are completing, and then continue the 
        rest of your response. For example, if the user says 'come here please', this means 
        they are asking you to move to their location, so your response should look like 
        '1 Okay on my way!' If the user is not asking you to complete one of the special 
        tasks, you should respond normally.";

    private void Start()
    {
        customTTS = new CustomTTS();
        TestPrompt();
    }

    private async void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            await customTTS.Speak(testText, voice, audioSource);
        }
        if(Input.GetKeyDown(KeyCode.Space) && !speechRecognition.IsRecording)
        {
            StartRecording();
        }
        if(Input.GetKeyUp(KeyCode.Space) && speechRecognition.IsRecording)
        {
            StopRecording();
        }
    }

    public void StartRecording()
    {
        if(speechRecognition.IsRecording) { return; }
        recordingNotification.SetActive(true);
        speechRecognition.StartRecording();
    }

    public async void StopRecording()
    {
        if(!speechRecognition.IsRecording) { return; }
        recordingNotification.SetActive(false);
        speechRecognition.EndRecording();
        await TranscribeAndReply();   
    }

    public void InterruptSpeaker()
    {
        Debug.Log("Intterupt not implemented yet");
    }

    private async Task TranscribeAndReply()
    {
        string transcription = await speechRecognition.GetTranscription();
        Debug.Log(transcription);
        string reply = await GetReply(transcription);
        Debug.Log(reply);
        await customTTS.Speak(reply, voice, audioSource);
    }

    private async Task TestPrompt()
    {
        //string response = await GetReply("Hey, please come over here.");
        //string response = await GetReply("Please teleport me to the bowling alley.");
        string response = await GetReply("Please wait where you are.");
        Debug.Log(response);
    }

    private async Task<string> GetReply(string inputText)
    {
        var newMessage = new ChatMessage() { Role = "user", Content = inputText };
        if(messages.Count == 0) { newMessage.Content = prompt + "\n" + inputText; }
        messages.Add(newMessage);
        
        var completionResponse = 
            await openai.CreateChatCompletion(new CreateChatCompletionRequest()
            {
                Model = "gpt-3.5-turbo-0301", 
                Messages = messages
            });

        if(completionResponse.Choices != null && completionResponse.Choices.Count > 0)
        {
            var message = completionResponse.Choices[0].Message;
            message.Content = message.Content.Trim();
            messages.Add(message);
            return message.Content;
        }
        else
        {
            Debug.LogWarning("No text was generated from this prompt.");
            return "";
        }
    }
}
