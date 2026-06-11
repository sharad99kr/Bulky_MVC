namespace Bulky.Models
{
    //public record ChatMessage
    //(
    //    string Message,
    //    Guid ConversationId,
    //    bool FromCache,
    //    bool FallbackUsed,
    //    int TOkensUsed,
    //    string? WarningMessage=null
    //);

    public class ChatMessage {

        public int Id { get; set; }
        public Guid ConversationId { get; set; }
        public string UserId { get; set; } = null!;
        public string Role {  get; set; } = null!; //"user" | "assistant"
        public string Content { get; set; } = null!;
        public int TokensUsed { get; set; }
        public bool FallbackUsed { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
