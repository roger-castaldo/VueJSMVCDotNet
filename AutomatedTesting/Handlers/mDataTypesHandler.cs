using AutomatedTesting.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Interfaces;
using static AutomatedTesting.Models.mDataTypes;

namespace AutomatedTesting.Handlers
{
    [ModelRouteAttribute(Constants.DataTypesModelRoute)]
    public class mDataTypesHandler : IModelHandler<mDataTypes>
    {
        ValueTask<mDataTypes> IModelHandler<mDataTypes>.LoadAsync(string id)
            => ValueTask.FromResult<mDataTypes>(null);

        [ExposedMethodAttribute()]
        [NotNullArguementAttribute("stringArg")]
        public void TestSingleNotNullInput(string stringArg, string nullStringArg) { }

        [ExposedMethodAttribute()]
        [NotNullArguementAttribute(["stringArg", "byteArrayArg", "IPAddressArg", "VersionArg", "ExceptionArg"])]
        public void TestInputs(
            [ModelIDParameter] string modelId,
            string stringArg, string nullStringArg,
            char charArg, char? nullCharArg,
            short shortArg, short? nullShortArg,
            ushort ushortArg, ushort? nullUShortArg,
            int intArg, int? nullIntArg,
            uint uintArg, uint? nullUIntArg,
            long longArg, long? nullLongArg,
            ulong ulongArg, ulong? nullULongArg,
            float floatArg, float? nullFloatArg,
            decimal decimalArg, decimal? nullDecimalArg,
            double doubleArg, double? nullDoubleArg,
            byte byteArg, byte? nullByteArg,
            bool boolArg, bool? nullBooleanArg,
            TestEnums enumArg, TestEnums? nullEnumArg,
            DateTime DateTimeArg, DateTime? nullDateTimeArg,
            byte[] byteArrayArg, byte[] nullByteArrayArg,
            IPAddress IPAddressArg, IPAddress nullIPAddressArg,
            Version VersionArg, Version nullVersionArg,
            Exception ExceptionArg, Exception nullExceptionArg,
            Guid guidArg, Guid? nullGuidArg
        )
        {

        }

        [ExposedMethodAttribute()]
        [NotNullArguementAttribute(new string[] { "stringArg", "byteArrayArg", "IPAddressArg", "VersionArg", "ExceptionArg" })]
        public void StaticTestInputs(
            string stringArg, string nullStringArg,
            char charArg, char? nullCharArg,
            short shortArg, short? nullShortArg,
            ushort ushortArg, ushort? nullUShortArg,
            int intArg, int? nullIntArg,
            uint uintArg, uint? nullUIntArg,
            long longArg, long? nullLongArg,
            ulong ulongArg, ulong? nullULongArg,
            float floatArg, float? nullFloatArg,
            decimal decimalArg, decimal? nullDecimalArg,
            double doubleArg, double? nullDoubleArg,
            byte byteArg, byte? nullByteArg,
            bool boolArg, bool? nullBooleanArg,
            TestEnums enumArg, TestEnums? nullEnumArg,
            DateTime DateTimeArg, DateTime? nullDateTimeArg,
            byte[] byteArrayArg, byte[] nullByteArrayArg,
            IPAddress IPAddressArg, IPAddress nullIPAddressArg,
            Version VersionArg, Version nullVersionArg,
            Exception ExceptionArg, Exception nullExceptionArg,
            Guid guidArg, Guid? nullGuidArg
        )
        {

        }

        [ModelListMethodAttribute()]
        [NotNullArguementAttribute(new string[] { "stringArg", "byteArrayArg", "IPAddressArg", "VersionArg", "ExceptionArg" })]
        public static List<mDataTypes> TestListInputs(
            string stringArg, string nullStringArg,
            char charArg, char? nullCharArg,
            short shortArg, short? nullShortArg,
            ushort ushortArg, ushort? nullUShortArg,
            int intArg, int? nullIntArg,
            uint uintArg, uint? nullUIntArg,
            long longArg, long? nullLongArg,
            ulong ulongArg, ulong? nullULongArg,
            float floatArg, float? nullFloatArg,
            decimal decimalArg, decimal? nullDecimalArg,
            double doubleArg, double? nullDoubleArg,
            byte byteArg, byte? nullByteArg,
            bool boolArg, bool? nullBooleanArg,
            TestEnums enumArg, TestEnums? nullEnumArg,
            DateTime DateTimeArg, DateTime? nullDateTimeArg,
            IPAddress IPAddressArg, IPAddress nullIPAddressArg,
            Version VersionArg, Version nullVersionArg,
            Exception ExceptionArg, Exception nullExceptionArg,
            Guid guidArg, Guid? nullGuidArg
        )
        {
            return null;
        }

        [ExposedMethodAttribute()]
        [NotNullArguementAttribute(["people", "person"])]
        public void TestModelInputs(mPerson[] people, mPerson person) { }

        [EventStreamMethod]
        [NotNullArguementAttribute(["stringArg", "IPAddressArg", "VersionArg"])]
        public async Task TestStaticEventStreamInputs(
            string stringArg, string nullStringArg,
            char charArg, char? nullCharArg,
            short shortArg, short? nullShortArg,
            ushort ushortArg, ushort? nullUShortArg,
            int intArg, int? nullIntArg,
            uint uintArg, uint? nullUIntArg,
            long longArg, long? nullLongArg,
            ulong ulongArg, ulong? nullULongArg,
            float floatArg, float? nullFloatArg,
            decimal decimalArg, decimal? nullDecimalArg,
            double doubleArg, double? nullDoubleArg,
            byte byteArg, byte? nullByteArg,
            bool boolArg, bool? nullBooleanArg,
            TestEnums enumArg, TestEnums? nullEnumArg,
            DateTime DateTimeArg, DateTime? nullDateTimeArg,
            IPAddress IPAddressArg, IPAddress nullIPAddressArg,
            Version VersionArg, Version nullVersionArg,
            Guid guidArg, Guid? nullGuidArg,
            ChannelWriter<object> writer, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            await writer.WriteAsync("complete");
            writer.Complete();
        }

        [EventStreamMethod]
        [NotNullArguementAttribute(["stringArg", "IPAddressArg", "VersionArg"])]
        public async Task TestEventStreamInputs(
            [ModelIDParameter] string id,
            string stringArg, string nullStringArg,
            char charArg, char? nullCharArg,
            short shortArg, short? nullShortArg,
            ushort ushortArg, ushort? nullUShortArg,
            int intArg, int? nullIntArg,
            uint uintArg, uint? nullUIntArg,
            long longArg, long? nullLongArg,
            ulong ulongArg, ulong? nullULongArg,
            float floatArg, float? nullFloatArg,
            decimal decimalArg, decimal? nullDecimalArg,
            double doubleArg, double? nullDoubleArg,
            byte byteArg, byte? nullByteArg,
            bool boolArg, bool? nullBooleanArg,
            TestEnums enumArg, TestEnums? nullEnumArg,
            DateTime DateTimeArg, DateTime? nullDateTimeArg,
            IPAddress IPAddressArg, IPAddress nullIPAddressArg,
            Version VersionArg, Version nullVersionArg,
            Guid guidArg, Guid? nullGuidArg,
            ChannelWriter<object> writer, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            await writer.WriteAsync("complete");
            writer.Complete();
        }
    }
}
