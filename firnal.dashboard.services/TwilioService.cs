using firnal.dashboard.services.Interfaces;
using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace firnal.dashboard.services
{
    public class TwilioService : ITwilioService
    {
        private readonly string? _accountSid;
        private readonly string? _authToken;
        private readonly string? _fromPhoneNumber;
        private readonly string? _toPhoneNumber;

        public TwilioService(IConfiguration config)
        {
            _accountSid = config["TwilioSettings:AccountSid"] ?? throw new Exception("AccountSid string not found.");
            _authToken = config["TwilioSettings:AuthToken"] ?? throw new Exception("AuthToken string not found."); ;
            _fromPhoneNumber = config["TwilioSettings:FromPhoneNumber"] ?? throw new Exception("To PhoneNumber string not found.");
            _toPhoneNumber = config["TwilioSettings:ToPhoneNumber"] ?? throw new Exception("To PhoneNumber string not found.");

            TwilioClient.Init(_accountSid, _authToken);
        }

        public Task SendSmsAsync(string user)
        {
            var message = MessageResource.Create(
                body: $"Solomon search for {user} is ready.  Please download and email to user",
                from: new Twilio.Types.PhoneNumber(_fromPhoneNumber),
                to: new Twilio.Types.PhoneNumber(_toPhoneNumber)
            );

            return Task.CompletedTask;
        }
    }
}
