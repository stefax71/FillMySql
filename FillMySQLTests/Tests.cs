using System;
using System.Collections.Generic;
using FillMySQL;
using NUnit.Framework;

namespace FillMySQLTests
{
    
    [TestFixture]
    public class Tests
    {
        string testString = @"This is the first line not containing any SQL
                        Select  distinct  FIELD1, FIELD2, FIELD 2 From Table1 T, Table2 T2 Where T2.FIELD2=0 And And T3.SOMEOTHERFIELD='MyValue' And  FILTER1=? And FILTER 2 IN (?, ?) And  FILTER3 IN(?, ?) And ( FILTER4 IN (Select SOMETHING From TABLE Where TABLE.FIELD=?)) And  (FINALCONDITION=?) and rownum <= 20000 ['9044954',1,110,2,1,1,7]
                        This is the third line, not containing any SQL
                        This is the fourth line, not containing any SQL"; 
        
        [Test]
        public void WhenProcessingEmptyString_DoesNothing()
        {
            FillMySQL.SqlProcessor sqlProcessor = new FillMySQL.SqlProcessor("");
            Assert.True(true);
        }

        [Test]
        public void WhenProcessingStringWithoutSql_DoesNothing()
        {
            FillMySQL.SqlProcessor sqlProcessor = new SqlProcessor("string with no sql");
            Assert.True(true);
        }

        [Test]
        public void WhenProcessingMultipleLines_ExtractThoseThatHaveSQL()
        {
            string[] parts = testString.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            SqlProcessor sqlProcessor = new SqlProcessor(testString);
            List<(string, string)> result = sqlProcessor.Process();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(4, parts.Length);
        }

        [Test]
        public void WhenProcessingSqlLines_ExtractSqlUpToParametersIncluded()
        {
            SqlProcessor sqlProcessor = new SqlProcessor(testString);
            List<(string, string)> result = sqlProcessor.Process();
            var returnedString = result[0];
            var sqlString = result[0].Item1;
            var paramsString = result[0].Item2;
            Assert.IsTrue(sqlString.ToUpper().StartsWith("SELECT"));
            Assert.IsTrue(paramsString.ToUpper().StartsWith("["));
            Assert.IsTrue(paramsString.ToUpper().EndsWith("]"));
        }

        [Test]
        public void WhenProcessingSqlLine_CorrectlyReplacesPlaceholders()
        {
            string expected = "Select  distinct  FIELD1, FIELD2, FIELD 2 From Table1 T, Table2 T2 Where T2.FIELD2=0 And And T3.SOMEOTHERFIELD='MyValue' And  FILTER1='9044954' And FILTER 2 IN (1, 110) And  FILTER3 IN(2, 1) And ( FILTER4 IN (Select SOMETHING From TABLE Where TABLE.FIELD=1)) And  (FINALCONDITION=7) and rownum <= 20000";
            SqlProcessor sqlProcessor = new SqlProcessor(testString);
            List<(string, string)> result = sqlProcessor.Process();
            var sqlString = result[0].Item1;
            var paramsString = result[0].Item2;
            Assert.AreEqual(expected, sqlString);
        }
    }
}