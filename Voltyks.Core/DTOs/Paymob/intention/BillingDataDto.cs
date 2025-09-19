using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs.Paymob.intention
{
    public class BillingDataDto
    {
        public string First_Name { get; set; }
        public string Last_Name { get; set; }
        public string Email { get; set; }
        public string Phone_Number { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Street { get; set; }
        public string Building { get; set; }
        public string Apartment { get; set; }
        public string Floor { get; set; }
        public string Postal_Code { get; set; }

        // Constructor to initialize the class
        public BillingDataDto(
            string first_Name,
            string last_Name,
            string email,
            string phone_Number,
            string country,
            string city,
            string state,
            string street,
            string building,
            string apartment,
            string floor,
            string postal_Code = "")
        {
            First_Name = first_Name;
            Last_Name = last_Name;
            Email = email;
            Phone_Number = phone_Number;
            Country = country;
            City = city;
            State = state;
            Street = street;
            Building = building;
            Apartment = apartment;
            Floor = floor;
            Postal_Code = postal_Code;
        }
    }

}
