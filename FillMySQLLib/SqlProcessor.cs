using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FillMySQL
{
    public class SqlProcessor
    {
        private List<QueryData> _queriesData;
        public ObservableCollection<string> Queries => GetAllQueries();

        public string SqlString { get; private set; }

        public string Eol { get; set; }

        public int NumberOfQueries
        {
            get => _queriesData.Count;
        }

        public void Load(string sqlString)
        {
            CheckIfStringIsEmpty(sqlString);
            CheckIfStringContainsSql(sqlString);

            SqlString = sqlString;
            _queriesData = ProcessSqlContent();
        }

        private static void CheckIfStringContainsSql(string sqlString)
        {
            string pattern = $@"select|delete|update";
            MatchCollection matches = Regex.Matches(sqlString, pattern, RegexOptions.IgnoreCase);
            if (matches.Count == 0)
            {
                throw new ArgumentException("String does not contain any query");
            }
        }

        private static void CheckIfStringIsEmpty(string sqlString)
        {
            if (sqlString.Trim().Length == 0)
            {
                throw new ArgumentException("Empty query string passed");
            }
        }

        public QueryData GetQueryAtPosition(int i)
        {
            if (i > _queriesData.Count)
            {
                throw new ArgumentException("No more queries");
            }
            return _queriesData[i - 1];
        }

        private List<QueryData> ProcessSqlContent()
        {
            var resultingStrings = OriginalStringAsArray();
            _queriesData = new List<QueryData>();

            var currentAbsoluteIndex = 0;
            var queryIndex = 0;
            foreach (var currentString in resultingStrings)
            {
                try
                {
                    CheckIfStringContainsSql(currentString);
                    var queryData = PopulateQueryData(currentAbsoluteIndex, queryIndex, currentString);
                    _queriesData.Add(queryData);
                    queryIndex++;
                }
                catch (ArgumentException ex)
                {
                    // Do nothing, go to next line
                }
                finally
                {
                    currentAbsoluteIndex += currentString.Length + Environment.NewLine.Length;
                }
            }
            return _queriesData;
        }

        public string[] OriginalStringAsArray()
        {
            if (HasWindowsEOL())
            {
                Eol = "\r\n";
                return SqlString.Split(new[] { "\r\n" }, System.StringSplitOptions.None);    
            }

            if (HasMacEOL())
            {
                Eol = "\r";
                return SqlString.Split(new[] { "\r" }, System.StringSplitOptions.None);
            }

            if (HasLinuxEOL())
            {
                Eol = "\n";
                return SqlString.Split(new[] { "\n" }, System.StringSplitOptions.None);
            }

            return new[] { SqlString };
        }

        private bool HasWindowsEOL()
        {
            return SqlString.IndexOf("\r\n", StringComparison.Ordinal) >= 0;
        }

        private bool HasMacEOL()
        {
            return SqlString.IndexOf("\r", StringComparison.Ordinal) >= 0 && SqlString.IndexOf("\n", StringComparison.Ordinal) < 0;
        }
        
        private bool HasLinuxEOL()
        {
            return SqlString.IndexOf("\n", StringComparison.Ordinal) >= 0 && SqlString.IndexOf("\r", StringComparison.Ordinal) < 0;
        }        

        private static (int firstKeyworkPosition, int openingParamDelimiterPosition, int closingParamDelimiterPosition, string
            sqlPart, string paramsPart) ParseQuery(string currentString)
        {
            var firstKeywordPosition = currentString.IndexOf("select", StringComparison.OrdinalIgnoreCase);
            var openingParamDelimiterPosition = currentString.IndexOf("[", StringComparison.OrdinalIgnoreCase) >= 0
                ? currentString.IndexOf("[", StringComparison.OrdinalIgnoreCase)
                : currentString.Length;
            var closingParamDelimiterPosition = currentString.IndexOf("]", StringComparison.OrdinalIgnoreCase);
            var sqlPart = currentString.Substring(firstKeywordPosition,
                openingParamDelimiterPosition - firstKeywordPosition).Trim();
            string paramsPart = null;
            if (openingParamDelimiterPosition > 0 && closingParamDelimiterPosition > 0)
            {
                paramsPart = currentString.Substring(openingParamDelimiterPosition,
                    closingParamDelimiterPosition - openingParamDelimiterPosition + 1).Trim();
            }

            return (firstKeywordPosition, openingParamDelimiterPosition, closingParamDelimiterPosition, sqlPart, paramsPart);
        }



        public (int StartSql, int EndSql, int StartParam, int EndParam) GetIndexesOfQuery(int i)
        {
            return (_queriesData[i - 1].SqlStartPosition, _queriesData[i - 1].SqlEndPosition,
                _queriesData[i - 1].ParamsStartPosition, _queriesData[i - 1].ParamsEndPosition);
        }

        public string GetQueryProcessed(int queryIndex)
        {
            if (queryIndex <= 0 || queryIndex > _queriesData.Count)
            {
                throw new ArgumentException("Indexing starts from 1 and cannot go beyond the number of queries");
            }
            QueryData qd = _queriesData[queryIndex - 1];
            return ProcessQueryFromQueryData(qd);
        }

        public string ProcessQueryFromQueryData(QueryData qd)
        {
            if (qd.QueryParameters == null)
            {
                return qd.Query;
            }

            var (listOfParams, sqlString) = ListOfParams(qd);

            var regex = new Regex(Regex.Escape("?"));
            sqlString = listOfParams.Aggregate(sqlString, (current, currentParam) => regex.Replace(current, currentParam, 1));
            return sqlString.Trim();
        }

        private (string[] listOfParams, string sqlString) ListOfParams(QueryData qd)
        {
            var paramsString = qd.QueryParameters.Replace('[', ' ').Replace(']', ' ').Trim();
            var listOfParams = paramsString.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
            var countPlaceholders = qd.Query.Count(s => s == '?');
            if (countPlaceholders != listOfParams.Length)
            {
                throw new ArgumentException("Number of parameters do not match the number of placeholders");
            }

            var sqlString = qd.Query;
            return (listOfParams, sqlString);
        }

        public void LoadFile(string fillmysqllibSamplelog)
        {
            var fileContent = File.ReadAllText(@fillmysqllibSamplelog, Encoding.UTF8);
            SqlString = fileContent;
            Load(fileContent);
        }

        private ObservableCollection<string> GetAllQueries()
        {
            var list = new ObservableCollection<string>();
            foreach (var currentQueryData in _queriesData)
            {
                list.Add(currentQueryData.Query);
            }
            return list;
        }

        public void Reset()
        {
            SqlString = "";
            _queriesData.Clear();
        }

        public (int, QueryData?) GetQueryAtCharacterPosition(int i)
        {
            var counter = 1;
            foreach (var queryData in _queriesData)
            {
                if (queryData.SqlStartPosition <= i && queryData.SqlEndPosition >= i)
                {
                    return (counter, queryData);
                }
                counter++;
            }
            return (-1, null);
        }
        
        private QueryData PopulateQueryData(in int currentAbsoluteIndex, in int queryIndex, in string currentString)
        {
            var (firstKeywordPosition, openingParamDelimiterPosition, closingParamDelimiterPosition, sqlPart, paramsPart) = ParseQuery(currentString);
            QueryData queryData = new QueryData
            {
                Index = queryIndex + 1,
                SqlStartPosition = firstKeywordPosition + currentAbsoluteIndex,
                SqlEndPosition = openingParamDelimiterPosition + currentAbsoluteIndex - 1,
                ParamsStartPosition = closingParamDelimiterPosition > 0 ? openingParamDelimiterPosition + currentAbsoluteIndex : -1,
                ParamsEndPosition = closingParamDelimiterPosition > 0
                    ? closingParamDelimiterPosition + currentAbsoluteIndex + 1
                    : -1,
                Query = sqlPart,
                QueryParameters = paramsPart
            };
            return queryData;
        }        
    }
}