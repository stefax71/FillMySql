using System;
using System.Collections.Generic;
using FillMySQL;
using NUnit.Framework;

namespace FillMySQLTests
{
    
    [TestFixture]
    public class ProcessorTest
    {
        string testString = @"This is the first line not containing any SQL
                        (stupid text before) Select  distinct  FIELD1, FIELD2, FIELD 2 From Table1 T, Table2 T2 Where T2.FIELD2=0 And And T3.SOMEOTHERFIELD='MyValue' And  FILTER1=? And FILTER 2 IN (?, ?) And  FILTER3 IN(?, ?) And ( FILTER4 IN (Select SOMETHING From TABLE Where TABLE.FIELD=?)) And  (FINALCONDITION=?) and rownum <= 20000 ['9044954',1,110,2,1,1,7] (silly text after)
                        This is the third line, not containing any SQL
                        Select  distinct  FIELD1, FIELD2, FIELD 2 From Table1 T, Table2 T2 Where T2.FIELD2=0 And And T3.SOMEOTHERFIELD='MyValue' And  FILTER1=? And FILTER 2 IN (?, ?) And  FILTER3 IN(?, ?) And ( FILTER4 IN (Select SOMETHING From TABLE Where TABLE.FIELD=?)) And  (FINALCONDITION=?) and rownum <= 20000 ['9044954',1,110,2,1,1,7]
                        This is the fourth line, not containing any SQL
                        SELECT * FROM TABLE 
                        This is the fifth line";

        [Test]
        public void WhenInitializingWithEmptyText_RaiseException()
        {
            Assert.Catch<ArgumentException>(() => new SqlProcessor(""));
        }

        [Test]
        public void WhenInitializingWithStringWithNoQuery_RaiseException()
        {
            Assert.Catch<ArgumentException>(() => new SqlProcessor("Lorem ipsum dolor sit amet"));
        }

        [Test]
        public void WhenInitializingWithStringWithJustOneQuery_CallingFirstQueryReturnsItAsTuple()
        {
            SqlProcessor sqlProcessor =
                new SqlProcessor("SELECT * FROM TABLE WHERE FIELD=? AND OTHERFIELD=? [1, 'abc']");
            (string query, string queryParams) = sqlProcessor.GetQueryAtPosition(1);
            Assert.AreEqual("SELECT * FROM TABLE WHERE FIELD=? AND OTHERFIELD=?", query);
            Assert.AreEqual("[1, 'abc']", queryParams);
        }
        
        [Test]
        public void WhenInitializingWithDirtyStringWithOneQuery_CallingFirstQueryReturnsItAsTuple()
        {
            SqlProcessor sqlProcessor =
                new SqlProcessor("(OtherText Before Query) SELECT * FROM SECONDTABLE WHERE FIELD=? AND OTHERFIELD=? [1, 'abc'](text after)");
            (string query, string queryParams) = sqlProcessor.GetQueryAtPosition(1);
            Assert.AreEqual("SELECT * FROM SECONDTABLE WHERE FIELD=? AND OTHERFIELD=?", query);
            Assert.AreEqual("[1, 'abc']", queryParams);
        }

