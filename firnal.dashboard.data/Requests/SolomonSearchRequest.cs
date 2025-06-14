namespace firnal.dashboard.data
{
    public class SolomonSearchRequest
    {
        public PersonalInfo PersonalInfo { get; set; } = new();
        public ProfessionalInfo ProfessionalInfo { get; set; } = new();
        public CompanyInfo CompanyInfo { get; set; } = new();
    }

    public class PersonalInfo
    {
        public string? Include_FirstName { get; set; }
        public string? Exclude_FirstName { get; set; }

        public string? Include_LastName { get; set; }
        public string? Exclude_LastName { get; set; }

        public string? Include_Gender { get; set; }
        public string? Exclude_Gender { get; set; }

        public string? Include_AgeRange { get; set; }
        public string? Exclude_AgeRange { get; set; }

        public string? Include_IsMarried { get; set; }
        public string? Exclude_IsMarried { get; set; }

        public string? Include_HasChildren { get; set; }
        public string? Exclude_HasChildren { get; set; }

        public string? Include_PersonalPhone { get; set; }
        public string? Exclude_PersonalPhone { get; set; }

        public string? Include_PersonalAddress { get; set; }
        public string? Exclude_PersonalAddress { get; set; }

        public string? Include_HomeOwner { get; set; }
        public string? Exclude_HomeOwner { get; set; }

        public string? Include_PersonalCity { get; set; }
        public string? Exclude_PersonalCity { get; set; }

        public string? Include_PersonalState { get; set; }
        public string? Exclude_PersonalState { get; set; }

        public string? Include_PersonalZip { get; set; }
        public string? Exclude_PersonalZip { get; set; }

        public string? Include_PersonalEmails { get; set; }
        public string? Exclude_PersonalEmails { get; set; }

        public string? Include_LinkedInUrl { get; set; }
        public string? Exclude_LinkedInUrl { get; set; }

        public string? Include_IncomeRange { get; set; }
        public string? Exclude_IncomeRange { get; set; }

        public string? Include_NetWorth { get; set; }
        public string? Exclude_NetWorth { get; set; }
    }

    public class ProfessionalInfo
    {
        public string? Include_JobTitle { get; set; }
        public string? Exclude_JobTitle { get; set; }

        public string? Include_SeniorityLevel { get; set; }
        public string? Exclude_SeniorityLevel { get; set; }

        public string? Include_Department { get; set; }
        public string? Exclude_Department { get; set; }

        public string? Include_BusinessEmails { get; set; }
        public string? Exclude_BusinessEmails { get; set; }

        public string? Include_ProfessionalAddress { get; set; }
        public string? Exclude_ProfessionalAddress { get; set; }

        public string? Include_ProfessionalCity { get; set; }
        public string? Exclude_ProfessionalCity { get; set; }

        public string? Include_ProfessionalState { get; set; }
        public string? Exclude_ProfessionalState { get; set; }

        public string? Include_ProfessionalZip { get; set; }
        public string? Exclude_ProfessionalZip { get; set; }

        public string? Include_WorkHistory { get; set; }
        public string? Exclude_WorkHistory { get; set; }

        public string? Include_EducationHistory { get; set; }
        public string? Exclude_EducationHistory { get; set; }
    }

    public class CompanyInfo
    {
        public string? Include_CompanyName { get; set; }
        public string? Exclude_CompanyName { get; set; }

        public string? Include_Website { get; set; }
        public string? Exclude_Website { get; set; }

        public string? Include_CCID { get; set; }
        public string? Exclude_CCID { get; set; }

        public string? Include_SICCode { get; set; }
        public string? Exclude_SICCode { get; set; }

        public string? Include_CompanyAddress { get; set; }
        public string? Exclude_CompanyAddress { get; set; }

        public string? Include_CompanyCity { get; set; }
        public string? Exclude_CompanyCity { get; set; }

        public string? Include_CompanyState { get; set; }
        public string? Exclude_CompanyState { get; set; }

        public string? Include_CompanyZip { get; set; }
        public string? Exclude_CompanyZip { get; set; }

        public string? Include_CompanyLinkedInUrl { get; set; }
        public string? Exclude_CompanyLinkedInUrl { get; set; }

        public string? Include_CompanyRevenue { get; set; }
        public string? Exclude_CompanyRevenue { get; set; }

        public string? Include_CompanyEmployeeCount { get; set; }
        public string? Exclude_CompanyEmployeeCount { get; set; }

        public string? Include_PrimaryIndustry { get; set; }
        public string? Exclude_PrimaryIndustry { get; set; }

        public string? Include_CompanyDescription { get; set; }
        public string? Exclude_CompanyDescription { get; set; }

        public string? Include_RelatedDomains { get; set; }
        public string? Exclude_RelatedDomains { get; set; }
    }
}
