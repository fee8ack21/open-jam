namespace EmailService.Options;

public class SmtpOptions
{
    public string Host        { get; set; } = "localhost";
    public int    Port        { get; set; } = 587;
    public bool   UseSsl      { get; set; } = false;
    public string Username    { get; set; } = "";
    public string Password    { get; set; } = "";
    public string FromAddress { get; set; } = "";
}
