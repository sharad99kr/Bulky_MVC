namespace ProjectCore.Models.AI
{
    public class SearchResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        public float TopScore { get; init; }

        // True when top cosine similarity score is below 0.75.
        // Surfaced in UI so users understand why results may be unexpected.
        // Logged to App Insights and aggregated in upcoming dashboard.
        public bool LowConfidence { get; init; }
    }
}
