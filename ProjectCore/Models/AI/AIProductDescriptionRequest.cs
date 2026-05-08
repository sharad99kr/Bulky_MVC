namespace ProjectCore.Models.AI
{

    public enum DescriptionTone {
        Professional,
        Casual,
        Playful,
        Academic
    }

    //Input to the Product Description generator AI Service
    public class AIProductDescriptionRequest
    {
        public required string ProductName { get; init; }
        public required string Category { get; init; }
        public  string? Author { get; init; }
        public DescriptionTone Tone { get; init; } = DescriptionTone.Professional;
        public int MaxSentences { get; init; } = 3;
    }
}
