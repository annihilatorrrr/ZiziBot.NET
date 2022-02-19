﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.Zizi.Utils.Telegram;

public static class KeyboardUtil
{
    public static Dictionary<string, string> StringToDict(string buttonStr)
    {
        var dict = new Dictionary<string, string>();
        var splitWelcomeButton = buttonStr.Split(',').ToList();

        foreach (var button in splitWelcomeButton)
        {
            Log.Information("Button: {Button}", button);

            if (button.Contains("|"))
            {
                var buttonLink = button.Split('|').ToList();
                Log.Information("Appending keyboard: {V} -> {V1}", buttonLink[0], buttonLink[1]);
                dict.Add(buttonLink[0], buttonLink[1]);
            }
        }

        return dict;
    }

    public static InlineKeyboardMarkup CreateInlineKeyboardButton(
        Dictionary<string, string> buttonList,
        int columns
    )
    {
        var rows = (int) Math.Ceiling(buttonList.Count / (double) columns);
        var buttons = new InlineKeyboardButton[rows][];

        for (var i = 0; i < buttons.Length; i++)
        {
            buttons[i] = buttonList
                .Skip(i * columns)
                .Take(columns)
                .Select
                (
                    direction => {
                        if (direction.Value.CheckUrlValid())
                            return InlineKeyboardButton.WithUrl(direction.Key, direction.Value);
                        else
                            return InlineKeyboardButton.WithCallbackData(direction.Key, direction.Value);
                    }
                )
                .ToArray();
        }

        return new InlineKeyboardMarkup(buttons);
    }

    public static InlineKeyboardMarkup ToReplyMarkup(
        this string buttonStr,
        int columns = 2
    )
    {
        return CreateInlineKeyboardButton(StringToDict(buttonStr), columns);
    }

    public static async Task<InlineKeyboardMarkup> JsonToButton(
        this string jsonPath,
        int chunk = 2
    )
    {
        string json;

        if (File.Exists(jsonPath))
        {
            Log.Information("Loading Json from path: {JsonPath}", jsonPath);
            json = await File.ReadAllTextAsync(jsonPath);
        }
        else
        {
            Log.Information("Loading Json from string..");
            json = jsonPath;
        }

        var replyMarkup = json.MapObject<DataTable>();

        var btnList = new List<InlineKeyboardButton>();

        foreach (DataRow row in replyMarkup.Rows)
        {
            var btnText = row["text"].ToString();
            var data = row["data"].ToString();

            if (data.CheckUrlValid())
            {
                Log.Verbose("Appending button URL. Text: '{BtnText}', Url: '{Data}'", btnText, data);
                btnList.Add(InlineKeyboardButton.WithUrl(btnText, data));
            }
            else
            {
                Log.Verbose("Appending button callback. Text: '{BtnText}', Data: '{Data}'", btnText, data);
                btnList.Add(InlineKeyboardButton.WithCallbackData(btnText, data));
            }
        }

        return new InlineKeyboardMarkup(btnList.ChunkBy(chunk));
    }

    public static InlineKeyboardMarkup ToButtonMarkup(this List<List<ButtonMarkup>> rawButtonMarkups)
    {
        var buttonMarkup = InlineKeyboardMarkup.Empty();

        if (rawButtonMarkups != null)
        {
            buttonMarkup = new InlineKeyboardMarkup
            (
                rawButtonMarkups
                    .Select
                    (
                        x => x
                            .Select(y => InlineKeyboardButton.WithUrl(y.Text, y.Url))
                    )
            );
        }

        return buttonMarkup;
    }
}