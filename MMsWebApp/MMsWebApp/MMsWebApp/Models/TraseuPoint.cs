using CsvHelper.Configuration;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;
using System.Globalization;

namespace MMsWebApp.Models
{
    public class TraseuPoint
    {
        [Name("nr_masina")]
        public required string NrMasina { get; set; }

        [Name("id_pubela")]
        public required string IdPubela { get; set; }

        [Name("colectat_la")]
        [TypeConverter(typeof(CustomDateTimeConverter))]
        public DateTime ColectatLa { get; set; }

        [Name("adresa")]
        public required string Adresa { get; set; }

        [Name("Latitude")]
        public double Latitude { get; set; }

        [Name("Longitude")]
        public double Longitude { get; set; }
    }

    public class CustomDateTimeConverter : DateTimeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrEmpty(text))
            {
                return DateTime.MinValue; // sau orice valoare implicită dorită
            }
            return base.ConvertFromString(text, row, memberMapData);
        }
    }
}