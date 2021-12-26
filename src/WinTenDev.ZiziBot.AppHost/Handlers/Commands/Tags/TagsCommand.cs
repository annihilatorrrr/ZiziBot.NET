﻿using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Tags;

public class TagsCommand : CommandBase
{
    private readonly TagsService _tagsService;
    private readonly SettingsService _settingsService;
    private readonly TelegramService _telegramService;

    public TagsCommand(
        TelegramService telegramService,
        TagsService tagsService,
        SettingsService settingsService
    )
    {
        _telegramService = telegramService;
        _tagsService = tagsService;
        _settingsService = settingsService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);
        var chatId = _telegramService.ChatId;
        var msg = context.Update.Message;

        var sendText = "Under maintenance";

        await _telegramService.DeleteAsync(msg.MessageId);
        await _telegramService.SendTextMessageAsync("🔄 Loading tags..");
        var tagsData = (await _tagsService.GetTagsByGroupAsync(chatId)).ToList();
        var tagsStr = new StringBuilder();

        foreach (var tag in tagsData)
        {
            tagsStr.Append($"#{tag.Tag} ");
        }

        sendText = $"#️⃣<b> {tagsData.Count} Tags</b>\n" +
                   $"\n{tagsStr.ToTrimmedString()}";

        await _telegramService.EditMessageTextAsync(sendText);

        var currentSetting = await _settingsService.GetSettingsByGroup(chatId);
        var lastTagsMsgId = currentSetting.LastTagsMessageId;
        Log.Information("LastTagsMsgId: {LastTagsMsgId}", lastTagsMsgId);

        if (lastTagsMsgId.ToInt() > 0)
            await _telegramService.DeleteAsync(lastTagsMsgId.ToInt());

        await _tagsService.UpdateCacheAsync(chatId);
        await _settingsService.UpdateCell(chatId, "last_tags_message_id", _telegramService.SentMessageId);
    }
}