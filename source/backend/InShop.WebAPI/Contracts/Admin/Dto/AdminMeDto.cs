namespace Contracts.Admin.Dto
{
    public class AdminMeDto
    {
        public string Email { get; set; } = null!;
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
