using System.ComponentModel;

namespace CustomAgent.Server.Tools;

public class GetDateTime
{
    [Description("date time formatted as dddd, MMMM dd, yyyy HH:mm:ss zzz")]
    [return: Description("date time formatted as dddd, MMMM dd, yyyy HH:mm:ss zzz")]
    public string GetCurrentDateTime()
    {
        return DateTime.Now.ToString("dddd, MMMM dd, yyyy HH:mm:ss zzz");
    }
}
