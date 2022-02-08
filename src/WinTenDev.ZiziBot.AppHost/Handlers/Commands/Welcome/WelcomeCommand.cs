﻿using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Enums;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Welcome;

public class WelcomeCommand : CommandBase
{
    private readonly SettingsService _settingsService;
    private readonly TelegramService _telegramService;

    public WelcomeCommand(
        TelegramService telegramService,
        SettingsService settingsService
    )
    {
        _settingsService = settingsService;
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(
        IUpdateContext context,
        UpdateDelegate next,
        string[] args
    )
    {
        await _telegramService.AddUpdateContext(context);

        var chatId = _telegramService.ChatId;
        var chatTitle = _telegramService.ChatTitle;

        Log.Information("Args: {V}", string.Join(" ", args));

        if (!await _telegramService.CheckFromAdminOrAnonymous()) return;

        var settings = await _settingsService.GetSettingsByGroup(chatId);
        var welcomeMessage = settings.WelcomeMessage;
        var welcomeButton = settings.WelcomeButton;
        var welcomeMedia = settings.WelcomeMedia;
        var welcomeMediaType = settings.WelcomeMediaType;

        var sendText = $"⚙ Konfigurasi Welcome di <b>{chatTitle}</b>\n\n";

        if (welcomeMessage.IsNullOrEmpty())
        {
            var defaultWelcome = "Hai {allNewMember}" +
                                 "\nSelamat datang di kontrakan {chatTitle}" +
                                 "\nKamu adalah anggota ke-{memberCount}";

            sendText += "Tidak ada konfigurasi pesan welcome, pesan default akan di terapkan" +
                        $"\n\n<code>{defaultWelcome}</code>" +
                        $"\n\nUntuk bantuan silakan ketik /help" +
                        $"\nBantuan pesan Welcome ke Bantuan > Grup > Welcome";
        }
        else
        {
            sendText += $"<code>{welcomeMessage}</code>";
        }

        var keyboard = InlineKeyboardMarkup.Empty();

        if (!welcomeButton.IsNullOrEmpty())
        {
            keyboard = welcomeButton.ToReplyMarkup(2);

            sendText += "\n\n<b>Raw Button:</b>" +
                        $"\n<code>{welcomeButton}</code>";
        }

        if (welcomeMediaType != MediaType.Unknown)
        {
            await _telegramService.SendMediaAsync(welcomeMedia, welcomeMediaType, sendText, keyboard);
        }
        else
        {
            await _telegramService.SendTextMessageAsync(sendText, keyboard);
        }
    }
}