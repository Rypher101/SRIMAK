using System;

namespace SRIMAK.Models
{
    public class UserModel
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime DOA { get; set; }
        public string Contact { get; set; }
        public string Address { get; set; }
        public int Type { get; set; }

        public override string ToString()
        {
            return
                $"{nameof(UserId)}: {UserId}, {nameof(Name)}: {Name}, {nameof(Email)}: {Email}, {nameof(DOA)}: {DOA}, {nameof(Contact)}: {Contact}, {nameof(Address)}: {Address}, {nameof(Type)}: {Type}";
        }
    }
}