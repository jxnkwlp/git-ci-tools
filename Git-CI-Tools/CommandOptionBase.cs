namespace Git_CI_Tools
{
    public class CommandOptionBase
    {
        public string Project { get; set; }
        public string Branch { get; set; }
        public bool IncludePrerelease { get; set; }

        public string Format { get; set; }
        public string Output { get; set; }
    }
}