        [Test]
        public void WhenInitializingWithMultilineStringWithTwoQueries_GettingFirstQueryReturnsFirstQueryInText()
        {
            SqlProcessor sqlProcessor =
                new SqlProcessor(@"(OtherText Before Query) SELECT * FROM SECONDTABLE WHERE FIELD=? AND OTHERFIELD=? [1, 'abc'](text after)
                                            SELECT * FROM FIRSTTABLE WHERE FIELDONE=? [8]");

            (string query, string queryParams) = sqlProcessor.GetQueryAtPosition(2);
            Assert.AreEqual("SELECT * FROM FIRSTTABLE WHERE FIELDONE=?", query);
            Assert.AreEqual("[8]", queryParams);
        }

        [Test]
        public void WhenRequestingFirstStringPosition_ReturnsActualBeginAndEndCharacterIndexForSql()
        {
            string initializer =
                @"(OtherText Before Query) SELECT * FROM SECONDTABLE WHERE FIELD=? AND OTHERFIELD=? [1, 'abc'](text after)
                                            Meaningless Text
                                            SELECT * FROM FIRSTTABLE WHERE FIELDONE=? [8]";
            SqlProcessor sqlProcessor = new SqlProcessor(initializer);
        
            (int StartSql, int EndSql, int StartParam, int EndParam) = sqlProcessor.GetIndexesOfQuery(1);
            Assert.AreEqual(25, StartSql);
            Assert.AreEqual(82, EndSql);
            Assert.AreEqual(82, StartParam);
            Assert.AreEqual(92, EndParam);

            string queryCheck = initializer.Substring(StartSql, EndSql - StartSql);
            Assert.AreEqual("SELECT * FROM SECONDTABLE WHERE FIELD=? AND OTHERFIELD=?", queryCheck.Trim());

            string paramsCheck = initializer.Substring(StartParam, EndParam - StartParam);
            Assert.AreEqual("[1, 'abc']", paramsCheck.Trim());
        }
        
        [Test, Description("Test description here")]
        public void WhenRequestingSecondStringPositionInComplexQuery_ReturnsActualBeginAndEndCharacterIndexForSql()
        {
            SqlProcessor sqlProcessor = new SqlProcessor(testString);
        
            (int StartSql, int EndSql, int StartParam, int EndParam) = sqlProcessor.GetIndexesOfQuery(2);
            Assert.AreEqual(527, StartSql);
            Assert.AreEqual(820, EndSql);
            Assert.AreEqual(820, StartParam);
            Assert.AreEqual(845, EndParam);

            string queryCheck = testString.Substring(StartSql, EndSql - StartSql);
            Assert.AreEqual("Select  distinct  FIELD1, FIELD2, FIELD 2 From Table1 T, Table2 T2 Where T2.FIELD2=0 And And T3.SOMEOTHERFIELD='MyValue' And  FILTER1=? And FILTER 2 IN (?, ?) And  FILTER3 IN(?, ?) And ( FILTER4 IN (Select SOMETHING From TABLE Where TABLE.FIELD=?)) And  (FINALCONDITION=?) and rownum <= 20000", 
                                queryCheck.Trim());

            string paramsCheck = testString.Substring(StartParam, EndParam - StartParam);
            Assert.AreEqual("['9044954',1,110,2,1,1,7]", paramsCheck.Trim());
        }
        
        [Test, Description("Test description here")]
        public void WhenRequestingQueryWithNoParamsPositionInComplexString_ReturnsActualBeginAndEndCharacterIndexForSql()
        {
            SqlProcessor sqlProcessor = new SqlProcessor(testString);
        
            (int StartSql, int EndSql, int StartParam, int EndParam) = sqlProcessor.GetIndexesOfQuery(3);
            Assert.AreEqual(944, StartSql);
            Assert.AreEqual(964, EndSql);
            Assert.AreEqual(-1, StartParam);
            Assert.AreEqual(-1, EndParam);

            string queryCheck = testString.Substring(StartSql, EndSql - StartSql);
            Assert.AreEqual("SELECT * FROM TABLE", 
                queryCheck.Trim());
        }

        [Test]
        public void WhenRequestingSecondProcessedQuery_ReturnsQueryWithParamsInterpolated()
        {
            string expected =
                "Select  distinct  FIELD1, FIELD2, FIELD 2 From Table1 T, Table2 T2 Where T2.FIELD2=0 And And T3.SOMEOTHERFIELD='MyValue' And  FILTER1='9044954' And FILTER 2 IN (1, 110) And  FILTER3 IN(2, 1) And ( FILTER4 IN (Select SOMETHING From TABLE Where TABLE.FIELD=1)) And  (FINALCONDITION=7) and rownum <= 20000";
            SqlProcessor sqlProcessor = new SqlProcessor(testString);
            string processedQuery = sqlProcessor.GetQueryProcessed(2);
            Assert.AreEqual(expected.ToLower(), processedQuery.ToLower());
        }

        [Test]
        public void WhenRequestingProcessedQueryWithNoParams_ReturnsSimpleQuery()
        {
            string expected = "SELECT * FROM TABLE";
            SqlProcessor sqlProcessor = new SqlProcessor(testString);
            string processedQuery = sqlProcessor.GetQueryProcessed(3);
            Assert.AreEqual(expected.ToLower(), processedQuery.ToLower());
            
        }
   }
}