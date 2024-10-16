using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Data.Models;

namespace Server.Services;

public class MessageService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public MessageService(ApplicationDbContext context, IHubContext<ChatHub> hubContext,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _hubContext = hubContext;
        _userManager = userManager;
    }

    public async Task<bool> SendMessageAsync(string userId, MessageRequest req)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        var message = new Message
        {
            Content = req.Content,
            SenderId = user.Id,
            Timestamp = DateTime.Now
        };

        try
        {
            // Add the message to the database
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Prepare the message to broadcast
            var messageToBroadcast = new
            {
                Content = message.Content,
                SenderId = message.SenderId,
                Timestamp = message.Timestamp,
                SenderUsername = user.UserName
            };

            var jsonMessage = JsonSerializer.Serialize(messageToBroadcast);

            // Broadcast the message via SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", jsonMessage);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }

    public async Task<List<MessageResponse>> GetLatestMessage()
    {
        var messages = await _context.Messages
            .OrderByDescending(m => m.Timestamp)
            .Take(20)
            .Join(
                _context.Users,
                message => message.SenderId,
                user => user.Id,
                (message, user) => new MessageResponse
                {
                    Id = message.Id,
                    Content = message.Content,
                    Timestamp = message.Timestamp,
                    SenderId = message.SenderId,
                    SenderUsername = user.UserName
                })
            .ToListAsync();
        return messages;
    }
}