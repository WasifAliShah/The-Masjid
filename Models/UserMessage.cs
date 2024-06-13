namespace Project2.Models
{
    public class UserMessage
    {
        public string Username { get; set; }
        public string Message { get; set; }
        public bool CurrentUser { get; set; }
        public DateTime DateSent { get; set; }

        public UserMessage(string username, string message, bool currentUser, DateTime dateSent)
        {
            Username = username;
            Message = message;
            CurrentUser = currentUser;
            DateSent = dateSent;
        }
    }
}
