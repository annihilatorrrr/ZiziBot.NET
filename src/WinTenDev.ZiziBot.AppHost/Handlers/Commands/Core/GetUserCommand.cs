﻿using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Core;

internal class GetUserCommand : CommandBase
{
    private readonly TelegramService _telegramService;

    public GetUserCommand(TelegramService telegramService)
    {
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        await _telegramService.GetUserInfoAsync();
    }
}
