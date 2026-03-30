using System.Net;
using System.Net.Mail;
using DriverRewards.Data;
using DriverRewards.Models;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace DriverRewards.Services;

public class NotificationService
{
    private readonly IConfiguration _config;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IConfiguration config, ILogger<NotificationService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task NotifyPointsChangedAsync(Driver driver, int pointChange, ApplicationDbContext context)
    {
        var direction = pointChange >= 0 ? "added" : "removed";
        var abs = Math.Abs(pointChange);
        var message = $"{abs} points {direction}. Your new balance is {driver.NumPoints ?? 0} points.";

        var notification = new DriverNotification
        {
            DriverId = driver.DriverId,
            Type = "PointsChanged",
            Message = message,
            CreatedAt = DateTime.UtcNow
        };
        context.DriverNotifications.Add(notification);
        await context.SaveChangesAsync();

        if (driver.NotifyEmailPoints)
            await SendEmailAsync(driver.Email, "Your DriverRewards Points Were Updated", message);

        if (driver.NotifySmsPoints && !string.IsNullOrWhiteSpace(driver.Phone))
            await SendSmsAsync(driver.Phone, $"DriverRewards: {message}");
    }

    public async Task NotifyOrderPlacedAsync(Driver driver, Order order, ApplicationDbContext context)
    {
        var eta = order.EstimatedDeliveryAt.ToLocalTime().ToString("MM/dd/yyyy");
        var message = $"Your order #{order.TrackingNumber} has been placed for {order.TotalPoints} points. Estimated delivery: {eta}.";

        var notification = new DriverNotification
        {
            DriverId = driver.DriverId,
            Type = "OrderPlaced",
            Message = message,
            CreatedAt = DateTime.UtcNow
        };
        context.DriverNotifications.Add(notification);
        await context.SaveChangesAsync();

        if (driver.NotifyEmailOrder)
            await SendEmailAsync(driver.Email, "Your DriverRewards Order Was Placed", message);

        if (driver.NotifySmsOrder && !string.IsNullOrWhiteSpace(driver.Phone))
            await SendSmsAsync(driver.Phone, $"DriverRewards: {message}");
    }

    private async Task SendEmailAsync(string toAddress, string subject, string body)
    {
        var smtpSection = _config.GetSection("Smtp");
        var host = smtpSection["Host"];
        var username = smtpSection["Username"];
        var password = smtpSection["Password"];
        var fromAddress = smtpSection["FromAddress"];
        var fromName = smtpSection["FromName"] ?? "DriverRewards";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("SMTP not configured. Skipping email to {To}.", toAddress);
            return;
        }

        if (!int.TryParse(smtpSection["Port"], out var port))
            port = 587;

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(fromAddress!, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            mail.To.Add(toAddress);

            await client.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}.", toAddress);
        }
    }

    private async Task SendSmsAsync(string toPhone, string body)
    {
        var twilioSection = _config.GetSection("Twilio");
        var accountSid = twilioSection["AccountSid"];
        var authToken = twilioSection["AuthToken"];
        var fromPhone = twilioSection["FromPhone"];

        if (string.IsNullOrWhiteSpace(accountSid) || accountSid.StartsWith("your-") ||
            string.IsNullOrWhiteSpace(authToken) || string.IsNullOrWhiteSpace(fromPhone))
        {
            _logger.LogWarning("Twilio not configured. Skipping SMS to {To}.", toPhone);
            return;
        }

        try
        {
            TwilioClient.Init(accountSid, authToken);
            await MessageResource.CreateAsync(
                body: body,
                from: new Twilio.Types.PhoneNumber(fromPhone),
                to: new Twilio.Types.PhoneNumber(toPhone)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {To}.", toPhone);
        }
    }
}
