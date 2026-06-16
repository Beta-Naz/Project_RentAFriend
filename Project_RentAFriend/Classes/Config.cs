using Microsoft.EntityFrameworkCore;

namespace Project_RentAFriend.Classes
{
    public class Config
    {
        public static readonly string ConnectionString =
            "server=localhost;" +
            "database=rentafrienddb;" +
            "uid=root;" +
            "pwd=;";
        public static readonly MySqlServerVersion CurrentVersion = new(new Version(8, 0, 11));
    }
}
