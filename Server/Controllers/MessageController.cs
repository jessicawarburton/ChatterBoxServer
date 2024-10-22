using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Server.Data.Models;
using Server.Services;

namespace Server.Controllers;
[ApiController]
[Route("[controller]/[action]")]
public class MessageController : ControllerBase
{

    private readonly MessageService _messageService;
    
    public MessageController(MessageService messageService)
    {
        _messageService = messageService; 
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetLatestMessages()
    {
        var latestMessages = await _messageService.GetLatestMessage();
        return Ok(latestMessages);
    }


    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Message(MessageRequest req)
    {
        var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var result = await _messageService.SendMessageAsync(userId!, req);
        if (result)
        {
            return Ok("Message submitted successfully");
        }
        return BadRequest("Unable to create message");
    }

    
  
}