﻿using System.Net.Http.Headers;
using IncentiveCheckerForDemaekan.Base;

namespace IncentiveCheckerForDemaekan
{
    /// <summary>
    /// Line操作クラス
    /// </summary>
    public class Line : Http
    {
        /// <summary>
        /// アクセストークン
        /// </summary>
        public string AccessToken { get; }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="accessToken">Lineアクセストークン</param>
        public Line(string accessToken) : base()
        {
            AccessToken = accessToken;         
        }
        /// <summary>
        /// Line通知メッセージを送信する
        /// </summary>
        /// <param name="message">送信内容</param>
        /// <returns>Line通知メッセージを送信するタスク</returns>
        public async Task SendMessage(string message)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string> { { "message", message } });
            await PostRequestAsync(AppConfig.GetAppSettingsValue("lineUrl"), content, new AuthenticationHeaderValue("Bearer", AccessToken));
        }
    }
}
