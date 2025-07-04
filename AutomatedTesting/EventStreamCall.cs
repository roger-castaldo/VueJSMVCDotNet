using AutomatedTesting.Handlers;
using AutomatedTesting.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using static AutomatedTesting.Models.mDataTypes;

namespace AutomatedTesting
{
    [TestClass]
    public class EventStreamCall
    {
        [TestMethod]
        public async Task TestBasicStaticStreamCall()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            
            //Act
            var (responseStream, responseStatus, responseHeaders)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.PersonModelRoute}/StreamUsers", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual(200, responseStatus);
            Assert.AreEqual("text/event-stream", responseHeaders.GetValues("Content-Type").First());
            var split = content.Split('\n');
            Assert.AreEqual(mPersonHandler.Persons.Length, split.Count(s => Equals("event: message", s)));
            Assert.AreEqual(1, split.Count(s => Equals("event: close", s)));
            var dataItems = split.Where(s => s.StartsWith("data: ")).ToArray();
            Assert.AreEqual(mPersonHandler.Persons.Length+1, dataItems.Count());
            Assert.AreEqual(1, dataItems.Count(s => Equals("data: complete", s)));
            var providedPerson = JsonSerializer.Deserialize<mPerson>(dataItems[mPersonHandler.Persons.Length-1].Substring(6));
            var actualPerson = mPersonHandler.Persons[mPersonHandler.Persons.Length-1];
            Assert.IsTrue(
                Equals(providedPerson.id,actualPerson.id) &&
                Equals(providedPerson.Age, actualPerson.Age) &&
                Equals(providedPerson.LastName, actualPerson.LastName) &&
                Equals(providedPerson.FirstName, actualPerson.FirstName)
            );
        }

        [TestMethod]
        public async Task TestBasicInstanceStreamCall()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            var person = mPersonHandler.Persons[0];

            //Act
            var (responseStream, responseStatus, responseHeaders)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.PersonModelRoute}/{person.id}/StreamUserProperties", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual(200, responseStatus);
            Assert.AreEqual("text/event-stream", responseHeaders.GetValues("Content-Type").First());
            var split = content.Split('\n').Where(s=>!string.IsNullOrWhiteSpace(s)).ToArray();
            Assert.AreEqual(8, split.Length);
            Assert.AreEqual("event: message", split[0]);
            Assert.AreEqual($"data: \"{person.FirstName}\"", split[1]);
            Assert.AreEqual("event: message", split[2]);
            Assert.AreEqual($"data: \"{person.LastName}\"", split[3]);
            Assert.AreEqual("event: message", split[4]);
            Assert.AreEqual($"data: {person.Age}", split[5]);
            Assert.AreEqual("event: close", split[6]);
            Assert.AreEqual("data: complete", split[7]);
        }

        [TestMethod]
        public async Task TestStaticStreamCallWithParameter()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            var person = mPersonHandler.Persons[0];

            //Act
            var (responseStream, responseStatus, responseHeaders)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.PersonModelRoute}/StreamUserPropertiesById?id={person.id}", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual(200, responseStatus);
            Assert.AreEqual("text/event-stream", responseHeaders.GetValues("Content-Type").First());
            var split = content.Split('\n').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            Assert.AreEqual(8, split.Length);
            Assert.AreEqual("event: message", split[0]);
            Assert.AreEqual($"data: \"{person.FirstName}\"", split[1]);
            Assert.AreEqual("event: message", split[2]);
            Assert.AreEqual($"data: \"{person.LastName}\"", split[3]);
            Assert.AreEqual("event: message", split[4]);
            Assert.AreEqual($"data: {person.Age}", split[5]);
            Assert.AreEqual("event: close", split[6]);
            Assert.AreEqual("data: complete", split[7]);
        }

        [TestMethod]
        public async Task TestStaticStreamCallWithAllValidParameterTypes()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);
            IEnumerable<KeyValuePair<string, object?>> values = [
                new("stringArg","test"),
                new("nullStringArg",null),
                new("charArg",'t'),
                new("nullCharArg",null),
                new("shortArg",short.MinValue),
                new("nullShortArg",null),
                new("ushortArg",ushort.MinValue),
                new("nullUShortArg",null),
                new("intArg",int.MinValue),
                new("nullIntArg",null),
                new("uintArg",uint.MinValue),
                new("nullUIntArg",null),
                new("longArg",long.MinValue),
                new("nullLongArg",null),
                new("ulongArg",ulong.MinValue),
                new("nullULongArg",null),
                new("decimalArg",decimal.MinValue),
                new("nullDecimalArg",null),
                new("floatArg",float.MinValue),
                new("nullFloatArg",null),
                new("doubleArg",double.MinValue),
                new("nullDoubleArg",null),
                new("byteArg",byte.MinValue),
                new("nullByteArg",null),
                new("boolArg",false),
                new("nullBooleanArg",null),
                new("enumArg",TestEnums.Test1),
                new("nullEnumArg",null),
                new("DateTimeArg",DateTime.UtcNow),
                new("nullDateTimeArg",null),
                new("IPAddressArg",IPAddress.Loopback),
                new("nullIPAddressArg",null),
                new("VersionArg",new Version("1.0.0")),
                new("nullVersionArg",null),
                new("guidArg",Guid.NewGuid()),
                new("nullGuidArg",null),
            ];

            //Act
            var (responseStream, responseStatus, responseHeaders)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.DataTypesModelRoute}/TestStaticEventStreamInputs?{
                string.Join('&',values.Select(pair=>$"{pair.Key}={(pair.Value==null ? "null" : HttpUtility.UrlEncode(pair.Value.ToString()))}"))
            }", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.IsTrue(content.Length>0);
            Assert.AreEqual(200, responseStatus);
            Assert.AreEqual("text/event-stream", responseHeaders.GetValues("Content-Type").First());
            var split = content.Split('\n').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            Assert.AreEqual(4, split.Length);
            Assert.AreEqual("event: message", split[0]);
            Assert.AreEqual("data: \"complete\"", split[1]);
            Assert.AreEqual("event: close", split[2]);
            Assert.AreEqual("data: complete", split[3]);
        }

        [TestMethod]
        public async Task TestStaticStreamCallWithMissingParameter()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.PersonModelRoute}/StreamUserPropertiesById", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(404, responseStatus);
            Assert.AreEqual("Not Found", content);
        }

        [TestMethod]
        public async Task TestStaticStreamCallWithInvalidParameter()
        {
            //Arrange
            (var webApplicationFactory, _, _) = Utility.CreateApplication(true);

            //Act
            var (responseStream, responseStatus, _)= await Utility.ExecuteRequestAsync(HttpMethod.Get, $"{Constants.PersonModelRoute}/StreamUserPropertiesById?id=NaN", webApplicationFactory);
            var content = await new StreamReader(responseStream).ReadToEndAsync();

            //Assert
            Assert.AreEqual(404, responseStatus);
            Assert.AreEqual("Not Found", content);
        }
    }
}
