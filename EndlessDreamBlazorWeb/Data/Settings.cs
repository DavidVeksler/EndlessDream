namespace EndlessDreamBlazorWeb.Data
{
    public class Settings
    {
        internal static IConfigurationRoot? configuration;

        private Settings()
        {

        }

        private static void InitConfiguration()
        {
            configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .Build();
        }

        public static string OpenAIKey
        {
            get
            {
                if (configuration == null)
                {
                    InitConfiguration();
                }

                string apiKey = configuration["OpenAI:ApiKey"];
                return apiKey;
            }
        }

    }
}
