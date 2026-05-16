namespace ProjectCore.Services.AI
{
    public interface IRagEvaluationService
    {
        //score how faithful the retrieved context is to the original query, returning a score from 1(not grounded) to 5(fully grounded)
        //Fires asynchronously and does not block the search response, allowing for efficient evaluation without impacting user experience
        Task<int> ScoreFaithfulnessAsync(string query, IEnumerable<string> retrievedContext, CancellationToken ct = default);
    }
}
