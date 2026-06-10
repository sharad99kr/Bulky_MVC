using System.Text;

namespace ProjectCore.Services.AI
{
    public static class ChatPromptBuilder
    {
        public static string BuildSystemPrompt(string ragContext) {
            var sb = new StringBuilder();

            sb.AppendLine("You are a helpful bookstore assistant for Readify.");
            sb.AppendLine("You help customers find books and check their order status.");
            sb.AppendLine();

            // GUARDRAIL 1 — Grounding: answer from retrieved data only
            if(!string.IsNullOrEmpty(ragContext)) {
                sb.AppendLine(
                    "Answer ONLY using the product data below and the tools available to you. " +
                    "Do not make up books, authors, prices, or order details that are not in " +
                    "the provided data or returned by a tool call.");
                sb.AppendLine();
                sb.AppendLine(ragContext);
            } else {
                sb.AppendLine(
                    "No product context is available for this query. " +
                    "Use your available tools to look up product or order data. " +
                    "If you cannot find relevant information, say so honestly.");
            }

            sb.AppendLine();

            // GUARDRAIL 2 — Scope: restrict to bookstore domain only
            sb.AppendLine(
                "Do not answer questions unrelated to books, authors, reading " +
                "recommendations, or the customer's own orders. If asked about " +
                "something outside this scope, politely redirect the customer.");

            // GUARDRAIL 3 — PII: hard rule — order data is per-user only
            sb.AppendLine(
                "Never reveal personal information about other customers. " +
                "Order lookups are only valid for the current authenticated user.");

            // GUARDRAIL 4 — Honesty: reduce hallucination via explicit permission to say IDK
            sb.AppendLine(
                "If you are not sure about something, say you are not sure " +
                "and offer to help find the information another way.");

            return sb.ToString();
        }
    }
}
