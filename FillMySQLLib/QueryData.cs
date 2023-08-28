namespace FillMySQL
{
    public struct QueryData
    {
        public int Index { get; set; }

        public int SqlStartPosition { get; set; }

        public int SqlEndPosition { get; set; }

        public int ParamsStartPosition { get; set; }

        public int ParamsEndPosition { get; set; }

        public string Query { get; set; }

        public string QueryParameters { get; set; }
    }
    
}