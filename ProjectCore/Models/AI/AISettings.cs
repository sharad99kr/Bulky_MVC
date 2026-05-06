namespace ProjectCore.Models.AI
{
    public class AISettings
    {
        public const string SectionName = "AISettings";

        public int MaxTokens { get; set; }
        public int CacheDurationMinutes { get; set; } 
        public double Temperature { get; set; } 
        public bool EnableCaching { get; set; } 
        public int MaxRetries { get; set; } 
    }

    public class AzureOpenAISettings
    {
        public const string SectionName = "AzureOpenAI";

        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string DeploymentName { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = string.Empty;

    }
}
