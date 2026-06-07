using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentAFriendApp.Models.ClassesDTO.NotificationDTO.Response
{
    public class MarkAllAsReadResponse
    {
        public string Message { get; set; } = string.Empty;
        public int UpdatedCount { get; set; }
    }

}
