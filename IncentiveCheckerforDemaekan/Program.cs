﻿using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Data;

namespace IncentiveCheckerforDemaekan
{
    class Program
    {
        /// <summary>
        /// 出前館 市区町村別ブースト情報サイトから
        /// csvファイル記載地域の明日のインセンティブ情報を取得して
        /// Line通知を行なう
        /// </summary>
        /// <param name="args">Lineアクセストークン</param>
        static async Task<int> Main(string[] args)
        {
            string message;
            int resCode;
            try
            {
                string locationPath = GetCurrentPath();
                ExitsFile(locationPath);
                CheckBrowser(locationPath);
                message = await MakeSendMessageAsync(locationPath);
                resCode = 0;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                resCode = 1;
            }
            if(args.Length > 0 )
            {
                try 
                {
                    await new Line(args[0]).SendMessage(message);           
                }
                catch { resCode = 1; }
            }
            return resCode;
        }

        /// <summary>
        /// 必要なファイルがあるか確認してなかったらファイルを作る
        /// </summary>
        /// <param name="locationPath">カレントディレクトリ</param>
        private static void ExitsFile(string locationPath)
        {
            var fileOprate = new FileOparate(locationPath);
            if (!File.Exists(Path.Combine(locationPath, "ChromeInstall.bat")))
            {
                fileOprate.WriteFile("ChromeInstall.bat", FileContents.ChromeInstall());
            }
            if (!File.Exists(Path.Combine(locationPath, "TargetPlace.csv")))
            {
                fileOprate.WriteFile("TargetPlace.csv", FileContents.TargetPlace());
            }
        }

        /// <summary>
        /// Chromeがインストールされているか確認しなかったらインストールする
        /// </summary>
        /// <param name="locationPath"></param>
        private static void CheckBrowser(string locationPath)
        {
            var browsers = Browser.GetInstallBrowser();
            if (!browsers.Contains("Google Chrome"))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Browser.InstallChromeWindows(Path.Combine(locationPath, "ChromeInstall.bat"));
                }
            }
        }

        /// <summary>
        /// 実行ファイルのカレントディレクトリを取得する
        /// </summary>
        /// <returns>カレントディレクトリ</returns>
        private static string GetCurrentPath()
        {
            var locationPath = @Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (locationPath == null) 
            { 
                return ""; 
            }else
            {
                return locationPath;
            }
        }

        /// <summary>
        /// 出前館 市区町村別ブースト情報サイトから
        /// csvファイル記載地域の明日のインセンティブ情報を取得して
        /// Line通知メッセージを作成する
        /// </summary>
        /// <returns>Line通知メッセージ</returns>
        private static async Task<string> MakeSendMessageAsync(string locationPath)
        {
            var fileOparate = new FileOparate(locationPath);
            DataTable targetPlace = fileOparate.ReadTargetPlace("TargetPlace.csv");
            Dictionary<string, Dictionary<string, string>> map = await MakeIncentiveMapAsync(targetPlace);
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(DateTime.Now.ToString("MM/dd") + "のインセンティブ情報");
            stringBuilder.AppendLine();
            foreach (var (address, incentive) in map)
            {
                stringBuilder.AppendLine(address);
                foreach (var (time, magnification) in incentive)
                {
                    //1.1倍以上の時間帯だけ抽出する
                    if (double.TryParse(magnification, out double val) && val > 1.0)
                    {
                        stringBuilder.AppendLine($"{time}:{val}");
                    }
                }
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        ///  csvファイル記載地域の明日のインセンティブ情報を取得してMap化する
        /// </summary>
        /// <param name="targetPlace"> csvファイル記載地域</param>
        /// <returns>csvファイル記載地域すべてのインセンティブ情報</returns>
        private static async Task<Dictionary<string, Dictionary<string, string>>> MakeIncentiveMapAsync(DataTable targetPlace)
        {
            
            var map = new Dictionary<string, Dictionary<string, string>>();
            using var reader = targetPlace.CreateDataReader();
            var tasks = new List<Task>();
            while (reader.Read())
            {
                var area = (string)reader["エリア"];
                var prefecture = (string)reader["都道府県"];
                var city = (string)reader["市区町村"];
                tasks.Add(Task.Run(() => AddMapOfIncentive(map, area, prefecture, city)));
            }
            await Task.WhenAll(tasks);  
            return map;
        }

        /// <summary>
        /// 各エリアのインセンティブ情報を取得してMapに追加する
        /// </summary>
        /// <param name="map">格納Map</param>
        /// <param name="area">エリア</param>
        /// <param name="prefecture">都道府県</param>
        /// <param name="city">市区町村</param>
        private static void AddMapOfIncentive(Dictionary<string, Dictionary<string, string>> map, string area, string prefecture, string city)
        {
            var options = new List<string>()
            {
                "--headless",
                "--incognito",
                "--start-maximized",
                "--blink-settings=imagesEnabled=false",
                "--lang=ja",
                "--proxy-server='direct://'",
                "--proxy-bypass-list=*"
            };
            using var webDriver = new WebDriverOpration(options.ToArray(), 10);
            map.Add(prefecture + city, webDriver.GetInsentiveInfo(area, prefecture, city));
        }
    }
}

