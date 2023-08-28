using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;

namespace FillMySQL;

public class SqlColorizingTransformer : HighlightingColorizer
{
    private readonly SqlProcessor _sqlProcessor;
    private int _currentLine;
    private readonly Dictionary<int, int> _lineLengths;

    public SqlColorizingTransformer(SqlProcessor sqlProcessor, IHighlightingDefinition highlightingDefinition): base(highlightingDefinition)
    {
        highlightingDefinition.GetNamedColor("PlainText");
        this._sqlProcessor = sqlProcessor;
        _lineLengths = new Dictionary<int, int>();
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (line.LineNumber == 0)
        {
            _lineLengths.Clear();
        }

        if (line.Length > 0 && !_lineLengths.ContainsKey(line.LineNumber))
        {
            _lineLengths.Add(line.LineNumber, line.TotalLength);    
        }
        _currentLine = line.LineNumber;
        base.ColorizeLine(line);
    }

    protected override void ApplyColorToElement(VisualLineElement element, HighlightingColor color)
    {
        var currentOffset = element.RelativeTextOffset;
        var totalOffset = 0;
        for (var i = 1; i < _currentLine; i++)
        {
            if (_lineLengths.TryGetValue(i, out var length))
            {
                totalOffset += length;
            }
        }

        totalOffset += currentOffset;
        var (queryIndex, _) = _sqlProcessor.GetQueryAtCharacterPosition(totalOffset);
        if (queryIndex > 0)
        {
            base.ApplyColorToElement(element, color);
        }
    }
}
