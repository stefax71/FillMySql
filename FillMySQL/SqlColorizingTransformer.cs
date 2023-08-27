using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;

namespace FillMySQL;

public class SqlColorizingTransformer : HighlightingColorizer
{
    private readonly SqlProcessor _sqlProcessor;
    private int _currentLine;
    private Dictionary<int, int> lineLengths;
    private HighlightingColor plainTextColor;
    public SqlColorizingTransformer(SqlProcessor sqlProcessor, IHighlightingDefinition highlightingDefinition): base(highlightingDefinition)
    {
        plainTextColor = highlightingDefinition.GetNamedColor("PlainText");
        this._sqlProcessor = sqlProcessor;
        lineLengths = new Dictionary<int, int>();
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (line.LineNumber == 0)
        {
            lineLengths.Clear();
        }

        if (line.Length > 0 && !lineLengths.ContainsKey(line.LineNumber))
        {
            lineLengths.Add(line.LineNumber, line.TotalLength);    
        }
        _currentLine = line.LineNumber;
        base.ColorizeLine(line);
    }

    protected override void ApplyColorToElement(VisualLineElement element, HighlightingColor color)
    {
        var currentOffset = element.RelativeTextOffset;
        int totalOffset = 0;
        for (int i = 1; i < _currentLine; i++)
        {
            if (lineLengths.ContainsKey(i))
            {
                totalOffset += lineLengths[i];    
            }
            
        }
        totalOffset += currentOffset;
        (int queryIndex, QueryData? qd) = _sqlProcessor.GetQueryAtCharacterPosition(totalOffset);
        if (queryIndex > 0)
        {
            base.ApplyColorToElement(element, color);    
        }
    }
}
