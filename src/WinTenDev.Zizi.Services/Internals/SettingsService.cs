﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Serilog;
using SqlKata.Execution;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.Zizi.Services.Internals;

public class SettingsService
{
    private const string BaseTable = "group_settings";
    private const string CacheKey = "setting";
    private readonly CacheService _cacheService;
    private readonly QueryService _queryService;

    public SettingsService(
        CacheService cacheService,
        QueryService queryService
    )
    {
        _cacheService = cacheService;
        _queryService = queryService;
    }

    [Obsolete("Next time, this constructor will be removed.")]
    public SettingsService(Message message)
    {
        Message = message;
    }

    [Obsolete("This property will be removed.")]
    public Message Message { get; set; }

    public async Task<bool> IsSettingExist(long chatId)
    {
        var data = await GetSettingsByGroupCore(chatId);
        var isExist = data != null;

        Log.Debug("Group setting for ChatID '{ChatId}' IsExist? {IsExist}", chatId, isExist);
        return isExist;
    }

    public string GetCacheKey(long chatId)
    {
        return CacheKey + "-" + chatId.ReduceChatId();
    }

    public async Task<ChatSetting> GetSettingsByGroupCore(long chatId)
    {
        var where = new Dictionary<string, object>
        {
            { "chat_id", chatId }
        };
        var queryFactory = _queryService.CreateMySqlConnection();
        var data = await queryFactory.FromTable(BaseTable)
            .Where(where)
            .FirstOrDefaultAsync<ChatSetting>();

        return data;
    }

    public async Task<ChatSetting> GetSettingsByGroup(long chatId)
    {
        Log.Information("Get settings chat {ChatId}", chatId);
        var cacheKey = GetCacheKey(chatId);

        var settings = await _cacheService.GetOrSetAsync(cacheKey, async () => {
            var queryFactory = _queryService.CreateMySqlConnection();
            var data = await queryFactory.FromTable(BaseTable)
                .Where("chat_id", chatId)
                .FirstOrDefaultAsync<ChatSetting>();

            return data ?? new ChatSetting();
        });

        return settings;
    }

    public Task<IEnumerable<ChatSetting>> GetAllSettings()
    {
        var queryFactory = _queryService.CreateMySqlConnection();
        var chatGroups = queryFactory.FromTable(BaseTable)
            // .WhereNot("chat_type", "Private")
            // .WhereNot("chat_type", "0")
            .GetAsync<ChatSetting>();

        return chatGroups;
    }

    public async Task UpdateCacheAsync(long chatId)
    {
        Log.Debug("Updating cache for {ChatId}", chatId);
        var cacheKey = GetCacheKey(chatId);

        var data = await GetSettingsByGroupCore(chatId);

        if (data == null)
        {
            Log.Warning("This may first time chat for this ChatId: {ChatId}", chatId);
            return;
        }

        await _cacheService.SetAsync(cacheKey, data);
    }

    public async Task<int> DeleteSettings(long chatId)
    {
        Log.Debug("Starting delete ChatSetting for ChatID: '{ChatId}'", chatId);
        var queryFactory = _queryService.CreateMySqlConnection();
        var deleteSetting = await queryFactory.FromTable(BaseTable)
            .Where("chat_id", chatId)
            .DeleteAsync();

        Log.Debug("Delete ChatSetting by ChatID: '{ChatId}' result => {ChatGroups}", chatId, deleteSetting);
        return deleteSetting;
    }

    public async Task<int> PurgeSettings(int daysOffset)
    {
        var queryFactory = _queryService.CreateMySqlConnection();
        var purgeSettings = await queryFactory.FromTable(BaseTable)
            .WhereRaw($"datediff(now(), updated_at) > {daysOffset}")
            .DeleteAsync();
        Log.Information("About purge settings, total '{Purge}' rows of chats settings is removed", purgeSettings);

        return purgeSettings;
    }

