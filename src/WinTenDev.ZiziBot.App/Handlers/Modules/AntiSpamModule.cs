﻿using System;
using System.Threading.Tasks;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Services.Internals;

namespace WinTenDev.ZiziBot.App.Handlers.Modules
{
    /// <summary>
    ///
    /// </summary>
    public class AntiSpamModule
    {
        private readonly AntiSpamService _antiSpamService;

        /// <summary>
        ///
        /// </summary>
        /// <param name="antiSpamService"></param>
        public AntiSpamModule(
            AntiSpamService antiSpamService
        )
        {
            _antiSpamService = antiSpamService;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="fromId"></param>
        /// <param name="funcAntiSpamResult"></param>
        /// <returns></returns>
        public async Task<AntiSpamResult> CheckAntiSpam(long fromId, Func<AntiSpamResult, Task> funcAntiSpamResult)
        {
            var result = await _antiSpamService.CheckSpam(fromId);

            await funcAntiSpamResult(result);

            return result;
        }
    }
}