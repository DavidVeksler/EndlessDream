using EndlessDreamBlazorWeb.Data;

namespace EndlessDreamBlazorWeb.Services
{
    public class OpenAIGPTConversationService
    {
        public async Task<string> GetSingleResponseAsync(string systemMessage)
        {
            OpenAI_API.OpenAIAPI api = new(Settings.OpenAIKey);

            // https://github.com/OkGoDoIt/OpenAI-API-dotnet

            OpenAI_API.Chat.Conversation chat = api.Chat.CreateConversation();

            chat.AppendSystemMessage(systemMessage);
            //// give a few examples as user and assistant
            //chat.AppendUserInput("Is this an animal? Cat");
            //chat.AppendExampleChatbotOutput("Yes");
            //chat.AppendUserInput("Is this an animal? House");
            //chat.AppendExampleChatbotOutput("No");
            //// now let's ask it a question'
            //chat.AppendUserInput("Is this an animal? Dog");
            // and get the response
            string response = await chat.GetResponseFromChatbotAsync();

            return response;
        }

        public async Task<string> GetStableDiffusionRandomPrompt()
        {
            OpenAI_API.OpenAIAPI api = new(Settings.OpenAIKey);

            // https://github.com/OkGoDoIt/OpenAI-API-dotnet

            OpenAI_API.Chat.Conversation chat = api.Chat.CreateConversation();

            string systemMessage = "I want you to provide a random prompt for an AI image generator.  \r\n\r\nHere are 3 examples of good prompts:\r\n\r\n\"A dreamy, vibrant portrait of Ana de Armas; aesthetically pleasing anime style, trending on popular art platforms, minutely detailed, with precise, sharp lines, a composition that qualifies as an award-winning illustration, presented in 4K resolution, inspired by master artists like Eugene de Blaas and Ross Tran, employing a vibrant color palette, intricately detailed.\"\r\n\r\n\"A portrait exuding Alberto Seveso and Geo2099's distinctive styles; an ultra-detailed and hyper-realistic portrayal of Ana de Armas, designed with Lisa Frank aesthetics, featuring popular art elements such as butterflies and florals, sharp focus, akin to a high-quality studio photograph, with meticulous detailing, made famous by artists such as Tvera, Wlop, and Artgerm.\"\r\n\r\n\"Section model of a 2 story Richard Meier building, architectural, cantilever, dynamic composition, asymmetrical composition, photorealism, Lens with Nikon d850, 28mm, Film Light, Overall Light, Ethereal Light, Depth of Field, f/2.8, Ultra HD, 128k, 3D Shadows, Tone Mapping, Ray Traced Global Illumination, Super Resolution, Gigapixel, Color Correction, Retouching, Enhancement\"\r\n\r\nNow reply with a random prompt.  Do not reply with any other text.\r\n\r\n";

            chat.AppendSystemMessage(systemMessage);
            //// give a few examples as user and assistant
            //chat.AppendUserInput("Is this an animal? Cat");
            //chat.AppendExampleChatbotOutput("Yes");
            //chat.AppendUserInput("Is this an animal? House");
            //chat.AppendExampleChatbotOutput("No");
            //// now let's ask it a question'
            //chat.AppendUserInput("Is this an animal? Dog");
            // and get the response
            string response = await chat.GetResponseFromChatbotAsync();

            return response;
        }
    }
}