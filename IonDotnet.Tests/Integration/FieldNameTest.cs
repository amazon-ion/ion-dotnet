using IonDotnet.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IonDotnet.Tests.Integration
{
    [TestClass]
    public class FieldNameTest : IntegrationTestBase
    {
        /// <summary>
        /// Test if field name is read correctly even if it matches some keywords
        /// </summary>
        [TestMethod]
        [DataRow("fieldNameInf", "inf")]
        [DataRow("fieldNameQuotedFalse", "false")]
        [DataRow("fieldNameQuotedNan", "nan")]
        [DataRow("fieldNameQuotedNegInf", "+inf")]
        [DataRow("fieldNameQuotedNull", "null")]
        [DataRow("fieldNameQuotedNullInt", "null.int")]
        [DataRow("fieldNameQuotedPosInf", "+inf")]
        [DataRow("fieldNameQuotedTrue", "true")]
        public void FieldNameInStruct(string fileName, string fieldName)
        {
            void assertReader(IIonReader reader)
            {
                Assert.AreEqual(IonType.Struct, reader.MoveNext());
                reader.StepIn();
                Assert.AreEqual(IonType.Bool, reader.MoveNext());
                Assert.AreEqual(fieldName, reader.CurrentFieldName);
                Assert.IsFalse(reader.BoolValue());
            }

            void writeFunc(IIonWriter writer)
            {
                writer.StepIn(IonType.Struct);
                writer.SetFieldName(fieldName);
                writer.WriteBool(false);
                writer.StepOut();
                writer.Finish();
            }

            var r = ReaderFromFile(DirStructure.IonTestFile($"good/{fileName}.ion"), InputStyle.Text);
            assertReader(r);
            AssertReaderWriter(assertReader, writeFunc);
        }
    }
}
