namespace ProjectCore.Models.AI
{
    public class AzureSearchSettings
    {
        public const string SectionName = "AzureSearch";
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }
}
