using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using CarDealer.Data;
using CarDealer.Dtos.Import;
using CarDealer.Models;

namespace CarDealer
{
    public class StartUp
    {
        public static void Main(string[] args)
        {
            CarDealerContext dbContext = new CarDealerContext();

            string xml = File.ReadAllText("../../../Datasets/cars.xml");

            string result = ImportCars(dbContext, xml);

            Console.WriteLine(result);

            //dbContext.Database.EnsureDeleted();
            //dbContext.Database.EnsureCreated();
            //Console.WriteLine("Database reset successfully");
        }

        //Query 9. Import Suppliers
        public static string ImportSuppliers(CarDealerContext context, string inputXml)
        {
            XmlRootAttribute xmlRoot = new XmlRootAttribute("Suppliers");
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ImportSuppliersDto[]), xmlRoot);

            using StringReader reader = new StringReader(inputXml);
            ImportSuppliersDto[] suppliersDto = (ImportSuppliersDto[])xmlSerializer.Deserialize(reader);

            Supplier[] suppliers = suppliersDto
                .Select(dto => new Supplier()
                {
                    Name = dto.Name,
                    IsImporter = dto.IsImporter,
                })
                .ToArray();

            context.Suppliers.AddRange(suppliers);
            context.SaveChanges();

            return $"Successfully imported {suppliers.Length}";
        }

        //Query 10. Import Parts
        public static string ImportParts(CarDealerContext context, string inputXml)
        {
            XmlRootAttribute xmlRoot = new XmlRootAttribute("Parts");
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ImportPartsDto[]), xmlRoot);

            using StringReader reader = new StringReader(inputXml);
            ImportPartsDto[] partsDtos = (ImportPartsDto[])xmlSerializer.Deserialize(reader);

            //If the supplierId doesn't exist, skip the record:
            ICollection<Part> parts = new List<Part>();
            foreach (ImportPartsDto partsDto in partsDtos)
            {
                if (!context.Suppliers.Any(s => s.Id == partsDto.SupplierId))
                {
                    continue;
                }

                Part part = new Part()
                {
                    Name = partsDto.Name,
                    Price = partsDto.Price,
                    Quantity = partsDto.Quantity,
                    SupplierId = partsDto.SupplierId,
                };

                parts.Add(part);
            }

            context.Parts.AddRange(parts);
            context.SaveChanges();

            return $"Successfully imported {parts.Count}";
        }

        //Query 11. Import Cars
        public static string ImportCars(CarDealerContext context, string inputXml)
        {
            XmlRootAttribute xmlRoot = new XmlRootAttribute("Cars");
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ImportCarsDto[]), xmlRoot);

            using StringReader reader = new StringReader(inputXml);
            ImportCarsDto[] carsDtos = (ImportCarsDto[])xmlSerializer.Deserialize(reader);

            ICollection<Car> cars = new List<Car>();
            foreach (ImportCarsDto carsDto in carsDtos)
            {
                Car car = new Car()
                {
                    Make = carsDto.Make,
                    Model = carsDto.Model,
                    TravelledDistance = carsDto.TraveledDistance
                };

                ICollection<PartCar> currCarPart = new List<PartCar>();
                foreach (int partId in carsDto.Parts.Select(p => p.Id).Distinct())
                {
                    if (!context.Parts.Any(p => p.Id == partId))
                    {
                        continue;
                    }

                    currCarPart.Add(new PartCar()
                    {
                        Car = car,
                        PartId = partId
                    });
                }

                car.PartCars = currCarPart;
                cars.Add(car);
            }

            context.Cars.AddRange(cars);
            context.SaveChanges();

            return $"Successfully imported {cars.Count}";
        }

        //Helper xml method
        private static T Deserialize<T>(string inputXml, string rootName)
        {
            XmlRootAttribute xmlRoot = new XmlRootAttribute(rootName);
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T), xmlRoot);

            using StringReader reader = new StringReader(inputXml);
            T dtos = (T)xmlSerializer.Deserialize(reader);

            return dtos;
        }
    }
}