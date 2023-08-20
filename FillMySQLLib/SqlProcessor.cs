using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FillMySQL
{
    public class SqlProcessor
    {
        private string _sql;
        private List<(string, string)> _strings_to_fill;

        public SqlProcessor(string sql)
        {
            this._sql = sql;
            _strings_to_fill = new List<(string, string)>();
        }

        public List<(string, string)> Process()
        {
            string[] parts = _sql.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (var part in parts)
            {
                if (part.IndexOf("select", StringComparison.OrdinalIgnoreCase) < 0) continue;
                
                var startPos = part.IndexOf("select", StringComparison.OrdinalIgnoreCase);
                var paramsStart = part.IndexOf('[', startPos);
                var endPos = part.LastIndexOf("]", StringComparison.Ordinal);

                var sqlString = part.Substring(startPos, paramsStart - startPos);
                var paramsString = part.Substring(paramsStart, endPos - paramsStart + 1);
                sqlString = substituteParams(sqlString, paramsString);
                _strings_to_fill.Add((
                    sqlString,
                    paramsString
                ));
            }

            return _strings_to_fill;
        }

        private string substituteParams(string sqlString, string paramsString)
        {
            paramsString = paramsString.Replace('[', ' ').Replace(']', ' ').Trim();
            string[] listOfParams = paramsString.Split(',');
            int countPlaceholders = sqlString.Count(s => s == '?');
            if (countPlaceholders != listOfParams.Length)
            {
                throw new ArgumentException("Number of parameters do not match the number of placeholders");
            }

            var regex = new Regex(Regex.Escape("?"));
            foreach (var currentParam in listOfParams)
            {
                sqlString = regex.Replace(sqlString, currentParam, 1);
            }
            return sqlString.Trim();
        }
    }
}