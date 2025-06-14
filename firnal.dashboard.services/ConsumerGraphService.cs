using firnal.dashboard.data;
using firnal.dashboard.services.Helpers;
using firnal.dashboard.services.Interfaces;
using Microsoft.Playwright;

namespace firnal.dashboard.services
{
    public class ConsumerGraphService : IConsumerGraphService
    {
        private readonly ITwilioService _twilioService;

        public ConsumerGraphService(ITwilioService twilioService)
        {
            _twilioService = twilioService;
        }

        public async Task GetSearchResults(SolomonSearchRequest filters, string userEmail)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true // set to true to run without UI
            });

            // create page
            var page = await browser.NewPageAsync();

            // log in to OG
            await page.OutreachGeniusLogin();

            // navigate to search page
            await page.NavigateToSolomonSearch();

            // enter personal details
            await page.EnterPersonalInfoDetails(filters.PersonalInfo);

            // enter professional details
            await page.EnterProfessionalDetails(filters.ProfessionalInfo);

            // enter commpany details
            await page.EnterCompanyDetails(filters.CompanyInfo);

            // search inputs
            await page.SearchInputs();

            // save results
            await page.SaveResults(userEmail);

            // send text notification
            await _twilioService.SendSmsAsync(userEmail);
        }
    }
}
