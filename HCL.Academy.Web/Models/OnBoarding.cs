namespace HCLAcademy.Models
{
    public class OnBoarding
    {
        public int BoardingItemId { get; set; }

        public string BoardingItemName { get; set; }

        public string BoardingInternalName { get; set; }

        public string BoardingItemDesc { get; set; }

        public string BoardingItemLink { get; set; }

        public bool IsWikiLink { get; set; }

        public int BoardIngTrainingId { get; set; }

        public int BoardIngAssessmentId { get; set; }
        public bool BoardingIsMandatory { get; set; }

        public OnboardingStatus BoardingStatus { get; set; }

        public OnboardingItemType BoardingType { get; set; }

    }

    public class OnBoardingTrainingStatus
    {
        public int Id { get; set; }
        public OnboardingItemType OnboardingType { get; set; }
        public bool SendEmail { get; set; }
        public string BoardingInternalName { get; set; }
    }

    public enum OnboardingStatus
    {
        NotStarted,
        OnGoing,
        Completed,
        Rejected,
        OverDue,
        Failed,

    }

    public enum OnboardingItemType
    {
        Default,
        Assessment,
        Training,
        RoleTraining
    }

    public enum bgColor
    {
        red,
        orange,
        green,
        blue
    }
}