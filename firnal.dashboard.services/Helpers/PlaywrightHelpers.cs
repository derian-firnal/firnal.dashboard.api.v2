using firnal.dashboard.data;
using Microsoft.Playwright;

namespace firnal.dashboard.services.Helpers
{
    public static class PlaywrightHelpers
    {
        public async static Task<IPage> OutreachGeniusLogin(this IPage page)
        {
            // 1. Navigate to sign-in page
            await page.GotoAsync("https://outreachgenius.ai/auth/sign-in");

            // 2. Fill in the email and password
            await page.Locator("input[name='email']").FillAsync("president@firnal.com");
            await page.Locator("input[name='password']").FillAsync("FirnalVikings2024");

            // 3. Click the "Login" button
            await page.GetByRole(AriaRole.Button, new() { Name = "Login" }).ClickAsync();

            // 4. Wait for navigation or feedback
            await page.WaitForTimeoutAsync(3000);

            return page;
        }

        public async static Task<IPage> NavigateToSolomonSearch(this IPage page)
        {
            // 1. Go to find-customers page
            await page.GotoAsync("https://outreachgenius.ai/user/find-customers");

            // 2. Click "Solomon" tab (left one)
            await page.Locator("div:text('Solomon')").First.ClickAsync();

            // 3. Click "Solomon Search" (second tab below it)
            await page.Locator("div:text('Solomon Search')").First.ClickAsync();

            return page;
        }

        public async static Task<IPage> EnterPersonalInfoDetails(this IPage page, PersonalInfo filters)
        {
            // 1. Click to expand the Personal Information section
            await page.Locator("text=Personal Information").WaitForAsync();
            await page.Locator("text=Personal Information").ClickAsync();

            // 2. Get all "Include" inputs for the personal information section
            var includeInputs = page.Locator("div:has-text(\"Personal Information\")").Locator("form input[type='text']");

            var fields = new (string? Value, int Index)[]
            {
                (filters.Include_FirstName, 0),
                (filters.Exclude_FirstName, 1),
                (filters.Include_LastName, 2),
                (filters.Exclude_LastName, 3),
                (filters.Include_Gender, 4),
                (filters.Exclude_Gender, 5),
                (filters.Include_AgeRange, 6),
                (filters.Exclude_AgeRange, 7),
                (filters.Include_IsMarried, 8),
                (filters.Exclude_IsMarried, 9),
                (filters.Include_HasChildren, 10),
                (filters.Exclude_HasChildren, 11),
                (filters.Include_PersonalPhone, 12),
                (filters.Exclude_PersonalPhone, 13),
                (filters.Include_PersonalAddress, 14),
                (filters.Exclude_PersonalAddress, 15),
                (filters.Include_HomeOwner, 16),
                (filters.Exclude_HomeOwner, 17),
                (filters.Include_PersonalCity, 18),
                (filters.Exclude_PersonalCity, 19),
                (filters.Include_PersonalState, 20),
                (filters.Exclude_PersonalState, 21),
                (filters.Include_PersonalZip, 22),
                (filters.Exclude_PersonalZip, 23),
                (filters.Include_PersonalEmails, 24),
                (filters.Exclude_PersonalEmails, 25),
                (filters.Include_LinkedInUrl, 26),
                (filters.Exclude_LinkedInUrl, 27),
                (filters.Include_IncomeRange, 28),
                (filters.Exclude_IncomeRange, 29),
                (filters.Include_NetWorth, 30),
                (filters.Exclude_NetWorth, 31),
            };

            // 3. Fill in all entered personal info section
            foreach (var (value, index) in fields)
                if (!string.IsNullOrWhiteSpace(value))
                    await includeInputs.Nth(index).FillAsync(value);

            return page;
        }

