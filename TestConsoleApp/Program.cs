using System;
using SearchDescriptor;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp1
{
    static class Program
    {
        public static List<Person> Persons = new List<Person>
        {
            new Person{ Firstname="ewkfg", Birthday=new DateTime(2051,01,12)},
            new Person{ Firstname="0ван", Birthday=new DateTime(1999,04,25)},
            new Person{ Firstname="Иван", Birthday=new DateTime(2000,05,16)},
            new Person{ Firstname="иван", Birthday=new DateTime(2001,11,08)},
        };
        static void Main(string[] args)
        {
            var sd1 = SearchDescriptor<Person>.Create(p => p.Firstname, Operand.Equal, "Иван");
            sd1.OpenBrasket();
            sd1.Or(p => p.Firstname, Operand.NotIncludes, "0");
            sd1.Or(p => p.Birthday, Operand.Equal, new DateTime(2051, 01, 12));
            sd1 = sd1.And(p => p.Firstname, Operand.Equal, "ewkfg");
            sd1.CloseBrasket();

            Console.WriteLine(sd1.ToString());
            Console.WriteLine();
            Console.WriteLine(Persons.AsQueryable().Where(sd1.ToExpression()).Count());
            Console.ReadKey();

        }
    }

    public class Person
    {
        private DateTime? _birthday;

        public long Id { get; set; }
        public bool Isdeleted { get; set; }
        public string Lastname { get; set; }
        public string Middlename { get; set; }
        public string Firstname { get; set; }
        public DateTime? Birthday
        {
            get { return _birthday; }
            set { _birthday = value is null ? default(DateTime?) : DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified); }
        }
        public bool? Sex { get; set; }
        public string Language { get; set; }
        public string Email { get; set; }
        public /*City */ string City { get; set; }
        public string Country { get; set; }
        public DateTime? DeleteDate { get; set; }
        public string Avatarurl { get; set; }
        public bool IsOnline { get; set; }
        public string About { get; set; }
        public string PhoneNumber { get; set; }

        public string FullName => $"{Lastname} {Firstname} {Middlename}".Trim();

        public bool EqualsExceptId(Person second)
        {
            return second.Firstname == Firstname
                && second.Lastname == Lastname
                && second.Middlename == Middlename
                && second.Birthday == Birthday;
        }
    }
}
