using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class ChatMessageRepository : IChatMessageRepository
    {
        private readonly ApplicationDbContext _db;
        public ChatMessageRepository(ApplicationDbContext db) {
            _db = db;
        }

        public async Task<IEnumerable<ChatMessage>> GetRecentAsync(Guid conversationId, string userId, int count) {
           
            var turns = await _db.ChatMessages
                .Where(cm => cm.ConversationId == conversationId && cm.UserId == userId)
                .OrderByDescending(cm => cm.CreatedAtUtc)
                .Take(count)
                .ToListAsync();

            turns.Reverse(); // Reverse so turns come back oldest-first for ChatHistory replay
            return turns;
        }

        public async Task SaveTurnsAsync(Guid conversationId, 
                                    string userId, 
                                    string userMessage, 
                                    string assistantMessage, 
                                    int tokensUsed, bool fallbackUsed) {

            var now = DateTime.UtcNow;
            _db.ChatMessages.Add(new ChatMessage {
                ConversationId = conversationId,
                Role = "user",
                UserId = userId,
                Content = userMessage,
                TokensUsed = 0, // Only count tokens for assistant response
                FallbackUsed = false,
                CreatedAtUtc = now
            });

            _db.ChatMessages.Add(new ChatMessage {
                ConversationId = conversationId,
                Role = "assistant",
                UserId = userId,
                Content = assistantMessage,
                TokensUsed = tokensUsed,
                FallbackUsed = fallbackUsed,
                CreatedAtUtc = now.AddTicks(1) // ensures assistant sorts after user
            });

            await _db.SaveChangesAsync();
        }
    }
}