        public async static Task<IPage> EnterProfessionalDetails(this IPage page, ProfessionalInfo filters)
        {
            // 1. Expand the Professional Information section
            await page.Locator("text=Professional Information").WaitForAsync();
            await page.Locator("text=Professional Information").ClickAsync();
          
            // 2. Get all "Include" inputs for the personal information section
            var includeInputs = page.Locator("div:has-text(\"Personal Information\")").Locator("form input[type='text']");

            var fields = new (string? Value, int Index)[]
            {
                (filters.Include_JobTitle, 32),
                (filters.Exclude_JobTitle, 33),
                (filters.Include_SeniorityLevel, 34),
                (filters.Exclude_SeniorityLevel, 35),
                (filters.Include_Department, 36),
                (filters.Exclude_Department, 37),
                (filters.Include_BusinessEmails, 38),
                (filters.Exclude_BusinessEmails, 39),
                (filters.Include_ProfessionalAddress, 40),
                (filters.Exclude_ProfessionalAddress, 41),
                (filters.Include_ProfessionalCity, 42),
                (filters.Exclude_ProfessionalCity, 43),
                (filters.Include_ProfessionalState, 44),
                (filters.Exclude_ProfessionalState, 45),
                (filters.Include_ProfessionalZip, 46),
                (filters.Exclude_ProfessionalZip, 47),
                (filters.Include_WorkHistory, 48),
                (filters.Exclude_WorkHistory, 49),
                (filters.Include_EducationHistory, 50),
                (filters.Exclude_EducationHistory, 51)
            };

            // 4. Fill in the inputs
            foreach (var (value, index) in fields)
                if (!string.IsNullOrWhiteSpace(value))
                    await includeInputs.Nth(index).FillAsync(value);

            return page;
        }

        public async static Task<IPage> EnterCompanyDetails(this IPage page, CompanyInfo filters)
        {
            // 1. Click to expand the Personal Information section
            await page.Locator("text=Company Information").WaitForAsync();
            await page.Locator("text=Company Information").ClickAsync();

            // 2. Get all "Include" inputs for the personal information section
            // var includeInputs = page.Locator("div:has-text(\"Company Information\")").Locator("form input[type='text']");
            var includeInputs = page.Locator("div:has-text(\"Personal Information\")").Locator("form input[type='text']");

            var fields = new (string? Value, int Index)[]
            {
                (filters.Include_CompanyName, 52),
                (filters.Exclude_CompanyName, 53),
                (filters.Include_Website, 54),
                (filters.Exclude_Website, 55),
                (filters.Include_CCID, 56),
                (filters.Exclude_CCID, 57),
                (filters.Include_SICCode, 58),
                (filters.Exclude_SICCode, 59),
                (filters.Include_CompanyAddress, 60),
                (filters.Exclude_CompanyAddress, 61),
                (filters.Include_CompanyCity, 62),
                (filters.Exclude_CompanyCity, 63),
                (filters.Include_CompanyState, 64),
                (filters.Exclude_CompanyState, 65),
                (filters.Include_CompanyZip, 66),
                (filters.Exclude_CompanyZip, 67),
                (filters.Include_CompanyLinkedInUrl, 68),
                (filters.Exclude_CompanyLinkedInUrl, 69),
                (filters.Include_CompanyRevenue, 70),
                (filters.Exclude_CompanyRevenue, 71),
                (filters.Include_CompanyEmployeeCount, 72),
                (filters.Exclude_CompanyEmployeeCount, 73),
                (filters.Include_PrimaryIndustry, 74),
                (filters.Exclude_PrimaryIndustry, 75),
                (filters.Include_CompanyDescription, 76),
                (filters.Exclude_CompanyDescription, 77),
                (filters.Include_RelatedDomains, 78),
                (filters.Exclude_RelatedDomains, 79)
            };

            // 3. Fill in all entered personal info section
            foreach (var (value, index) in fields)
                if (!string.IsNullOrWhiteSpace(value))
                    await includeInputs.Nth(index).FillAsync(value);

            return page;
        }

        public async static Task<IPage> SearchInputs(this IPage page)
        {
            // 1: Click the "Search" button (if it exists and is visible)
            await page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();

            // 2. wait for results
            await page.WaitForTimeoutAsync(5000); // wait 5 seconds

            return page;
        }

        public async static Task<IPage> SaveResults(this IPage page, string userEmail)
        {
            // 1. Save results
            await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

            // 2. Enter Campaign name
            var campaignNameInput = page.Locator("div:has-text(\"Campaign Name\") >> xpath=..").Locator("input[type='text']").First;
            var campaignName = $"{DateTime.Now:yyyy-MM-dd_HH:mm:ss}-{userEmail}";
            await campaignNameInput.FillAsync(campaignName);

            // 3. Click save results button
            await page.GetByRole(AriaRole.Button, new() { Name = "Save Results" }).ClickAsync();

            // 4. Wait before closing window
            await page.WaitForTimeoutAsync(5000);

            return page;
        }
    }
}
