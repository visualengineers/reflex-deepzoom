namespace ReFlex.Apps.DeepZoom.Events.EventData;

public class ImageChangeRequestArguments
{
    public ImageChangeRequestType Type { get; set; } = ImageChangeRequestType.NextImage;

    public int Parameter { get; set; } = 0;
}