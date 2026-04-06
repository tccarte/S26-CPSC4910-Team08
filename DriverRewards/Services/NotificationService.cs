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
    private readonly AuditService _auditService;

    public NotificationService(IConfiguration config, ILogger<NotificationService> logger, AuditService auditService)
    {
        _config = config;
        _logger = logger;
        _auditService = auditService;
    }

    public async Task SendBulkMessageAsync(Driver driver, string message, ApplicationDbContext context)
    {
        var notification = new DriverNotification
        {
            DriverId = driver.DriverId,
            Type = "SponsorMessage",
            Message = message,
            CreatedAt = DateTime.UtcNow
        };
        context.DriverNotifications.Add(notification);
        await context.SaveChangesAsync();

        if (driver.NotifyEmailPoints)
            await SendEmailAsync(driver.Email, "Message from your Sponsor", message);

        if (driver.NotifySmsPoints && !string.IsNullOrWhiteSpace(driver.Phone))
            await SendSmsAsync(driver.Phone, $"DriverRewards: {message}");
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
            await _auditService.LogEventAsync(
                category: "Notification",
                action: "EmailSkipped",
                description: $"Skipped email to {toAddress} because SMTP is not configured.",
                metadata: new { Channel = "Email", Recipient = toAddress, Subject = subject });
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
            await _auditService.LogEventAsync(
                category: "Notification",
                action: "EmailSent",
                description: $"Sent email to {toAddress}.",
                metadata: new { Channel = "Email", Recipient = toAddress, Subject = subject });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}.", toAddress);
            await _auditService.LogEventAsync(
                category: "Notification",
                action: "EmailFailed",
                description: $"Email delivery failed for {toAddress}.",
                metadata: new { Channel = "Email", Recipient = toAddress, Subject = subject, ex.Message });
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
            await _auditService.LogEventAsync(
                category: "Notification",
                action: "SmsSkipped",
                description: $"Skipped SMS to {toPhone} because Twilio is not configured.",
                metadata: new { Channel = "Sms", Recipient = toPhone });
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
            await _auditService.LogEventAsync(
                category: "Notification",
                action: "SmsSent",
                description: $"Sent SMS to {toPhone}.",
                metadata: new { Channel = "Sms", Recipient = toPhone });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {To}.", toPhone);
            await _auditService.LogEventAsync(
                category: "Notification",
                action: "SmsFailed",
                description: $"SMS delivery failed for {toPhone}.",
                metadata: new { Channel = "Sms", Recipient = toPhone, ex.Message });
        }
    }
}
