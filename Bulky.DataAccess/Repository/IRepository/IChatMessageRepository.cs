using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface IChatMessageRepository
    {
        Task<IEnumerable<ChatMessage>> GetRecentAsync(
            Guid conversationId,
            string userId,
            int count);

        Task SaveTurnsAsync(
            Guid conversationId,
            string userId,
            string userMessage,
            string assistantMessage,
            int tokensUsed,
            bool fallbackUsed
            );
    }
}
