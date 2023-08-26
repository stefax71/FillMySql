using System;
using System.Collections.Generic;
using FillMySQL;
using NUnit.Framework;

namespace FillMySQLTests
{
    
    [TestFixture]
    public class ProcessorTest
    {
        private const string TestString = @"This is the first line not containing any SQL
                        (stupid text before) Select  distinct  FIELD1, FIELD2, FIELD 2 From Table1 T, Table2 T2 Where T2.FIELD2=0 And And T3.SOMEOTHERFIELD='MyValue' And  FILTER1=? And FILTER 2 IN (?, ?) And  FILTER3 IN(?, ?) And ( FILTER4 IN (Select SOMETHING From TABLE Where TABLE.FIELD=?)) And  (FINALCONDITION=?) and rownum <= 20000 ['9044954',1,110,2,1,1,7] (silly text after)
                        This is the third line, not containing any SQL
                        Select  distinct  FIELD1, FIELD2, FIELD 2 From Table1 T, Table2 T2 Where T2.FIELD2=0 And And T3.SOMEOTHERFIELD='MyValue' And  FILTER1=? And FILTER 2 IN (?, ?) And  FILTER3 IN(?, ?) And ( FILTER4 IN (Select SOMETHING From TABLE Where TABLE.FIELD=?)) And  (FINALCONDITION=?) and rownum <= 20000 ['9044954',1,110,2,1,1,7]
                        This is the fourth line, not containing any SQL
                        SELECT * FROM TABLE 
                        This is the fifth line";

        [Test]
        public void WhenInitializingWithEmptyText_RaiseException()
        {
            var mySqlProcessor = new SqlProcessor();
            Assert.Catch<ArgumentException>(() => mySqlProcessor.Load(""));
            
        }

        [Test]
        public void WhenInitializingWithStringWithNoQuery_RaiseException()
        {
            var mySqlProcessor = new SqlProcessor();
            Assert.Catch<ArgumentException>(() => mySqlProcessor.Load("Lorem ipsum dolor sit amet"));
        }

        [Test]
        public void WhenInitializingWithStringWithJustOneQuery_CallingFirstQueryReturnsItAsTuple()
        {
            var mySqlProcessor = new SqlProcessor();
            mySqlProcessor.Load("SELECT * FROM TABLE WHERE FIELD=? AND OTHERFIELD=? [1, 'abc']");
            QueryData queryData = mySqlProcessor.GetQueryAtPosition(1);
            Assert.AreEqual("SELECT * FROM TABLE WHERE FIELD=? AND OTHERFIELD=?", queryData.Query);
            Assert.AreEqual("[1, 'abc']", queryData.QueryParameters);
        }
        
        [Test]
        public void WhenInitializingWithDirtyStringWithOneQuery_CallingFirstQueryReturnsItAsTuple()
        {
            var sqlProcessor = new SqlProcessor();
            sqlProcessor.Load("(OtherText Before Query) SELECT * FROM SECONDTABLE WHERE FIELD=? AND OTHERFIELD=? [1, 'abc'](text after)");
            QueryData queryData = sqlProcessor.GetQueryAtPosition(1);
            Assert.AreEqual("SELECT * FROM SECONDTABLE WHERE FIELD=? AND OTHERFIELD=?", queryData.Query);
            Assert.AreEqual("[1, 'abc']", queryData.QueryParameters);
        }

