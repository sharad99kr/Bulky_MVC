namespace ProjectCore.Models.AI
{
    public class AIResponse<T>
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public T? Data { get; init; }
        public bool FromCache { get; init; }
        public int TokensUsed { get; init; }
        public static AIResponse<T> Ok(T data, int tokens=0, bool fromCache = false) => new () 
        {
            Success = true,
            Data = data,
            TokensUsed = tokens,
            FromCache = fromCache
        };

        public static AIResponse<T> Fail(string errorMessage) => new () 
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
