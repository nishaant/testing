using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication3.Models
{
    public class UsedCarStock
    {
            public UsedCarStock()
            {
                this._fuelEconomy = -1;
                this._dateAdded = DateTime.Now;
                this._isDeleted = 0;
                this._imgURL = "car.jpg";
            }
            private int _stockId;
            public int StockId
            {
                get { return _stockId; }
                set { _stockId = value; }
            }

            private string _city;
            public string City
            {
                get { return _city; }
                set { _city = value; }
            }

            private string _model;
            public string Model
            {
                get { return _model; }
                set { _model = value; }
            }

            private string _make;
            public string Make
            {
                get { return _make; }
                set { _make = value; }
            }

            private string _version;
            public string Version
            {
                get { return _version; }
                set { _version = value; }
            }

            private string _fuelType;
            public string FuelType
            {
                get { return _fuelType; }
                set { _fuelType = value; }
            }

            private string _color;
            public string Color
            {
                get { return _color; }
                set { _color = value; }
            }

            private string _imgURL;
            public string ImgURL
            {
                get { return _imgURL; }
                set { _imgURL = value; }
            }


            private decimal _price;
            public decimal Price
            {
                get { return _price; }
                set { _price = value; }
            }

            private decimal _fuelEconomy;
            public decimal FuelEconomy
            {
                get { return _fuelEconomy; }
                set { _fuelEconomy = value; }
            }

            private int _year;
            public int Year
            {
                get { return _year; }
                set { _year = value; }
            }

            private int _kilometer;
            public int Kilometer
            {
                get { return _kilometer; }
                set { _kilometer = value; }
            }

            private int _isDeleted;
            public int IsDeleted
            {
                get { return _isDeleted; }
                set { _isDeleted = value; }
            }

            private DateTime _dateAdded;
            public DateTime DateAdded
            {
                get { return _dateAdded; }
                set { _dateAdded = value; }
            }

        }
    }
