﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Services.Telegram;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Tags;

public class TagCommand : CommandBase
{
    private readonly TagsService _tagsService;
    private readonly TelegramService _telegramService;

    public TagCommand(
        TelegramService telegramService,
        TagsService tagsService
    )
    {
        _telegramService = telegramService;
        _tagsService = tagsService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        await _telegramService.AddUpdateContext(context);

        var msg = _telegramService.Message;
        var chatId = _telegramService.ChatId;
        var fromId = _telegramService.FromId;
        var isSudoer = _telegramService.IsFromSudo;
        var isAdmin = await _telegramService.IsAdminOrPrivateChat();
        var sendText = "Hanya admin yang bisa membuat Tag";

        var cmd = _telegramService.GetCommand();
        var isForceTag = cmd.Contains("/retag");

        if (!isSudoer && !isAdmin)
        {
            // await _telegramService.DeleteAsync(msg.MessageId);
            await _telegramService.SendTextMessageAsync(sendText);
            Log.Information("This User is not Admin or Sudo!");
            return;
        }

        sendText = "ℹ Simpan tag ke Cloud Tags" +
                   "\nContoh: <code>/tag tagnya [tombol|link.tombol]</code> - Reply pesan" +
                   "\nPanjang tag minimal 3 karakter." +
                   "\nTanda [ ] artinya tidak harus" +
                   "\n" +
                   "\n📝 <i>Jika untuk grup, di rekomendasikan membuat sebuah channel, " +
                   "lalu link ke post di tautkan sebagai tombol.</i>";

        if (msg.ReplyToMessage == null)
        {
            await _telegramService.SendTextMessageAsync(sendText);
            return;
        }

        Log.Information("Replied message detected..");

        var msgText = msg.Text.GetTextWithoutCmd();

        var repMsg = msg.ReplyToMessage;
        var repFileId = repMsg.GetFileId();
        var repMsgText = repMsg.Text;
        var partsMsgText = msgText.SplitText(" ").ToArray();

        Log.Information("Part1: {V}", partsMsgText.ToJson(true));

        var slugTag = partsMsgText.ValueOfIndex(0);
        var tagAndCmd = partsMsgText.Take(2);
        var buttonData = msgText.TrimStart(slugTag.ToCharArray()).Trim();

        if (slugTag.Length < 3)
        {
            await _telegramService.EditMessageTextAsync("Slug Tag minimal 3 karakter");

            return;
        }

        await _telegramService.SendTextMessageAsync("📖 Sedang mempersiapkan..");

        if (isForceTag)
        {
            await _telegramService.EditMessageTextAsync("Sedang menghapus tag sebelumnya..");
            await _tagsService.DeleteTag(chatId, slugTag.Trim());
        }

        var content = repMsg.Text ?? repMsg.Caption ?? "";
        Log.Information("Content: {0}", content);

        var isExist = await _tagsService.IsExist(msg.Chat.Id, slugTag);
        Log.Information("Tag isExist: {IsExist}", isExist);
        if (isExist)
        {
            await _telegramService.EditMessageTextAsync("✅ Tag sudah ada. " +
                                                        "Silakan ganti Tag jika ingin isi konten berbeda");
            return;
        }

        var data = new Dictionary<string, object>()
        {
            { "chat_id", chatId },
            { "from_id", fromId },
            { "tag", slugTag.Trim() },
            { "btn_data", buttonData },
            { "content", content },
            { "file_id", "" }
        };

        if (repFileId.IsNotNullOrEmpty())
        {
            data.Remove("file_id");

            data.Add("file_id", repFileId);
            data.Add("type_data", repMsg.Type);
        }

        await _telegramService.EditMessageTextAsync("📝 Menyimpan tag data..");
        await _tagsService.SaveTagAsync(data);

        await _telegramService.EditMessageTextAsync("✅ Tag berhasil di simpan.." +
                                                    $"\nTag: <code>#{slugTag}</code>" +
                                                    $"\n\nKetik /tags untuk melihat semua Tag.");

        await _tagsService.UpdateCacheAsync(chatId);
    }
}