using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FillMySQL
{

    struct QueryData
    {
        public int SqlStartPosition;
        public int SQLEndPosition;
        public int ParamsStartPosition;
        public int ParamsEndPosition;
        public string query;
        public string queryParameters;
    }
    
    public class SqlProcessor
    {
        private readonly string _sqlString;
        private List<(string, string)> _queriesInString;
        private List<QueryData> _queriesData;

        public SqlProcessor(string sqlString)
        {
            CheckIfStringIsEmpty(sqlString);
            CheckIfStringContainsSql(sqlString);

            _sqlString = sqlString;
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

        public (string query, string queryParams) GetQueryAtPosition(int i)
        {
            return (_queriesData[i - 1].query, _queriesData[i - 1].queryParameters);
        }

        private List<QueryData> ProcessSqlContent()
        {
            var resultingStrings = _sqlString.Split(new[] { Environment.NewLine }, System.StringSplitOptions.None);
            _queriesData = new List<QueryData>();

            var currentAbsoluteIndex = 0;
            foreach (var currentString in resultingStrings)
            {
                try
                {
                    CheckIfStringContainsSql(currentString);
                    QueryData queryData = new QueryData();
                    var (firstKeywordPosition, openingParamDelimiterPosition, closingParamDelimiterPosition, sqlPart, paramsPart) = ParseQuery(currentString);
                    queryData = PopulateQueryData(firstKeywordPosition, currentAbsoluteIndex, openingParamDelimiterPosition, closingParamDelimiterPosition, sqlPart, paramsPart);
                    _queriesData.Add(queryData);
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

        private static (int firstKeyworkPosition, int openingParamDelimiterPosition, int closingParamDelimiterPosition, string
            sqlPart, string paramsPart) ParseQuery(string currentString)
        {
            int firstKeyworkPosition = currentString.IndexOf("select", StringComparison.OrdinalIgnoreCase);
            int openingParamDelimiterPosition = currentString.IndexOf("[", StringComparison.OrdinalIgnoreCase) > 0
                ? currentString.IndexOf("[", StringComparison.OrdinalIgnoreCase)
                : currentString.Length;
            int closingParamDelimiterPosition = currentString.IndexOf("]", StringComparison.OrdinalIgnoreCase);
            string sqlPart = currentString.Substring(firstKeyworkPosition,
                openingParamDelimiterPosition - firstKeyworkPosition).Trim();
            string paramsPart = null;
            if (openingParamDelimiterPosition > 0 && closingParamDelimiterPosition > 0)
            {
                paramsPart = currentString.Substring(openingParamDelimiterPosition,
                    closingParamDelimiterPosition - openingParamDelimiterPosition + 1).Trim();
            }

            return (firstKeyworkPosition, openingParamDelimiterPosition, closingParamDelimiterPosition, sqlPart, paramsPart);
        }

        private QueryData PopulateQueryData(int firstKeyworkPosition, int currentAbsoluteIndex,
            int openingParamDelimiterPosition, int closingParamDelimiterPosition, string sqlPart, string paramsPart)
        {
            QueryData queryData;
            queryData.SqlStartPosition = firstKeyworkPosition + currentAbsoluteIndex;
            queryData.SQLEndPosition = openingParamDelimiterPosition + currentAbsoluteIndex;
            queryData.ParamsStartPosition =
                closingParamDelimiterPosition > 0 ? openingParamDelimiterPosition + currentAbsoluteIndex : -1;
            queryData.ParamsEndPosition = closingParamDelimiterPosition > 0
                ? closingParamDelimiterPosition + currentAbsoluteIndex + 1
                : -1;
            queryData.query = sqlPart;
            queryData.queryParameters = paramsPart;
            return queryData;
        }

        public (int StartSql, int EndSql, int StartParam, int EndParam) GetIndexesOfQuery(int i)
        {
            return (_queriesData[i - 1].SqlStartPosition, _queriesData[i - 1].SQLEndPosition,
                _queriesData[i - 1].ParamsStartPosition, _queriesData[i - 1].ParamsEndPosition);
        }

        public string GetQueryProcessed(int queryIndex)
        {
            QueryData qd = _queriesData[queryIndex - 1];
            if (qd.queryParameters == null)
            {
                return qd.query;
            }

            var (listOfParams, sqlString) = ListOfParams(qd);

            var regex = new Regex(Regex.Escape("?"));
            sqlString = listOfParams.Aggregate(sqlString, (current, currentParam) => regex.Replace(current, currentParam, 1));
            return sqlString.Trim();
        }

        private (string[] listOfParams, string sqlString) ListOfParams(QueryData qd)
        {
            var paramsString = qd.queryParameters.Replace('[', ' ').Replace(']', ' ').Trim();
            var listOfParams = paramsString.Split(',');
            var countPlaceholders = qd.query.Count(s => s == '?');
            if (countPlaceholders != listOfParams.Length)
            {
                throw new ArgumentException("Number of parameters do not match the number of placeholders");
            }

            var sqlString = qd.query;
            return (listOfParams, sqlString);
        }
    }
}