    public async Task<List<CallBackButton>> GetSettingButtonByGroup(long chatId, bool appendChatId = false)
    {
        var selectColumns = new[]
        {
            "id",
            "enable_anti_malfiles",
            "enable_fed_cas_ban",
            "enable_fed_es2_ban",
            "enable_fed_spamwatch",
            // "enable_find_notes",
            "enable_find_tags",
            "enable_human_verification",
            "enable_reply_notification",
            "enable_warn_username",
            "enable_welcome_message",
            // "enable_word_filter_group",
            "enable_word_filter_global",
            "enable_zizi_mata"
        };

        Log.Debug("Append Settings button with Chat ID '{ChatId}'? {AppendChatId}", chatId, appendChatId);

        var queryFactory = _queryService.CreateMySqlConnection();
        var data = await queryFactory.FromTable(BaseTable)
            .Select(selectColumns)
            .Where("chat_id", chatId)
            .GetAsync();

        // Log.Debug($"PreTranspose: {data.ToJson()}");
        // data.ToJson(true).ToFile("settings_premap.json");

        using var dataTable = data.ToJson().MapObject<DataTable>();

        var rowId = dataTable.Rows[0]["id"].ToString();
        Log.Debug("RowId: {RowId}", rowId);

        var transposedTable = dataTable.TransposedTable();
        // Log.Debug($"PostTranspose: {transposedTable.ToJson()}");
        // transposedTable.ToJson(true).ToFile("settings_premap.json");

        // Log.Debug("Setting Buttons:{0}", transposedTable.ToJson());

        var listBtn = new List<CallBackButton>();
        foreach (DataRow row in transposedTable.Rows)
        {
            var textOrig = row["id"].ToString();
            var value = row[rowId].ToString();

            Log.Debug("Orig: {TextOrig}, Value: {Value}", textOrig, value);

            var boolVal = value.ToBool();

            var forCallbackData = textOrig;
            var forCaptionText = textOrig;

            if (!boolVal) forCallbackData = textOrig.Replace("enable", "disable");

            if (boolVal)
                forCaptionText = textOrig.Replace("enable", "✅");
            else
                forCaptionText = textOrig.Replace("enable", "🚫");

            var btnText = forCaptionText
                .Replace("enable_", "")
                .Replace("_", " ");

            // listBtn.Add(new CallBackButton()
            // {
            //     Text = row["id"].ToString(),
            //     Data = row[rowId].ToString()
            // });

            var tail = appendChatId ? $" {chatId}" : "";
            listBtn.Add(new CallBackButton
            {
                Text = btnText.ToTitleCase(),
                Data = $"setting {forCallbackData}" + tail
            });
        }

        //
        // listBtn.Add(new CallBackButton()
        // {
        //     Text = "Enable Word filter Per-Group",
        //     Data = $"setting {mapped.EnableWordFilterGroupWide.ToString()}_word_filter_per_group"
        // });

        // var x =mapped.Cast<CallBackButton>();

        // MatrixHelper.TransposeMatrix<List<ChatSetting>(mapped);
        Log.Debug("ListBtn: {0}", listBtn.ToJson(true));
        // listBtn.ToJson(true).ToFile("settings_listbtn.json");

        return listBtn;
    }

    public async Task<int> SaveSettingsAsync(Dictionary<string, object> data)
    {
        var chatId = data["chat_id"].ToInt64();
        var where = new Dictionary<string, object> { { "chat_id", chatId } };
        var queryFactory = _queryService.CreateMySqlConnection();

        Log.Debug("Checking Chat Settings for {ChatId}", chatId);

        var isExist = await IsSettingExist(chatId);

        int insert;
        // Log.Debug("Group setting IsExist: {0}", isExist);
        if (!isExist)
        {
            Log.Information("Inserting new Chat Settings for {ChatId}", chatId);

            insert = await queryFactory.FromTable(BaseTable).InsertAsync(data);
        }
        else
        {
            Log.Information("Updating Chat Settings for {ChatId}", chatId);

            insert = await queryFactory.FromTable(BaseTable)
                .Where(where)
                .UpdateAsync(data);
        }

        await UpdateCacheAsync(chatId);

        return insert;
    }

    public async Task<int> UpdateCell(long chatId, string key, object value)
    {
        Log.Debug("Updating Chat Settings '{ChatId}'. Field '{Key}' with value '{Value}'", chatId, key, value);
        var where = new Dictionary<string, object> { { "chat_id", chatId } };
        var data = new Dictionary<string, object> { { key, value } };

        var queryFactory = _queryService.CreateMySqlConnection();
        var save = await queryFactory.FromTable(BaseTable)
            .Where(where)
            .UpdateAsync(data);

        await UpdateCacheAsync(chatId);

        return save;
    }
}