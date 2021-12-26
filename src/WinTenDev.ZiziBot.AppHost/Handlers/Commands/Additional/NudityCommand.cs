using System.Diagnostics;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.Enums;
using WinTenDev.Zizi.Services.Externals;
using WinTenDev.Zizi.Services.Telegram;

namespace WinTenDev.ZiziBot.AppHost.Handlers.Commands.Additional;

public class NudityCommand : CommandBase
{
    private readonly TelegramService _telegramService;
    private readonly DeepAiService _deepAiService;

    public NudityCommand(
        DeepAiService deepAiService,
        TelegramService telegramService
    )
    {
        _deepAiService = deepAiService;
        _telegramService = telegramService;
    }

    public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args)
    {
        var sw = Stopwatch.StartNew();

        await _telegramService.AddUpdateContext(context);

        var msg = _telegramService.Message;

        var repMsg = msg.ReplyToMessage;

        if (repMsg == null || repMsg.Type != MessageType.Photo)
        {
            await _telegramService.SendTextMessageAsync("Silakan balas pesan Gambar yang mau dicek");
            Log.Debug("Processing {0} completed in {1}", nameof(NudityCommand), sw.Elapsed);

            sw.Stop();
            return;
        }

        await _telegramService.SendTextMessageAsync("Sedang memeriksa Nudity..");

        var fileName = await _telegramService.DownloadFileAsync("nsfw-check");

        var result = await _deepAiService.NsfwDetectCoreAsync(fileName);
        var output = result.Output;

        var text = $"NSFW Score: {output.NsfwScore}" +
                   $"\n\nPowered by https://deepai.org";

        await _telegramService.EditMessageTextAsync(text, disableWebPreview: true);
    }
}