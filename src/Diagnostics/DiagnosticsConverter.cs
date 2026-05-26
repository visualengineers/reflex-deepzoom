namespace ReFlex.Apps.DeepZoom.Diagnostics;

public static class DiagnosticsConverter
{
    public static string ToCsv(this DiagnosticsData data)
    {
        return $"{DiagnosticsData.ApplicationId};{data.EventTypeDescription};{FirstLine(data.Data1)};{FirstLine(data.Data2)};{FirstLine(data.Remarks)};{data.Timestamp}";
    }
    
    public static DiagnosticsDataEncoded ToJson(this DiagnosticsData data)
    {
        var msg = data.ToCsv();
        
        return new DiagnosticsDataEncoded { Message = msg };
    }

    private static string FirstLine(string rawValue)
    {
        var newlineIndex = rawValue.IndexOfAny(['\r', '\n']);

        var result = newlineIndex >= 0
            ? rawValue[..newlineIndex]
            : rawValue;

        return result;
    }
}