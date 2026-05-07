namespace ProjectCore.Models.AI
{
    public class AIProductDescriptionResult 
    {
        public required string Description { get; init; }
        public required string Tone { get; init; }
        public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    }
}
