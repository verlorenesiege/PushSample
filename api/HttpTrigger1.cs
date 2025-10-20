using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Text.Json;
using WebPush;

namespace Company.Function;

public class HttpTrigger1
{
    private readonly ILogger<HttpTrigger1> _logger;
    private static readonly string VAPID_SUBJECT = "mailto:verlorenesiege@gmail.com";
    private static readonly string VAPID_PUBLIC_KEY = "BCrMZpWrJhviBTe76eDmqd9kOGxnHZeIS-iPNGBvd6KjhcLlN6jIprlXLJ519j3B3QybhoNxx3d_AzC-zKiigec";
    private static readonly string VAPID_PRIVATE_KYE = "Ve_SCxfZxHNI5ElUXP4suC30mBqM9PizvAWxdWGMcSI";
    public HttpTrigger1(ILogger<HttpTrigger1> logger)
    {
        _logger = logger;
    }

    [Function("save-subscription")]
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        _logger.LogInformation("requestBody : " + requestBody);

        try
        {
            Subscribe sub = JsonSerializer.Deserialize<Subscribe>(requestBody);

            _logger.LogInformation("Endpoint : " + sub.endpoint);
            _logger.LogInformation("Keys.p256dh : " + sub.keys.p256dh);
            _logger.LogInformation("Keys.auth : " + sub.keys.auth);

            var subscription = new PushSubscription(sub.endpoint, sub.keys.p256dh, sub.keys.auth);

            var options = new Dictionary<string, object>();
            options["vapidDetails"] = new VapidDetails(VAPID_SUBJECT, VAPID_PUBLIC_KEY, VAPID_PRIVATE_KYE);
            //options["gcmAPIKey"] = @"[your key here]";


            var payLoadObj = new PayLoad { title = "Push疎通", body = "疎通成功" };
            string payloadStr = JsonSerializer.Serialize(payLoadObj);
            _logger.LogInformation(payloadStr);

            var webPushClient = new WebPushClient();
            await webPushClient.SendNotificationAsync(subscription, payloadStr, options);



            // --- Gmail設定 ---
            const string SmtpHost = "smtp.gmail.com";
            const int SmtpPort = 587; // STARTTLS用のポート
            const string YourEmail = "verlorenesiege@gmail.com"; // あなたのGmailアドレス


            string YourAppPassword = Environment.GetEnvironmentVariable("APPL_KEY", EnvironmentVariableTarget.Process);
            //const string YourAppPassword = ""; // 生成した16桁のアプリパスワード

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("送信者名", YourEmail));
            message.To.Add(new MailboxAddress("宛先名", "nobuhiro-miyamoto@exa-corp.co.jp")); // 宛先
            message.Subject = "Push通知送信履歴";


            var end_point = "end_point : " + sub.endpoint;
            var p256dh    = "p256dh    : " + sub.keys.p256dh;
            var auth      = "auth      : " + sub.keys.auth;

            message.Body = new TextPart("plain")
            {
                Text = "送信先端末情報\n" + 
                       end_point + "\n" +
                       p256dh + "\n" +
                       auth + "\n"
            };
            using (var client = new SmtpClient())
            {
                // SMTPサーバーに接続 (STARTTLSを使用)
                await client.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.StartTls);

                // 認証
                await client.AuthenticateAsync(YourEmail, YourAppPassword);

                _logger.LogInformation("メールを送信しています...");
                await client.SendAsync(message);
                _logger.LogInformation("メールを送信しました。");

                await client.DisconnectAsync(true);
            }


        } catch (WebPushException exception)
        {
            _logger.LogInformation(exception.Message);
            return new OkObjectResult(exception.Message);

        }
        catch (Exception e)
        {
            _logger.LogInformation(e.Message);
            return new OkObjectResult(e.Message);
        }
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Push通知送信");
    }

    class PayLoad { 
        public required string title { get; set; }
        public required string body { get; set; }
    }
    class Subscribe { 

        public string? endpoint { get; set; }
        public string? expirationTime { get; set; }    
        public Keys? keys { get; set; }
        
    }

    class Keys {
        public string? p256dh { get; set; }
        public string? auth { get; set; }
    }

}