        [Test]
        public void WhenInitializingWithMultilineStringWithTwoQueries_GettingFirstQueryReturnsFirstQueryInText()
        {
            var sqlProcessor = new SqlProcessor();
            sqlProcessor.Load(@"(OtherText Before Query) SELECT * FROM SECONDTABLE WHERE FIELD=? AND OTHERFIELD=? [1, 'abc'](text after)
                                            SELECT * FROM FIRSTTABLE WHERE FIELDONE=? [8]");

            QueryData queryData = sqlProcessor.GetQueryAtPosition(2);
            
            Assert.AreEqual("SELECT * FROM FIRSTTABLE WHERE FIELDONE=?", queryData.Query);
            Assert.AreEqual("[8]", queryData.QueryParameters);
        }

        [Test]
        public void WhenRequestingFirstStringPosition_ReturnsActualBeginAndEndCharacterIndexForSql()
        {
            string initializer =
                @"(OtherText Before Query) SELECT * FROM SECONDTABLE WHERE FIELD=? AND OTHERFIELD=? [1, 'abc'](text after)
                                            Meaningless Text
                                            SELECT * FROM FIRSTTABLE WHERE FIELDONE=? [8]";
            SqlProcessor sqlProcessor = new SqlProcessor();
            sqlProcessor.Load(initializer);
        
            (int StartSql, int EndSql, int StartParam, int EndParam) = sqlProcessor.GetIndexesOfQuery(1);
            Assert.AreEqual(25, StartSql);
            Assert.AreEqual(81, EndSql);
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
            SqlProcessor sqlProcessor = new SqlProcessor();
            sqlProcessor.Load(TestString);
        
            (int StartSql, int EndSql, int StartParam, int EndParam) = sqlProcessor.GetIndexesOfQuery(2);
            Assert.AreEqual(527, StartSql);
            Assert.AreEqual(819, EndSql);
            Assert.AreEqual(820, StartParam);
            Assert.AreEqual(845, EndParam);

            string queryCheck = TestString.Substring(StartSql, EndSql - StartSql);
            Assert.AreEqual("Select  distinct  FIELD1, FIELD2, FIELD 2 From Table1 T, Table2 T2 Where T2.FIELD2=0 And And T3.SOMEOTHERFIELD='MyValue' And  FILTER1=? And FILTER 2 IN (?, ?) And  FILTER3 IN(?, ?) And ( FILTER4 IN (Select SOMETHING From TABLE Where TABLE.FIELD=?)) And  (FINALCONDITION=?) and rownum <= 20000", 
                                queryCheck.Trim());

            string paramsCheck = TestString.Substring(StartParam, EndParam - StartParam);
            Assert.AreEqual("['9044954',1,110,2,1,1,7]", paramsCheck.Trim());
        }
        
        [Test, Description("Test description here")]
        public void WhenRequestingQueryWithNoParamsPositionInComplexString_ReturnsActualBeginAndEndCharacterIndexForSql()
        {
            SqlProcessor sqlProcessor = new SqlProcessor();
            sqlProcessor.Load(TestString);
        
            (int StartSql, int EndSql, int StartParam, int EndParam) = sqlProcessor.GetIndexesOfQuery(3);
            Assert.AreEqual(944, StartSql);
            Assert.AreEqual(963, EndSql);
            Assert.AreEqual(-1, StartParam);
            Assert.AreEqual(-1, EndParam);

            string queryCheck = TestString.Substring(StartSql, EndSql - StartSql);
            Assert.AreEqual("SELECT * FROM TABLE", 
                queryCheck.Trim());
        }

        [Test]
        public void WhenRequestingSecondProcessedQuery_ReturnsQueryWithParamsInterpolated()
        {
            string expected =
                "Select  distinct  FIELD1, FIELD2, FIELD 2 From Table1 T, Table2 T2 Where T2.FIELD2=0 And And T3.SOMEOTHERFIELD='MyValue' And  FILTER1='9044954' And FILTER 2 IN (1, 110) And  FILTER3 IN(2, 1) And ( FILTER4 IN (Select SOMETHING From TABLE Where TABLE.FIELD=1)) And  (FINALCONDITION=7) and rownum <= 20000";
            SqlProcessor sqlProcessor = new SqlProcessor();
            sqlProcessor.Load(TestString);
            string processedQuery = sqlProcessor.GetQueryProcessed(2);
            Assert.AreEqual(expected.ToLower(), processedQuery.ToLower());
        }

        [Test]
        public void WhenRequestingProcessedQueryWithNoParams_ReturnsSimpleQuery()
        {
            string expected = "SELECT * FROM TABLE";
            SqlProcessor sqlProcessor = new SqlProcessor();
            sqlProcessor.Load(TestString);
            string processedQuery = sqlProcessor.GetQueryProcessed(3);
            Assert.AreEqual(expected.ToLower(), processedQuery.ToLower());
        }

        [Test]
        public void WhenOpeningNewFile_AutomaticallyProcessContent()
        {
            string expected =
                "Select  distinct  FIELD1, FIELD2, FIELD 2 From Table1 T, Table2 T2 Where T2.FIELD2=0 And And T3.SOMEOTHERFIELD='MyValue' And  FILTER1='9044954' And FILTER 2 IN (1, 110) And  FILTER3 IN(2, 1) And ( FILTER4 IN (Select SOMETHING From TABLE Where TABLE.FIELD=1)) And  (FINALCONDITION=7) and rownum <= 20000";
            SqlProcessor sqlProcessor = new SqlProcessor();
            sqlProcessor.LoadFile("../../../FillMySQLLib/Sample.log");
            var queryFound = sqlProcessor.GetQueryProcessed(1);
            Assert.AreEqual(expected, queryFound);            
        }


        [Test]
        public void WhenUsingAccessorToRequestQueries_ReturnsAListWithThreeElements()
        {
            SqlProcessor sqlProcessor = new SqlProcessor();
            sqlProcessor.LoadFile("../../../FillMySQLLib/Sample.log");
            Assert.True(sqlProcessor.Queries.Count == 3);            
        }

        [Test]
        public void WhenCallingReset_EmptiesEverything()
        {
            SqlProcessor sqlProcessor = new SqlProcessor();
            sqlProcessor.LoadFile("../../../FillMySQLLib/Sample.log");
            Assert.True(sqlProcessor.Queries.Count == 3);
            Assert.True(sqlProcessor.SqlString.Length > 0);
            sqlProcessor.Reset();
            Assert.True(sqlProcessor.Queries.Count == 0);
            Assert.True(sqlProcessor.SqlString.Length == 0);
        }

        [Test]
        public void WhenRequestingQueryAtCharacter621_ReturnsSecondQuery()
        {
            var expected = "Select  distinct  FIELD1, FIELD2, FIELD 2 From Table1 T, Table2 T2 Where T2.FIELD2=0 And And T3.SOMEOTHERFIELD='MyValue' And  FILTER1=? And FILTER 2 IN (?, ?) And  FILTER3 IN(?, ?) And ( FILTER4 IN (Select SOMETHING From TABLE Where TABLE.FIELD=?)) And  (FINALCONDITION=?) and rownum <= 20000";
            SqlProcessor sqlProcessor = new SqlProcessor();
            sqlProcessor.LoadFile("../../../FillMySQLLib/Sample.log");
            (int position, QueryData? data) = sqlProcessor.GetQueryAtCharacterPosition(621);
            Assert.AreEqual(2, position);
            Assert.NotNull(data);
            Assert.AreEqual(expected, data.Value.Query);
        }

        [Test]
        public void WhenRequestingQueryAtCaracter621InLinuxEOL_ReturnsSecondQuery()
        {
            var source =
                "This is the first line not containing any SQL\n(stupid text before) Select  distinct  FIELD1, FIELD2, FIELD 2 From Table1 T, Table2 T2 Where T2.FIELD2=0 And And T3.SOMEOTHERFIELD='MyValue' And  FILTER1=? And FILTER 2 IN (?, ?) And  FILTER3 IN(?, ?) And ( FILTER4 IN (Select SOMETHING From TABLE Where TABLE.FIELD=?)) And  (FINALCONDITION=?) and rownum <= 20000 ['9044954',1,110,2,1,1,7] (silly text after)\nThis is the third line, not containing any SQL\nSelect  distinct  FIELD1, FIELD2, FIELD 2 From Table1 T, Table2 T2 Where T2.FIELD2=0 And And T3.SOMEOTHERFIELD='MyValue' And  FILTER1=? And FILTER 2 IN (?, ?) And  FILTER3 IN(?, ?) And ( FILTER4 IN (Select SOMETHING From TABLE Where TABLE.FIELD=?)) And  (FINALCONDITION=?) and rownum <= 20000 ['9044954',1,110,2,1,1,7]\nThis is the fourth line, not containing any SQL\nSELECT * FROM TABLE \nThis is the fifth line";
            var expected = "Select  distinct  FIELD1, FIELD2, FIELD 2 From Table1 T, Table2 T2 Where T2.FIELD2=0 And And T3.SOMEOTHERFIELD='MyValue' And  FILTER1=? And FILTER 2 IN (?, ?) And  FILTER3 IN(?, ?) And ( FILTER4 IN (Select SOMETHING From TABLE Where TABLE.FIELD=?)) And  (FINALCONDITION=?) and rownum <= 20000";            
            SqlProcessor sqlProcessor = new SqlProcessor();
            sqlProcessor.Load(source);
            (int position, QueryData? data) = sqlProcessor.GetQueryAtCharacterPosition(621);
            Assert.NotNull(data);
            Assert.AreEqual(2, position);
            Assert.AreEqual(expected, data.Value.Query);
        }
   }
}