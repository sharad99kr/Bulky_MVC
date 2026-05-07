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
        public required string ProductName { get; set; }
        public required string Category { get; set; }
        public  string? Author { get; set; }
        public DescriptionTone Tone { get; set; } = DescriptionTone.Professional;
        public int MaxSentences { get; set; } = 3;
    }
}
